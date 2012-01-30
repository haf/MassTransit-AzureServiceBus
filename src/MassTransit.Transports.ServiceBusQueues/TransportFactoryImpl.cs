using System;
using Magnum.Extensions;
using Magnum.Threading;
using MassTransit.Exceptions;

namespace MassTransit.Transports.ServiceBusQueues
{
	public class TransportFactoryImpl
		: ITransportFactory
	{
		private readonly ReaderWriterLockedDictionary<Uri, ConnectionHandler<ConnectionImpl>> _connectionCache;
		private bool _disposed;

		public TransportFactoryImpl()
		{
			_connectionCache = new ReaderWriterLockedDictionary<Uri, ConnectionHandler<ConnectionImpl>>();
		}

		/// <summary>
		/// 	Gets the scheme. (af-queues)
		/// </summary>
		public string Scheme
		{
			get { return "sb-queues"; }
		}

		/// <summary>
		/// 	Builds the loopback.
		/// </summary>
		/// <param name="settings"> The settings. </param>
		/// <returns> </returns>
		public IDuplexTransport BuildLoopback(ITransportSettings settings)
		{
			return new Transport(settings.Address, () => BuildInbound(settings), () => BuildOutbound(settings));
		}

		public IInboundTransport BuildInbound(ITransportSettings settings)
		{
			EnsureProtocolIsCorrect(settings.Address.Uri);

			var client = GetConnection(settings.Address);
			return new InboundTransportImpl(settings.Address, client);
		}

		public IOutboundTransport BuildOutbound(ITransportSettings settings)
		{
			EnsureProtocolIsCorrect(settings.Address.Uri);

			var client = GetConnection(settings.Address);
			return new OutboundServiceBusQueuesTransport(settings.Address, client);
		}

		public IOutboundTransport BuildError(ITransportSettings settings)
		{
			EnsureProtocolIsCorrect(settings.Address.Uri);

			var client = GetConnection(settings.Address);
			return new OutboundServiceBusQueuesTransport(settings.Address, client);
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

		private ConnectionHandler<ConnectionImpl> GetConnection(IEndpointAddress address)
		{
			return _connectionCache.Retrieve(address.Uri, () =>
				{
					var connection = new ConnectionImpl(address.Uri);
					var connectionHandler = new ConnectionHandlerImpl<ConnectionImpl>(connection);

					return connectionHandler;
				});
		}

		private void Dispose(bool disposing)
		{
			if (_disposed) return;
			if (disposing)
			{
				_connectionCache.Values.Each(x => x.Dispose());
				_connectionCache.Clear();

				_connectionCache.Dispose();
			}

			_disposed = true;
		}

		~TransportFactoryImpl()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}