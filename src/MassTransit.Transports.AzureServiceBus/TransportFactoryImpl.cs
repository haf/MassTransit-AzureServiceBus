using System;
using Magnum.Extensions;
using Magnum.Threading;
using MassTransit.AzureServiceBus;
using MassTransit.Exceptions;
using MassTransit.Logging;
using MassTransit.Transports.AzureServiceBus.Management;
using MassTransit.Transports.AzureServiceBus.Util;

namespace MassTransit.Transports.AzureServiceBus
{
	public class TransportFactoryImpl
		: ITransportFactory
	{
		static readonly ILog _logger = Logger.Get(typeof (TransportFactoryImpl));

		private readonly ReaderWriterLockedDictionary<Uri, ConnectionHandler<ConnectionImpl>> _connCache;
		private readonly ReaderWriterLockedDictionary<Uri, AzureServiceBusEndpointAddress> _addresses;
		private readonly AzureMessageNameFormatter _formatter;
		private bool _disposed;

		public TransportFactoryImpl()
		{
			_addresses = new ReaderWriterLockedDictionary<Uri, AzureServiceBusEndpointAddress>();
			_connCache = new ReaderWriterLockedDictionary<Uri, ConnectionHandler<ConnectionImpl>>();
			_formatter = new AzureMessageNameFormatter();

			_logger.Debug("created new transport factory");
		}

		/// <summary>
		/// 	Gets the scheme. (af-queues)
		/// </summary>
		public string Scheme
		{
			get { return Constants.Scheme; }
		}

		public IMessageNameFormatter MessageNameFormatter
		{
			get { return _formatter; }
		}

		/// <summary>
		/// 	Builds the duplex transport.
		/// </summary>
		/// <param name="settings"> The settings. </param>
		/// <returns> </returns>
		public IDuplexTransport BuildLoopback([NotNull] ITransportSettings settings)
		{
			if (settings == null) 
				throw new ArgumentNullException("settings");

			_logger.Debug("building duplex transport");

			return new Transport(settings.Address, () => BuildInbound(settings), () => BuildOutbound(settings));
		}

		public virtual IInboundTransport BuildInbound(ITransportSettings settings)
		{
			EnsureProtocolIsCorrect(settings.Address.Uri);

			var address = _addresses.Retrieve(settings.Address.Uri, () => AzureServiceBusEndpointAddressImpl.Parse(settings.Address.Uri));
			_logger.DebugFormat("building inbound transport for address '{0}'", address);

			var client = GetConnection(address);

			return new InboundTransportImpl(address, client,
				new AzureManagementImpl(settings.PurgeExistingMessages, address), MessageNameFormatter);
		}

		public virtual IOutboundTransport BuildOutbound(ITransportSettings settings)
		{
			EnsureProtocolIsCorrect(settings.Address.Uri);

			var address = _addresses.Retrieve(settings.Address.Uri, () => AzureServiceBusEndpointAddressImpl.Parse(settings.Address.Uri));
			_logger.DebugFormat("building outbound transport for address '{0}'", address);

			var client = GetConnection(address);

			return new OutboundTransportImpl(address, client);
		}

		public virtual IOutboundTransport BuildError(ITransportSettings settings)
		{
			EnsureProtocolIsCorrect(settings.Address.Uri);

			var address = _addresses.Retrieve(settings.Address.Uri, () => AzureServiceBusEndpointAddressImpl.Parse(settings.Address.Uri));
			_logger.DebugFormat("building error transport for address '{0}'", address);

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
			_logger.DebugFormat("get connection( address:'{0}' )", address);

			return _connCache.Retrieve(address.Uri, () =>
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
				_connCache.Values.Each(x => x.Dispose());
				_connCache.Clear();
				_connCache.Dispose();

				_addresses.Values.Each(x => x.Dispose());
				_addresses.Clear();
				_addresses.Dispose();
			}

			_disposed = true;
		}
	}
}