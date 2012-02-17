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
using System.Linq;
using System.Text;
using Magnum.Extensions;
using MassTransit.Async;
using MassTransit.Context;
using MassTransit.Logging;
using MassTransit.Transports.AzureServiceBus.Util;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.AzureServiceBus
{
	public class InboundTransportImpl
		: IInboundTransport
	{
		readonly ConnectionHandler<ConnectionImpl> _connectionHandler;
		readonly IMessageNameFormatter _formatter;
		readonly AzureServiceBusEndpointAddress _address;
		static volatile Receiver _r;
		static readonly object _rSem = new object();

		bool _disposed;

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

		public IMessageNameFormatter MessageNameFormatter
		{
			get { return _formatter; }
		}

		public void Receive(Func<IReceiveContext, Action<IReceiveContext>> callback, TimeSpan timeout)
		{
			_logger.Debug(() => "Receive({0}, timeout) called".FormatWith(_address));

			EnsureReceiver();

			_connectionHandler.Use(connection =>
				{
					var message = _r.Get(timeout);

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
				});
		}

		void EnsureReceiver()
		{
			if (_r == null)
				lock (_rSem)
					if (_r == null)
						_r = ReceiverModule.StartReceiver(_address.QueueDescription, () => _address.MessagingFactory, 1000, 30);
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

		void Dispose(bool disposing)
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