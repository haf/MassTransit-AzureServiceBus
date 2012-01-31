using System;
using System.IO;
using System.Threading;
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
		static readonly ILog _logger = LogManager.GetLogger(typeof (InboundTransportImpl));

		private readonly ConnectionHandler<ConnectionImpl> _connectionHandler;
		private readonly AzureServiceBusEndpointAddress _address;

		private bool _disposed;
		private Subscription _subsciption;

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
					BrokeredMessage message;
					/* timeout: before any message transmission start */
					if ((message = connection.InboundQueue.Receive(50.Milliseconds())) == null
						)//|| ((message = connection.Subscribers.)) == null)
					{
						Thread.Sleep(10);
						return;
					}

					using (var body = new MemoryStream(message.GetBody<MessageEnvelope>().ActualBody, false))
					{
						var context = ReceiveContext.FromBodyStream(body);
						context.SetMessageId(message.MessageId);
						context.SetInputAddress(Address);
						context.SetCorrelationId(message.CorrelationId);

						var receive = callback(context);
						if (receive == null)
						{
							if (SpecialLoggers.Messages.IsInfoEnabled)
								SpecialLoggers.Messages.InfoFormat("SKIP:{0}:{1}", Address, context.MessageId);
						}
						else
						{
							receive(context);
							message.Complete();
						}
					}
				});
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