using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Magnum.Extensions;
using MassTransit.Configurators;
using MassTransit.Util;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using MassTransit.Transports.AzureServiceBus.Receiver;
using QueueDescription = MassTransit.Transports.AzureServiceBus.QueueDescription;
using TopicDescription = MassTransit.Transports.AzureServiceBus.TopicDescription;

#pragma warning disable 1591

namespace MassTransit.Transports.AzureServiceBus
{
	/// <summary>
	/// implemntation, see <see cref="AzureServiceBusEndpointAddress"/>.
	/// </summary>
	public class AzureServiceBusEndpointAddressImpl 
		: AzureServiceBusEndpointAddress
	{
		internal class Data
		{
			public string UsernameIssuer { get; set; }
			public string PasswordSharedSecret { get; set; }
			public string Namespace { get; set; }
			public string QueueOrTopicName { get; set; }
			public AddressType AddressType { get; set; }
		}

		readonly Uri _friendlyUri;
		readonly Uri _rebuiltUri;
		readonly Data _data;
		readonly TokenProvider _tp;
		readonly Func<MessagingFactory> _mff;
		readonly NamespaceManager _nm;

		readonly QueueDescription _queueDescription;
		readonly TopicDescription _topicDescription;

		internal enum AddressType
		{
			Queue,
			Topic
		}

		AzureServiceBusEndpointAddressImpl([NotNull] Data data,
			AddressType addressType)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			_data = data;

			_tp = TokenProvider.CreateSharedSecretTokenProvider(_data.UsernameIssuer,
			                                                    _data.PasswordSharedSecret);

			var sbUri = ServiceBusEnvironment.CreateServiceUri("sb", _data.Namespace, string.Empty);

			var mfs = new MessagingFactorySettings
				{
					TokenProvider = _tp,
					NetMessagingTransportSettings =
						{
							// todo: configuration setting
							BatchFlushInterval = 50.Milliseconds()
						},
					OperationTimeout = 3.Seconds()
				};
			_mff = () => MessagingFactory.Create(sbUri, mfs);

			_nm = new NamespaceManager(sbUri, _tp);

			string suffix = "";
			if (addressType == AddressType.Queue)
				_queueDescription = new QueueDescriptionImpl(data.QueueOrTopicName);
			else
			{
				_topicDescription = new TopicDescriptionImpl(data.QueueOrTopicName);
				suffix = "?topic=true";
			}

			_rebuiltUri = new Uri(string.Format("azure-sb://{0}:{1}@{2}/{3}{4}", 
				data.UsernameIssuer,
				data.PasswordSharedSecret,
				data.Namespace,
				data.QueueOrTopicName,
				suffix));

			_friendlyUri = new Uri(string.Format("azure-sb://{0}/{1}{2}",
				data.Namespace,
				data.QueueOrTopicName,
				suffix));
		}

		public TokenProvider TokenProvider
		{
			get { return _tp; }
		}

		public Func<MessagingFactory> MessagingFactoryFactory
		{
			get { return _mff; }
		}

		public NamespaceManager NamespaceManager
		{
			get { return _nm; }
		}

		public Task CreateQueue()
		{
			if (QueueDescription == null)
				throw new InvalidOperationException("Cannot create queue is the endpoint address is not for a queue (but for a topic)");

			return _nm.CreateAsync(QueueDescription);
		}

		public QueueDescription QueueDescription
		{
			get { return _queueDescription; }
		}

		public TopicDescription TopicDescription
		{
			get { return _topicDescription; }
		}

		public AzureServiceBusEndpointAddress ForTopic(string topicName)
		{
			if (topicName == null) 
				throw new ArgumentNullException("topicName");

			return new AzureServiceBusEndpointAddressImpl(new Data
				{
					AddressType = AddressType.Topic,
					Namespace = _data.Namespace,
					PasswordSharedSecret = _data.PasswordSharedSecret,
					UsernameIssuer = _data.UsernameIssuer,
					QueueOrTopicName = topicName,
				}, AddressType.Topic);
		}

		[NotNull]
		internal Data Details
		{
			get { return _data; }
		}

		/// <summary>
		/// This uri is MT-schemed, not AzureSB-schemed
		/// </summary>
		public Uri Uri
		{
			get { return _rebuiltUri; }
		}

		bool IEndpointAddress.IsLocal
		{
			get { return false; }
		}

		bool IEndpointAddress.IsTransactional
		{
			get { return false; }
		}

		public void Dispose()
		{
		}

		public override string ToString()
		{
			return _friendlyUri.ToString();
		}

		// factory methods:

		public static AzureServiceBusEndpointAddressImpl Parse([NotNull] Uri uri)
		{
			if (uri == null)
				throw new ArgumentNullException("uri");

			Data data;
			IEnumerable<ValidationResult> results;
			return TryParseInternal(uri, out data, out results)
			       	? new AzureServiceBusEndpointAddressImpl(data, data.AddressType)
			       	: ParseFailed(uri, results);
		}

		public static bool TryParse([NotNull] Uri inputUri, out AzureServiceBusEndpointAddressImpl address,
		                            out IEnumerable<ValidationResult> validationResults)
		{
			if (inputUri == null) throw new ArgumentNullException("inputUri");
			Data data;
			if (TryParseInternal(inputUri, out data, out validationResults))
			{
				address = new AzureServiceBusEndpointAddressImpl(data, data.AddressType);
				return true;
			}
			address = null;
			return false;
		}

		static AzureServiceBusEndpointAddressImpl ParseFailed(Uri uri, IEnumerable<ValidationResult> results)
		{
			throw new ArgumentException(
				string.Format("There were problems parsing the uri '{0}': ", uri)
				+ string.Join(", ", results));
		}

		static bool TryParseInternal(Uri uri, out Data data, out IEnumerable<ValidationResult> results)
		{
			data = null;
			var res = new List<ValidationResult>();

			if (string.IsNullOrWhiteSpace(uri.UserInfo) || !uri.UserInfo.Contains(":"))
			{
				res.Add(new ValidationResultImpl(ValidationResultDisposition.Failure, "UserInfo",
				                                 "UserInfo part of uri (stuff before @-character), doesn't exist or doesn't " +
				                                 "contain the :-character."));
				results = res;
				return false;
			}

			if (uri.AbsolutePath.LastIndexOf('/') != 0) // first item must be /
			{
				res.Add(new ValidationResultImpl(ValidationResultDisposition.Failure, "Application",
				                                 "AbsolutePath part of uri (stuff after hostname), contains more than one slash"));
				results = res;
				return false;
			}

			data = new Data
				{
					UsernameIssuer = uri.UserInfo.Split(':')[0],
					PasswordSharedSecret = uri.UserInfo.Split(':')[1],
					Namespace = uri.Host.Contains(".") 
									? uri.Host.Substring(0, uri.Host.IndexOf('.')) 
									: uri.Host,
					QueueOrTopicName = uri.AbsolutePath.TrimStart('/'),
					AddressType = uri.PathAndQuery.Contains("topic=true") ? AddressType.Topic : AddressType.Queue
				};

			results = null;
			return true;
		}
	}
}