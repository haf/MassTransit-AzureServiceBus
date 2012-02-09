using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Magnum.Extensions;
using MassTransit.Context;
using MassTransit.Util;
using Microsoft.ServiceBus.Messaging;
using log4net;

namespace MassTransit.Transports.AzureServiceBus
{
	public class InboundTransportImpl
		: IInboundTransport
	{
		private readonly ConnectionHandler<ConnectionImpl> _connectionHandler;
		private readonly AzureServiceBusEndpointAddress _address;

		private bool _disposed;
		private Subscription _subsciption;
		static readonly ILog _logger = SpecialLoggers.Messages;

		public InboundTransportImpl(
			[Util.NotNull] AzureServiceBusEndpointAddress address, 
			[Util.NotNull] ConnectionHandler<ConnectionImpl> connectionHandler)
		{
			if (address == null) throw new ArgumentNullException("address");
			if (connectionHandler == null) throw new ArgumentNullException("connectionHandler");
			_connectionHandler = connectionHandler;
			_address = address;
		}

		public IEndpointAddress Address
		{
			get { return _address; }
		}

		public void Receive(Func<IReceiveContext, Action<IReceiveContext>> callback, TimeSpan timeout)
		{
			AddConsumerBinding();

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

					found.Each(message => TreatMessage(callback, message.Result));
				});
		}

		void TreatMessage(Func<IReceiveContext, Action<IReceiveContext>> callback, BrokeredMessage message)
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
					if (_logger.IsInfoEnabled)
						_logger.InfoFormat("SKIP:{0}:{1}", Address, context.MessageId);

					return;
				}

				if (_logger.IsDebugEnabled)
					_logger.DebugFormat("RECV:{0}:{1}:{2}", _address, message.Label, message.MessageId);

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

		private void AddConsumerBinding()
		{
			if (_subsciption != null)
				return;

			_subsciption = new Subscription(_address);
			_connectionHandler.AddBinding(_subsciption);
		}

		private void RemoveConsumer()
		{
			if (_subsciption != null)
			{
				_connectionHandler.RemoveBinding(_subsciption);
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
				RemoveConsumer();
			}

			_disposed = true;
		}

		~InboundTransportImpl()
		{
			Dispose(false);
		}
	}
}