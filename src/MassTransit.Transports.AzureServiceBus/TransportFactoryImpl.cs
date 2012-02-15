using System;
using Magnum.Extensions;
using Magnum.Threading;
using MassTransit.Exceptions;
using MassTransit.Logging;
using MassTransit.Transports.AzureServiceBus.Util;

namespace MassTransit.Transports.AzureServiceBus
{
	public class TransportFactoryImpl
		: ITransportFactory
	{
		static readonly ILog _logger = Logger.Get(typeof (TransportFactoryImpl));

		private readonly ReaderWriterLockedDictionary<Uri, ConnectionHandler<ConnectionImpl>> _connectionCache;
		private readonly ReaderWriterLockedDictionary<Uri, AzureServiceBusEndpointAddress> _addresses;
		private readonly MessageNameFormatter _messageNameFormatter;
		private bool _disposed;

		public TransportFactoryImpl()
		{
			_addresses = new ReaderWriterLockedDictionary<Uri, AzureServiceBusEndpointAddress>();
			_connectionCache = new ReaderWriterLockedDictionary<Uri, ConnectionHandler<ConnectionImpl>>();
			_messageNameFormatter = new MessageNameFormatter();

			_logger.Debug("created new transport factory");
		}

		/// <summary>
		/// 	Gets the scheme. (af-queues)
		/// </summary>
		public string Scheme
		{
			get { return "azure-sb"; }
		}

		public IMessageNameFormatter MessageNameFormatter
		{
			get { return _messageNameFormatter; }
		}

		/// <summary>
		/// 	Builds the loopback.
		/// </summary>
		/// <param name="settings"> The settings. </param>
		/// <returns> </returns>
		public IDuplexTransport BuildLoopback([NotNull] ITransportSettings settings)
		{
			if (settings == null) 
				throw new ArgumentNullException("settings");

			_logger.Debug("building loopback");

			return new Transport(settings.Address, () => BuildInbound(settings), () => BuildOutbound(settings));
		}

		public virtual IInboundTransport BuildInbound(ITransportSettings settings)
		{
			EnsureProtocolIsCorrect(settings.Address.Uri);

			_logger.Debug(
				() => string.Format("building inbound transport for address '{0}'", settings.Address));

			var address = _addresses.Retrieve(settings.Address.Uri, () => AzureServiceBusEndpointAddressImpl.Parse(settings.Address.Uri));
			var client = GetConnection(address);
			return new InboundTransportImpl(address, client, MessageNameFormatter);
		}

		public virtual IOutboundTransport BuildOutbound(ITransportSettings settings)
		{
			EnsureProtocolIsCorrect(settings.Address.Uri);

			_logger.Debug(() =>
				string.Format("building outbound transport for address '{0}'", settings.Address));

			var address = _addresses.Retrieve(settings.Address.Uri, () => AzureServiceBusEndpointAddressImpl.Parse(settings.Address.Uri));
			var client = GetConnection(address);

			return new OutboundTransportImpl(address, client);
		}

		public virtual IOutboundTransport BuildError(ITransportSettings settings)
		{
			EnsureProtocolIsCorrect(settings.Address.Uri);

			_logger.Debug(
				() => string.Format("building error transport for address '{0}'", settings.Address));

			var address = _addresses.Retrieve(settings.Address.Uri, () => AzureServiceBusEndpointAddressImpl.Parse(settings.Address.Uri));
			var client = GetConnection(address);
			return new OutboundTransportImpl(address, client);
		}

		/// <summary>
		/// 	Ensures the protocol is correct.
		/// </summary>
		/// <param name="address"> The address. </param>
		private void EnsureProtocolIsCorrect(Uri address)
		{
			if (address.Scheme != Scheme)
				throw new EndpointException(address,
				                            string.Format("Address must start with 'stomp' not '{0}'", address.Scheme));
		}

		private ConnectionHandler<ConnectionImpl> GetConnection(AzureServiceBusEndpointAddress address)
		{
			_logger.Debug(() => string.Format("get connection( address:'{0}' )", address));

			return _connectionCache.Retrieve(address.Uri, () =>
				{
					var connection = new ConnectionImpl(address, address.TokenProvider);
					var connectionHandler = new ConnectionHandlerImpl<ConnectionImpl>(connection);

					return connectionHandler;
				});
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool managed)
		{
			if (_disposed)
				return;

			if (managed)
			{
				_connectionCache.Values.Each(x => x.Dispose());
				_connectionCache.Clear();
				_connectionCache.Dispose();

				_addresses.Values.Each(x => x.Dispose());
				_addresses.Clear();
				_addresses.Dispose();
			}

			_disposed = true;
		}
	}
}