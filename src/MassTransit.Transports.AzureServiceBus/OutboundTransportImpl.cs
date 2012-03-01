using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Magnum.Extensions;
using Magnum.Threading;
using MassTransit.AzureServiceBus;
using MassTransit.AzureServiceBus.Util;
using MassTransit.Logging;
using Microsoft.ServiceBus.Messaging;
using ILog = MassTransit.Logging.ILog;
using MassTransit.Transports.AzureServiceBus.Internal;
using MessageSender = MassTransit.AzureServiceBus.MessageSender;

namespace MassTransit.Transports.AzureServiceBus
{
	/// <summary>
	/// Outbound transport targeting the azure service bus.
	/// </summary>
	public class OutboundTransportImpl
		: IOutboundTransport
	{
		const int MaxOutstanding = 100;
		const string BusyRetriesKey = "busy-retries";
		static readonly ILog _logger = Logger.Get(typeof(OutboundTransportImpl));

		bool _disposed;

		int _messagesInFlight;
		int _sleeping;

		readonly ReaderWriterLockedObject<Queue<BrokeredMessage>> _retryMsgs
			= new ReaderWriterLockedObject<Queue<BrokeredMessage>>(new Queue<BrokeredMessage>(MaxOutstanding));

		readonly ConnectionHandler<ConnectionImpl> _connectionHandler;
		readonly AzureServiceBusEndpointAddress _address;

		public OutboundTransportImpl(
			[NotNull] AzureServiceBusEndpointAddress address,
			[NotNull] ConnectionHandler<ConnectionImpl> connectionHandler)
		{
			if (address == null) throw new ArgumentNullException("address");
			if (connectionHandler == null) throw new ArgumentNullException("connectionHandler");

			_connectionHandler = connectionHandler;
			_address = address;

			_logger.DebugFormat("created outbound transport for address '{0}'", address);
		}

		public void Dispose()
		{
			if (_disposed) return;
			try
			{
				_address.Dispose();
				_connectionHandler.Dispose();
			}
			finally { _disposed = true; }
		}

		/// <summary>Gets the endpoint address this transport sends to.</summary>
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
							
							if (!string.IsNullOrWhiteSpace(context.CorrelationId))
								bm.CorrelationId = context.CorrelationId;
							
							if (!string.IsNullOrWhiteSpace(context.MessageId))
								bm.MessageId = context.MessageId;
							
							TrySendMessage(connection, bm);
						}
					});
		}

		void TrySendMessage(ConnectionImpl connection, BrokeredMessage message)
		{
			// don't have too many outstanding at same time
			SpinWait.SpinUntil(() => _messagesInFlight < MaxOutstanding);

			Address.LogBeginSend(message.MessageId);
			
			Interlocked.Increment(ref _messagesInFlight);

			connection.MessageSender.BeginSend(message, ar =>
				{
					var tuple = ar.AsyncState as Tuple<MessageSender, BrokeredMessage>;
					var msg = tuple.Item2;
					var sender = tuple.Item1;

					try
					{
						// if the queue is deleted in the middle of things here, then I can't recover
						// at the moment; I have to extend the connection handler with an asynchronous
						// API to let it re-initialize the queue and hence maybe even the full transport...
						// MessagingEntityNotFoundException.
						Interlocked.Decrement(ref _messagesInFlight);

						sender.EndSend(ar);

						Address.LogEndSend(msg.MessageId);
					}
					catch (ServerBusyException ex)
					{
						object val;
						var hasVal = msg.Properties.TryGetValue(BusyRetriesKey, out val);
						if (!hasVal) val = msg.Properties[BusyRetriesKey] = 1;
						_logger.Warn(string.Format("Server Too Busy, for {0}'(st|nd|th) time", val), ex);

						RetryLoop(connection, msg);
					}
					catch (Exception)
					{
						// dispose the message if it's not possible to send it
						msg.Dispose();
						throw;
					}
				}, Tuple.Create(connection.MessageSender, message));
		}

		// call only if first time gotten server busy exception
		private void RetryLoop(ConnectionImpl connection, BrokeredMessage bm)
		{
			Address.LogSendRetryScheduled(bm.MessageId, _messagesInFlight, Interlocked.Increment(ref _sleeping));
			// exception tells me to wait 10 seconds before retrying, so let's sleep 1 second instead,
			// just 2,600,000,000 CPU cycles
			Thread.Sleep(1.Seconds());
			Interlocked.Decrement(ref _sleeping);

			// push all pending retries onto the sending operation
			_retryMsgs.WriteLock(queue =>
				{
					queue.Enqueue(bm);
					while (queue.Count > 0)
						TrySendMessage(connection, queue.Dequeue());
				});
		}
	}
}