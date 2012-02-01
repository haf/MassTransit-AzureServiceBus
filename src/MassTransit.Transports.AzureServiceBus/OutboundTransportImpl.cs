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
		private readonly AzureServiceBusEndpointAddress _address;

		public OutboundTransportImpl(AzureServiceBusEndpointAddress address,
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
							//bm.ContentType = context.ContentType;
							
							if (!string.IsNullOrWhiteSpace(context.CorrelationId))
								bm.CorrelationId = context.CorrelationId;
							
							if (!string.IsNullOrWhiteSpace(context.MessageId))
								bm.MessageId = context.MessageId;

							connection.Queue.Send(bm); // sync?
						}
					});
		}

		public void Dispose()
		{
		}
	}
}