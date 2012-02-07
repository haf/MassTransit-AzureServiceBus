// Copyright 2012 Henrik Feldt
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Magnum.Extensions;
using MassTransit.Context;
using MassTransit.Transports.AzureServiceBus.Internal;
using MassTransit.Util;
using Microsoft.ServiceBus.Messaging;
using log4net;

namespace MassTransit.Transports.AzureServiceBus
{
	public class InboundTransportImpl
		: IInboundTransport
	{
		readonly ConnectionHandler<ConnectionImpl> _connectionHandler;
		readonly AzureServiceBusEndpointAddress _address;

		bool _disposed;
		Subscription _subsciption;
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
					Func<Task<BrokeredMessage>> startGet = () => Task.Factory.FromAsync<BrokeredMessage>(
						connection.Queue.BeginReceive,
						connection.Queue.EndReceive, null);

					startGet().ContinueWith(tmsg =>
						{
							if (tmsg.IsFaulted)
							{
								if (_logger.IsErrorEnabled)
									_logger.Error(tmsg.Exception.Flatten());

								return;
							}

							if (tmsg.IsCanceled)
							{
								if (_logger.IsInfoEnabled)
									_logger.Info(string.Format("task was cancelled: {0}", tmsg));

								return;
							}

							var message = tmsg.Result;

							if (message == null)
								return;

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
						});

					Thread.Sleep(10);
				});
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

		void AddConsumerBinding()
		{
			if (_subsciption != null)
				return;

			_subsciption = new Subscription(_address);
			_connectionHandler.AddBinding(_subsciption);
		}

		void RemoveConsumer()
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

		void Dispose(bool disposing)
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