using System;
using System.IO;
using MassTransit.Util;
using Microsoft.ServiceBus.Messaging;
using log4net;

namespace MassTransit.Transports.AzureServiceBus
{
	/// <summary>
	/// Outbound transport targeting the azure service bus.
	/// </summary>
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

		/// <summary>
		/// Gets the endpoint address this transport sends to.
		/// </summary>
		public IEndpointAddress Address
		{
			get { return _address; }
		}

		// service bus best practices for performance:
		// http://msdn.microsoft.com/en-us/library/windowsazure/hh528527.aspx
		public void Send(ISendContext context)
		{
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
							
							if (SpecialLoggers.Messages.IsDebugEnabled)
								SpecialLoggers.Messages.Debug(string.Format("SEND-begin:{0}:{1}:{2}",
									_address, bm.Label, bm.MessageId));

							connection.Queue.BeginSend(bm, ar =>
								{
									var q = ar.AsyncState as Tuple<QueueClient, BrokeredMessage>;
									var msg = q.Item2;

									if (SpecialLoggers.Messages.IsDebugEnabled)
										SpecialLoggers.Messages.Debug(string.Format("SEND-end:{0}:{1}:{2}",
											_address, msg.Label, msg.MessageId));

									q.Item1.EndSend(ar); // might blow up
								}, Tuple.Create(connection.Queue, bm));
						}
					});
		}

		public void Dispose()
		{
		}
	}
}