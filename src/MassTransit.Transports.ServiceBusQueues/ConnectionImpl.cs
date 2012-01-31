// Copyright 2011 Henrik Feldt
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
using Magnum.Extensions;
using MassTransit.Transports.ServiceBusQueues.Configuration;
using MassTransit.Transports.ServiceBusQueues.Util;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using log4net;
using MassTransit.Transports.ServiceBusQueues.Management;

namespace MassTransit.Transports.ServiceBusQueues
{
	public class ConnectionImpl :
		Connection
	{
		static readonly ILog _log = LogManager.GetLogger(typeof (ConnectionImpl));
	
		bool _disposed;

		readonly Uri _serviceUri;
		readonly TokenProvider _tokenProvider;
		readonly NamespaceManager _namespaceManager;
		readonly MessagingFactory _messagingFactory;

		QueueClient _queues;
		List<Subscriber> _subscribers = new List<Subscriber>();

		public ConnectionImpl([NotNull] Uri serviceUri, // with ServiceBusEnvironment
			TokenProvider tokenProvider,
			NamespaceManager namespaceManager,
			MessagingFactory messagingFactory)
		{
			if (serviceUri == null) throw new ArgumentNullException("serviceUri");

			_serviceUri = serviceUri;
			_tokenProvider = tokenProvider;
			_namespaceManager = namespaceManager;
			_messagingFactory = messagingFactory;
		}

		public QueueClient Queues
		{
			get { return _queues; }
		}

		public IEnumerable<Subscriber> Subscribers
		{
			get { return _subscribers; }
		}

		public void AddSubscriber([Util.NotNull] Subscriber subscriber)
		{
			if (subscriber == null) 
				throw new ArgumentNullException("subscriber");

			_subscribers.Add(subscriber);
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
					_serviceUri),
				                                  "The connection instance to AppFabric ServiceBus Queues, " +
				                                  "is already disposed and cannot be disposed twice.");
			try
			{
				Disconnect();
			}
			finally
			{
				_disposed = true;
			}
		}

		public void Connect()
		{
			Disconnect();

			// hackety
			var serverAddress = new UriBuilder("sb-queues", _serviceUri.Host, _serviceUri.Port).Uri;
			var upQueue = ConfigFactory.SetUpQueue(serverAddress.PathAndQuery);
			_queues = upQueue.Result.Item2;

			_log.Info("Connecting {0}".FormatWith(_serviceUri));
		}

		public void Disconnect()
		{
			try
			{
				if (_queues != null)
				{
					_log.Info("Disconnecting {0}".FormatWith(_serviceUri));

					_subscribers.Clear();

					//if (_stompClient.IsConnected)
					//    _stompClient.Disconnect();

					//_stompClient.Dispose();
					//_stompClient = null;
				}
			}
			catch (Exception ex)
			{
				_log.Warn("Failed to close AppFabric ServiceBus connection.", ex);
			}
		}
	}
}