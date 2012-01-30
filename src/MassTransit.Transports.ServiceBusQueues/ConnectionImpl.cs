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
using Magnum.Extensions;
using MassTransit.Transports.ServiceBusQueues.Utils;
using Microsoft.ServiceBus.Messaging;
using log4net;

namespace MassTransit.Transports.ServiceBusQueues
{
	public class ConnectionImpl :
		Connection
	{
		static readonly ILog _log = LogManager.GetLogger(typeof (ConnectionImpl));
		readonly Uri _serviceUri;
		bool _disposed;
		object _someClient;
		QueueClient _queues;
		readonly TopicClient _topics;

		public ConnectionImpl([NotNull] Uri serviceUri)
		{
			if (serviceUri == null) throw new ArgumentNullException("serviceUri");
			_serviceUri = serviceUri;
		}

		public QueueClient Queues
		{
			get { return _queues; }
		}

		public TopicClient Topics
		{
			get { return _topics; }
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

			var serverAddress = new UriBuilder("ws", _serviceUri.Host, _serviceUri.Port).Uri;

			_log.Info("Connecting {0}".FormatWith(_serviceUri));
		}

		public void Disconnect()
		{
			try
			{
				if (_someClient != null)
				{
					_log.Info("Disconnecting {0}".FormatWith(_serviceUri));

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