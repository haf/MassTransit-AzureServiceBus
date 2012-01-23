using System;
using System.IO;
using System.Text;
using System.Threading;
using MassTransit.Context;
using MassTransit.Util;

namespace MassTransit.Transports.AzureQueue
{
	public class InboundServiceBusQueueTransport
		: IInboundTransport
	{
		private readonly ConnectionHandler<ServiceBusQueueConnection> _connectionHandler;
		private readonly IEndpointAddress _address;

		private bool _disposed;
		private ServiceBusQueueSubsciption _subsciption;

		public InboundServiceBusQueueTransport(IEndpointAddress address,
		                                       ConnectionHandler<ServiceBusQueueConnection> connectionHandler)
		{
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

			_connectionHandler
				.Use(connection =>
					{
						StompMessage message;
						if (!connection.StompClient.Messages.TryDequeue(out message))
						{
							Thread.Sleep(10);
							return;
						}

						using (var body = new MemoryStream(Encoding.UTF8.GetBytes(message.Body), false))
						{
							var context = ReceiveContext.FromBodyStream(body);
							context.SetMessageId(message["id"]);
							context.SetInputAddress(Address);

							var receive = callback(context);
							if (receive == null)
							{
								if (SpecialLoggers.Messages.IsInfoEnabled)
									SpecialLoggers.Messages.InfoFormat("SKIP:{0}:{1}", Address, context.MessageId);
							}
							else
							{
								receive(context);
							}
						}
					});
		}

		private void AddConsumerBinding()
		{
			if (_subsciption != null)
				return;

			_subsciption = new ServiceBusQueueSubsciption(_address);
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

		~InboundServiceBusQueueTransport()
		{
			Dispose(false);
		}
	}
}