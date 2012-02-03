using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MassTransit.Configurators;
using MassTransit.Transports.AzureServiceBus.Management;
using MassTransit.Transports.AzureServiceBus.Util;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.AzureServiceBus
{
	public class AzureServiceBusEndpointAddressImpl 
		: AzureServiceBusEndpointAddress
	{
		internal class Data
		{
			public string UsernameIssuer { get; set; }
			public string PasswordSharedSecret { get; set; }
			public string Namespace { get; set; }
			public string Application { get; set; }
		}

		readonly Uri _rebuiltUri;
		readonly Data _data;
		readonly TokenProvider _tp;
		readonly MessagingFactory _mf;
		readonly NamespaceManager _nm;

		AzureServiceBusEndpointAddressImpl([NotNull] Data data)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			_data = data;

			_tp = TokenProvider.CreateSharedSecretTokenProvider(_data.UsernameIssuer,
			                                                    _data.PasswordSharedSecret);

			var sbUri = ServiceBusEnvironment.CreateServiceUri("sb", _data.Namespace, string.Empty);
			_mf = MessagingFactory.Create(sbUri, _tp);

			_nm = new NamespaceManager(sbUri, _tp);

			_rebuiltUri = new Uri(string.Format("azure-sb://{0}:{1}@{2}/{3}", 
				data.UsernameIssuer,
				data.PasswordSharedSecret,
				data.Namespace, data.Application));
		}

		[NotNull]
		public TokenProvider TokenProvider
		{
			get { return _tp; }
		}

		[NotNull]
		public MessagingFactory MessagingFactory
		{
			get { return _mf; }
		}

		[NotNull]
		public NamespaceManager NamespaceManager
		{
			get { return _nm; }
		}

		[NotNull]
		internal Data Details
		{
			get { return _data; }
		}

		public Task<QueueDescription> CreateQueue()
		{
			return _nm.TryCreateQueue(_data.Application);
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
			if (!_mf.IsClosed) _mf.Close();
			GC.SuppressFinalize(this);
		}

		public override string ToString()
		{
			return _rebuiltUri.ToString();
		}

		public static AzureServiceBusEndpointAddressImpl Parse([NotNull] Uri uri)
		{
			if (uri == null)
				throw new ArgumentNullException("uri");

			Data data;
			IEnumerable<ValidationResult> results;
			return TryParseInternal(uri, out data, out results)
			       	? new AzureServiceBusEndpointAddressImpl(data)
			       	: ParseFailed(uri, results);
		}

		public static bool TryParse([NotNull] Uri inputUri, out AzureServiceBusEndpointAddressImpl address,
		                            out IEnumerable<ValidationResult> validationResults)
		{
			if (inputUri == null) throw new ArgumentNullException("inputUri");
			Data data;
			if (TryParseInternal(inputUri, out data, out validationResults))
			{
				address = new AzureServiceBusEndpointAddressImpl(data);
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
					Application = uri.AbsolutePath.TrimStart('/')
				};

			results = null;
			return true;
		}
	}
}