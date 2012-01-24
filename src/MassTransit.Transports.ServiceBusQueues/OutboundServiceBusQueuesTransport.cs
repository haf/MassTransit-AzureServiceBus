using System.IO;
using System.Text;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.AzureQueue
{
	public class OutboundServiceBusQueuesTransport
		: IOutboundTransport
	{
		private readonly ConnectionHandler<ServiceBusQueuesConnection> _connectionHandler;
		private readonly IEndpointAddress _address;

		public OutboundServiceBusQueuesTransport(IEndpointAddress address,
		                                        ConnectionHandler<ServiceBusQueuesConnection> connectionHandler)
		{
			_connectionHandler = connectionHandler;
			_address = address;
		}

		public IEndpointAddress Address
		{
			get { return _address; }
		}

		public void Send(ISendContext context)
		{
			_connectionHandler
				.Use(connection =>
					{
						using (var body = new MemoryStream())
						{
							context.SerializeTo(body);
							var bm = new BrokeredMessage(new MessageEnvelope(body.ToArray()));
							connection.Queue.Send(bm);
						}
					});
		}

		public void Dispose()
		{
		}
	}
}