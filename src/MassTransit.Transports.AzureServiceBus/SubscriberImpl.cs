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
using System.Threading.Tasks;
using MassTransit.Logging;
using MassTransit.Transports.AzureServiceBus.Util;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.AzureServiceBus
{
	public class SubscriberImpl
		: Subscriber
	{
		static readonly ILog _logger = Logger.Get(typeof (SubscriberImpl));

		bool _disposed;
		readonly SubscriptionClient _client;
		readonly UnsubscribeAction _action;
		readonly Guid _subscriptionId;

		public SubscriberImpl([NotNull] SubscriptionClient client,
		                      Guid subscriptionId, [NotNull] UnsubscribeAction action)
		{
			if (client == null) throw new ArgumentNullException("client");
			if (action == null) throw new ArgumentNullException("action");

			_client = client;
			_subscriptionId = subscriptionId;
			_action = action;
		}

		public void Dispose()
		{
			Dispose(true);
		}

		void Dispose(bool managed)
		{
			if (!managed)
				return;

			if (_disposed)
				throw new ObjectDisposedException("SubscriberImpl");

			try
			{
				_client.Close();
				_action().Wait();
			}
			finally
			{
				_disposed = true;
			}
		}

		public Guid SubscriptionId
		{
			get { return _subscriptionId; }
		}

		public Task<BrokeredMessage> Receive()
		{
			_logger.Debug("begin receive");
			return Task.Factory.FromAsync<BrokeredMessage>(
				_client.BeginReceive,
				_client.EndReceive, null)
				.ContinueWith(tRec =>
					{
						_logger.Debug("end receive");
						return tRec.Result;
					});
		}

		public Task<BrokeredMessage> Receive(TimeSpan timeout)
		{
			_logger.Debug(string.Format("begin receive w/ timespan {0}", timeout));
			return Task.Factory.FromAsync<TimeSpan, BrokeredMessage>(
				_client.BeginReceive,
				_client.EndReceive, timeout, null)
				.ContinueWith(tRec =>
					{
						_logger.Debug("end receive w/ timespan");
						return tRec.Result;
					});
		}
	}
}