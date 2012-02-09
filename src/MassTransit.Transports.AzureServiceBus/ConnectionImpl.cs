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
using System.Collections.Generic;
using System.Linq;
using Magnum.Extensions;
using Magnum.Threading;
using MassTransit.Exceptions;
using MassTransit.Transports.AzureServiceBus.Internal;
using MassTransit.Transports.AzureServiceBus.Management;
using MassTransit.Transports.AzureServiceBus.Util;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using log4net;

namespace MassTransit.Transports.AzureServiceBus
{
	/// <summary>Connection to Azure Service Bus message broker.</summary>
	public class ConnectionImpl 
		: Connection
	{
		readonly AzureServiceBusEndpointAddress _endpointAddress;
		readonly int _prefetchCount;
		readonly MessagingFactory _messagingFactory;

		static readonly ILog _logger = LogManager.GetLogger(typeof (ConnectionImpl));
	
		bool _disposed;
		QueueClient _queue;

		readonly ReaderWriterLockedDictionary<Guid, Tuple<TopicClient, Subscriber>> _subscribers
			= new ReaderWriterLockedDictionary<Guid, Tuple<TopicClient, Subscriber>>();
		
		public ConnectionImpl(
			[NotNull] AzureServiceBusEndpointAddress endpointAddress,
			[NotNull] TokenProvider tokenProvider,
			int prefetchCount = 1000) // todo: configuration setting
		{
			if (endpointAddress == null) throw new ArgumentNullException("endpointAddress");
			if (tokenProvider == null) throw new ArgumentNullException("tokenProvider");

			_endpointAddress = endpointAddress;
			_prefetchCount = prefetchCount;

			/*When using the default lock expiration of 60 seconds, a good value for SubscriptionClient.PrefetchCount
			 * is 20 times the maximum processing rates of all receivers of the factory. For example,
			 * a factory creates 3 receivers. Each receiver can process up to 10 messages per second.
			 * The prefetch count should not exceed 20*3*10 = 600.By default, QueueClient.PrefetchCount
			 * is set to 0, which means that no additional messages are fetched from the service. */

			var mfs = new MessagingFactorySettings
				{
					TokenProvider = tokenProvider,
					NetMessagingTransportSettings =
						{
							// todo: configuration setting
							BatchFlushInterval = 50.Milliseconds()
						}
				};

			_messagingFactory = MessagingFactory.Create(_endpointAddress.NamespaceManager.Address, mfs);

			if (_logger.IsDebugEnabled)
				_logger.DebugFormat("created connection impl for address ('{0}')", endpointAddress);
		}

		public QueueClient Queue
		{
			get { return _queue; }
		}

		public void SignalSubscription(Guid subscriptionId, Topic topic)
		{
			topic.CreateSubscriber()
				.Then(tuple => _subscribers.Add(subscriptionId, tuple));
		}

		public IEnumerable<Subscriber> Subscribers
		{
			// is this a performance issue?
			get { return _subscribers.Values.Select(x => x.Item2); }
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool managed)
		{
			if (!managed)
				return;

			if (_disposed)
				throw new ObjectDisposedException("ServiceBusQueueConnection for {0}".FormatWith(
					_endpointAddress),
				                                  "The connection instance to AppFabric ServiceBus Queues, " +
				                                  "is already disposed and cannot be disposed twice.");
			try
			{
				if (_queue != null)
					_queue.Dispose();

				if (_messagingFactory != null && !_messagingFactory.IsClosed)
					_messagingFactory.Close();
			}
			finally
			{
				_disposed = true;
			}
		}

		public void Connect()
		{
			Disconnect();

			_logger.Info("Connecting {0}".FormatWith(_endpointAddress));

			// check if it's a queue or a subscription to subscribe either the queue or the subscription?
			_queue = _endpointAddress
						.CreateQueue()
						.Then(qdesc => _messagingFactory.TryCreateQueueClient(_endpointAddress.NamespaceManager, qdesc, _prefetchCount))
						.Result;
			
			if (_queue == null)
				throw new TransportException(_endpointAddress.Uri, "The create queue client task returned null.");

		}

		public void Disconnect()
		{
			_logger.Info("Disconnecting {0}".FormatWith(_endpointAddress));
		}
	}
}