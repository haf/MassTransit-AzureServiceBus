using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Magnum.Extensions;
using Magnum.Threading;
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
		const int MaxOutstanding = 100;

		static readonly ILog _logger = SpecialLoggers.Messages;

		int _messagesInFlight;

		readonly ReaderWriterLockedObject<Queue<BrokeredMessage>> _retryMsgs
			= new ReaderWriterLockedObject<Queue<BrokeredMessage>>(new Queue<BrokeredMessage>(MaxOutstanding));

		readonly ConnectionHandler<ConnectionImpl> _connectionHandler;
		readonly AzureServiceBusEndpointAddress _address;

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
							
							if (_logger.IsDebugEnabled)
								_logger.Debug(string.Format("SEND-begin:{0}:{1}:{2}",
									_address, bm.Label, bm.MessageId));

							TrySendMessage(connection, bm);
						}
					});
		}

		void TrySendMessage(ConnectionImpl connection, BrokeredMessage bm)
		{
			// don't have too many outstanding at same time
			SpinWait.SpinUntil(() => _messagesInFlight < MaxOutstanding);
			
			Interlocked.Increment(ref _messagesInFlight);

			connection.Queue.BeginSend(bm, ar =>
				{
					var tuple = ar.AsyncState as Tuple<QueueClient, BrokeredMessage>;
					var msg = tuple.Item2;
					var queueClient = tuple.Item1;

					if (_logger.IsDebugEnabled)
						_logger.Debug(string.Format("SEND-end:{0}:{1}:{2}",
						                                            _address, msg.Label, msg.MessageId));

					try
					{
						// if the queue is deleted in the middle of things here, then I can't recover
						// at the moment; I have to extend the connection handler with an asynchronous
						// API to let it re-initialize the queue and hence maybe even the full transport...
						// MessagingEntityNotFoundException.
						Interlocked.Decrement(ref _messagesInFlight);
						queueClient.EndSend(ar);
					}
					catch (ServerBusyException serverBusyException)
					{
						if (_logger.IsWarnEnabled)
							_logger.Warn(string.Format("SEND-too-busy:{0}:{1}:{2}",
							                                           _address, msg.Label, msg.MessageId), serverBusyException);
						
						RetryLoop(connection, bm);
					}
				}, Tuple.Create(connection.Queue, bm));
		}

		// call only if first time gotten server busy exception
		private void RetryLoop(ConnectionImpl connection, BrokeredMessage bm)
		{
			// exception tells me to wait 10 seconds before retrying.
			Thread.Sleep(10.Seconds());

			// push all pending retries onto the sending operation
			_retryMsgs.WriteLock(queue =>
				{
					queue.Enqueue(bm);

					if (_logger.IsInfoEnabled)
						_logger.Info(string.Format("SEND-retry:{0}. Queue count: {1}", _address, queue.Count));

					while (queue.Count > 0)
						TrySendMessage(connection, queue.Dequeue());
				});
		}

		public void Dispose()
		{
		}
	}
}