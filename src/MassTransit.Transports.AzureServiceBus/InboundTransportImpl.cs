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
using MassTransit.AzureServiceBus;
using MassTransit.Context;
using MassTransit.Logging;
using MassTransit.Transports.AzureServiceBus.Management;
using MassTransit.Transports.AzureServiceBus.Util;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.AzureServiceBus
{
	/// <summary>
	/// Inbound transport implementation for Azure Service Bus.
	/// </summary>
	public class InboundTransportImpl
		: IInboundTransport
	{
		readonly ConnectionHandler<ConnectionImpl> _connectionHandler;
		readonly IMessageNameFormatter _formatter;
		PerConnectionReceiver _receiver;
		readonly AzureManagement _management;
		readonly AzureServiceBusEndpointAddress _address;

		bool _bound;
		bool _disposed;

		static readonly ILog _logger = Logger.Get(typeof (InboundTransportImpl));

		public InboundTransportImpl(
			[NotNull] AzureServiceBusEndpointAddress address,
			[NotNull] ConnectionHandler<ConnectionImpl> connectionHandler,
			[NotNull] IMessageNameFormatter formatter,
			[NotNull] AzureManagement management)
		{
			if (address == null) throw new ArgumentNullException("address");
			if (connectionHandler == null) throw new ArgumentNullException("connectionHandler");
			if (formatter == null) throw new ArgumentNullException("formatter");
			if (management == null) throw new ArgumentNullException("management");

			_connectionHandler = connectionHandler;
			_formatter = formatter;
			_management = management;
			_address = address;

			_logger.DebugFormat("created new inbound transport for '{0}'", address);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool managed)
		{
			if (_disposed)
				return;

			if (!managed)
				return;

			_logger.DebugFormat("disposing transport for '{0}'", Address);

			try
			{
				RemoveReceiverBinding();
				RemoveManagementBinding();
			}
			finally
			{
				_disposed = true;
			}
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
			AddManagementBinding();

			AddReceiverBinding();

			_connectionHandler.Use(connection =>
				{
					var message = _receiver.Get(timeout);

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

		void AddManagementBinding()
		{
			if (!_bound)
				_connectionHandler.AddBinding(_management);
			
			_bound = true;
		}

		void RemoveManagementBinding()
		{
			if (_bound)
				_connectionHandler.RemoveBinding(_management);

			_bound = false;
		}

		void AddReceiverBinding()
		{
			if (_receiver != null)
				return;
			
			_receiver = new PerConnectionReceiver(_address);
			_connectionHandler.AddBinding(_receiver);
		}

		void RemoveReceiverBinding()
		{
			if (_receiver != null)
				_connectionHandler.RemoveBinding(_receiver);
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
	}
}