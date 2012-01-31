using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MassTransit.Configurators;
using MassTransit.Transports.ServiceBusQueues.Internal;
using MassTransit.Transports.ServiceBusQueues.Util;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using MassTransit.Transports.ServiceBusQueues.Management;

namespace MassTransit.Transports.ServiceBusQueues
{
	public class ServiceBusQueuesEndpointAddressImpl 
		: ServiceBusQueuesEndpointAddress
	{
		class Data
		{
			public string UsernameIssuer { get; set; }
			public string PasswordSharedSecret { get; set; }
			public string Namespace { get; set; }
			public string Application { get; set; }
		}

		readonly Data _data;
		readonly TokenProvider _tp;
		readonly MessagingFactory _mf;
		readonly NamespaceManager _nm;

		ServiceBusQueuesEndpointAddressImpl([NotNull] Data data)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			_data = data;

			_tp = TokenProvider.CreateSharedSecretTokenProvider(_data.UsernameIssuer,
			                                                    _data.PasswordSharedSecret);

			var sbUri = ServiceBusEnvironment.CreateServiceUri("sb", _data.Namespace, string.Empty);
			_mf = MessagingFactory.Create(sbUri, _tp);

			_nm = new NamespaceManager(sbUri, _tp);
		}
		
		public TokenProvider TokenProvider
		{
			get { return _tp; }
		}

		public MessagingFactory MessagingFactory
		{
			get { return _mf; }
		}

		public NamespaceManager NamespaceManager
		{
			get { return _nm; }
		}

		public Task<MessageReceiver> CreateQueueClient()
		{
			return _nm.TryCreateQueue(_data.Application).Then(qdesc => _mf.TryCreateQueueClient(qdesc));
		}

		public Uri Uri
		{
			get { return _nm.Address; }
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
			if (!_mf.IsClosed) _mf.Close();
			GC.SuppressFinalize(this);
		}

		public override string ToString()
		{
			return string.Format("{0}{1}", _nm.Address, _data.Application);
		}

		public static ServiceBusQueuesEndpointAddressImpl Parse([NotNull] Uri uri)
		{
			if (uri == null)
				throw new ArgumentNullException("uri");

			Data data;
			IEnumerable<ValidationResult> results;
			return TryParseInternal(uri, out data, out results)
			       	? new ServiceBusQueuesEndpointAddressImpl(data)
			       	: ParseFailed(uri, results);
		}

		public static bool TryParse([NotNull] Uri inputUri, out ServiceBusQueuesEndpointAddressImpl address,
		                            out IEnumerable<ValidationResult> validationResults)
		{
			if (inputUri == null) throw new ArgumentNullException("inputUri");
			Data data;
			if (TryParseInternal(inputUri, out data, out validationResults))
			{
				address = new ServiceBusQueuesEndpointAddressImpl(data);
				return true;
			}
			address = null;
			return false;
		}

		static ServiceBusQueuesEndpointAddressImpl ParseFailed(Uri uri, IEnumerable<ValidationResult> results)
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
					Application = uri.AbsolutePath
				};

			results = null;
			return true;
		}
	}
}