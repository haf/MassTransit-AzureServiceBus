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
using MassTransit.AzureServiceBus;
using MassTransit.AzureServiceBus.Util;
using MassTransit.Exceptions;
using MassTransit.Logging;
using MassTransit.Transports.AzureServiceBus.Internal;
using MassTransit.Transports.AzureServiceBus.Management;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using MessageSender = MassTransit.AzureServiceBus.MessageSender;

namespace MassTransit.Transports.AzureServiceBus
{
	/// <summary>
	/// 	Connection to Azure Service Bus message broker.
	/// </summary>
	public class ConnectionImpl
		: Connection
	{
		readonly AzureServiceBusEndpointAddress _endpointAddress;
		readonly int _prefetchCount;

		static readonly ILog _log = Logger.Get(typeof (ConnectionImpl));

		bool _disposed;

		MessageSender _messageSender;
		MessagingFactory _messagingFactory;

		public ConnectionImpl(
			[NotNull] AzureServiceBusEndpointAddress endpointAddress,
			int prefetchCount = 1000) // todo: configuration setting
		{
			if (endpointAddress == null) throw new ArgumentNullException("endpointAddress");

			_endpointAddress = endpointAddress;
			_prefetchCount = prefetchCount;

			/*When using the default lock expiration of 60 seconds, a good value for SubscriptionClient.PrefetchCount
			 * is 20 times the maximum processing rates of all receivers of the factory. For example,
			 * a factory creates 3 receivers. Each receiver can process up to 10 messages per second.
			 * The prefetch count should not exceed 20*3*10 = 600.By default, QueueClient.PrefetchCount
			 * is set to 0, which means that no additional messages are fetched from the service. */

			_log.DebugFormat("created connection impl for address ('{0}')", endpointAddress);
		}

		public MessageSender MessageSender
		{
			get { return _messageSender; }
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool managed)
		{
			if (!managed || _disposed)
				return;

			try
			{
				Disconnect();

				if (_messageSender != null)
					_messageSender.Dispose();
			}
			finally
			{
				_disposed = true;
			}
		}

		public void Connect()
		{
			Disconnect();

			_log.DebugFormat("Connecting '{0}'", _endpointAddress);

			if (_messagingFactory == null)
				_messagingFactory = _endpointAddress.MessagingFactoryFactory();

			// check if it's a queue or a subscription to subscribe either the queue or the subscription?
			_messageSender = _endpointAddress.QueueDescription != null
			                 	? _endpointAddress.CreateQueue()
			                 	  	.Then(_ =>
			                 	  	      _messagingFactory.TryCreateMessageSender(_endpointAddress.QueueDescription, _prefetchCount))
			                 	  	.Result
			                 	: _messagingFactory.TryCreateMessageSender(_endpointAddress.TopicDescription)
			                 	  	.Result;

			if (_messageSender == null)
				throw new TransportException(_endpointAddress.Uri, "The create message sender on messaging factory returned null.");
		}

		public void Disconnect()
		{
			_log.DebugFormat("disconnecting '{0}'", _endpointAddress);

			if (_messagingFactory != null)
				_messagingFactory.Close();

			_messagingFactory = null;
		}
	}
}