using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Magnum.Extensions;
using MassTransit.Context;
using MassTransit.Logging;
using MassTransit.Transports.AzureServiceBus.Util;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.AzureServiceBus
{
	public class InboundTransportImpl
		: IInboundTransport
	{
		private readonly ConnectionHandler<ConnectionImpl> _connectionHandler;
		private readonly IMessageNameFormatter _formatter;
		private readonly AzureServiceBusEndpointAddress _address;

		private bool _disposed;

		static readonly ILog _logger = Logger.Get(typeof (InboundTransportImpl));

		public InboundTransportImpl(
			[NotNull] AzureServiceBusEndpointAddress address,
			[NotNull] ConnectionHandler<ConnectionImpl> connectionHandler,
			[NotNull] IMessageNameFormatter formatter)
		{
			if (address == null) throw new ArgumentNullException("address");
			if (connectionHandler == null) throw new ArgumentNullException("connectionHandler");
			if (formatter == null) throw new ArgumentNullException("formatter");

			_connectionHandler = connectionHandler;
			_formatter = formatter;
			_address = address;

			_logger.Debug(() => string.Format("created new inbound transport for {0}", address));
		}

		public IEndpointAddress Address
		{
			get { return _address; }
		}

		public IMessageNameFormatter MessageNameFormatter { get { return _formatter; } }

		public void Receive(Func<IReceiveContext, Action<IReceiveContext>> callback, TimeSpan timeout)
		{
			_logger.Debug(() => "Receive(callback, timeout) called");

			_connectionHandler.Use(connection =>
				{
					/* timeout: before any message transmission start */
					var pollitems = new[] {connection.Queue.Receive(timeout)}
						.Concat(connection.Subscribers.Select(x => x.Receive(timeout)))
						.ToArray();

					Task.WaitAll(pollitems);

					var found = pollitems.Where(x => x.Result != null);

					if (!found.Any())
					{
						Thread.Sleep(10);
						return;
					}

					found.Each(message => ReceiveMessage(callback, message.Result));
				});
		}

		void ReceiveMessage(Func<IReceiveContext, Action<IReceiveContext>> callback, BrokeredMessage message)
		{
			using (var body = new MemoryStream(message.GetBody<MessageEnvelope>().ActualBody, false))
			{
				var context = ReceiveContext.FromBodyStream(body);
				context.SetMessageId(message.MessageId);
				context.SetInputAddress(Address);
				context.SetCorrelationId(message.CorrelationId);

				if (_logger.IsDebugEnabled)
					TraceMessage(context);

				var receive = callback(context);
				if (receive == null)
				{
					Address.LogSkipped(message.MessageId);
					return;
				}

				receive(context);

				try
				{
					message.Complete();
				}
				catch (MessageLockLostException ex)
				{
					if (_logger.IsErrorEnabled)
						_logger.Error("Message Lock Lost on message Complete()", ex);
				}
				catch (MessagingException ex)
				{
					if (_logger.IsErrorEnabled)
						_logger.Error("Generic MessagingException thrown", ex);
				}
			}
		}

		static void TraceMessage(ReceiveContext context)
		{
			using (var ms = new MemoryStream())
			{
				context.CopyBodyTo(ms);
				var msg = Encoding.UTF8.GetString(ms.ToArray());
				_logger.Debug(string.Format("{0} body:\n {1}", DateTime.UtcNow, msg));
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (_disposed) return;
			if (disposing)
			{
			}

			_disposed = true;
		}

		~InboundTransportImpl()
		{
			Dispose(false);
		}
	}
}