using System.IO;
using Microsoft.ServiceBus.Messaging;
using log4net;

namespace MassTransit.Transports.AzureServiceBus
{
	public class OutboundTransportImpl
		: IOutboundTransport
	{
		static readonly ILog _logger = LogManager.GetLogger(typeof (OutboundTransportImpl));
		
		private readonly ConnectionHandler<ConnectionImpl> _connectionHandler;
		private readonly IEndpointAddress _address;

		public OutboundTransportImpl(IEndpointAddress address,
		                             ConnectionHandler<ConnectionImpl> connectionHandler)
		{
			_connectionHandler = connectionHandler;
			_address = address;

			_logger.Debug(string.Format("created outbound transport for address '{0}'", address));
		}

		public IEndpointAddress Address
		{
			get { return _address; }
		}

		public void Send(ISendContext context)
		{
			_logger.Debug(string.Format("outbound ('{0}') sending {1}", _address, context));

			_connectionHandler
				.Use(connection =>
					{
						using (var body = new MemoryStream())
						{
							context.SerializeTo(body);
							var bm = new BrokeredMessage(new MessageEnvelope(body.ToArray()));
							//connection.Subscribers.Send(bm);
							// TODO: fix sending, probably we have the correct address in the c'tor.
						}
					});
		}

		public void Dispose()
		{
		}
	}
}