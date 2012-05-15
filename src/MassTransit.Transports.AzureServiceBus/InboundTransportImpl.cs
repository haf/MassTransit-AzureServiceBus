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
using Magnum.Caching;
using MassTransit.Context;
using MassTransit.Logging;
using MassTransit.Transports.AzureServiceBus.Management;
using MassTransit.Util;
using Microsoft.ServiceBus.Messaging;
using MassTransit.Transports.AzureServiceBus.Receiver;
using TopicDescription = MassTransit.Transports.AzureServiceBus.TopicDescription;

#pragma warning disable 1591

namespace MassTransit.Transports.AzureServiceBus
{
	/// <summary>
	/// Inbound transport implementation for Azure Service Bus.
	/// </summary>
	public class InboundTransportImpl
		: IInboundTransport
	{
		readonly IMessageNameFormatter _formatter;
		readonly AzureManagement _management;
		readonly ReceiverSettings _receiverSettings;
		readonly AzureServiceBusEndpointAddress _address;

		readonly ConnectionHandler<ConnectionImpl> _connectionHandler;
		readonly Cache<Guid, TopicDescription> _subscribed = new ConcurrentCache<Guid, TopicDescription>();
		PerConnectionReceiver _receiver;

		bool _bound;
		bool _disposed;

		static readonly ILog _logger = Logger.Get(typeof (InboundTransportImpl));

		public InboundTransportImpl([NotNull] AzureServiceBusEndpointAddress address,
			[NotNull] ConnectionHandler<ConnectionImpl> connectionHandler,
			[NotNull] AzureManagement management, 
			[CanBeNull] IMessageNameFormatter formatter = null,
			[CanBeNull] ReceiverSettings receiverSettings = null)
		{
			if (address == null) throw new ArgumentNullException("address");
			if (connectionHandler == null) throw new ArgumentNullException("connectionHandler");
			if (management == null) throw new ArgumentNullException("management");

			_address = address;
			_connectionHandler = connectionHandler;
			_management = management;
			_formatter = formatter ?? new AzureMessageNameFormatter();
			_receiverSettings = receiverSettings; // can be null all the way to usage in Receiver.

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

			_receiver = new PerConnectionReceiver(_address, _receiverSettings, 
				recv =>
					{
						lock (_subscribed)
							foreach (var d in _subscribed)
								recv.Subscribe(d);
					},
				recv =>
					{
						lock (_subscribed)
							foreach (var d in _subscribed)
								recv.Unsubscribe(d);
					});

			_connectionHandler.AddBinding(_receiver);
		}

		void RemoveReceiverBinding()
		{
			if (_receiver == null)
				return;

			_connectionHandler.RemoveBinding(_receiver);
			_receiver = null;
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

		public void SignalBoundSubscription(Guid key, TopicDescription value)
		{
			lock (_subscribed)
				if (!_subscribed.Has(key))
					_subscribed.Add(key, value);
		}

		public void SignalUnboundSubscription(Guid key, TopicDescription value)
		{
			lock (_subscribed)
				if (_subscribed.Has(key))
					_subscribed.Remove(key);
		}
	}
}