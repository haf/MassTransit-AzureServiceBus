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
using System.Threading.Tasks;
using MassTransit.Subscriptions.Coordinator;
using MassTransit.Subscriptions.Messages;
using MassTransit.Transports.AzureServiceBus.Management;
using MassTransit.Transports.AzureServiceBus.Util;
using log4net;

namespace MassTransit.Transports.AzureServiceBus
{
	/// <summary>
	/// 	Monitors the subscriptions from the local bus and subscribes the topics with topic clients when subscriptions occurr: when they do; create the appropriate topics for them.
	/// </summary>
	public class TopicSubscriptionObserver
		: SubscriptionObserver
	{
		static readonly ILog _logger = LogManager.GetLogger(typeof (TopicSubscriptionObserver));

		readonly AzureServiceBusEndpointAddress _address;
		readonly Dictionary<Guid, Topic> _bindings;

		public TopicSubscriptionObserver(
			[NotNull] AzureServiceBusEndpointAddress address)
		{
			if (address == null) throw new ArgumentNullException("address");
			_address = address;
			_bindings = new Dictionary<Guid, Topic>();
		}

		public void OnSubscriptionAdded(SubscriptionAdded message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			_logger.Debug(string.Format("subscription added: '{0}'", message));

			var messageName = GetMessageName(message);
			var topicName = messageName.ToString();

			var t = _address.NamespaceManager.TryCreateTopic(_address.MessagingFactory, topicName);
			t.Wait();

			_bindings[message.SubscriptionId] = t.Result;
		}

		public void OnSubscriptionRemoved(SubscriptionRemoved message)
		{
			_logger.Debug(string.Format("subscription removed: '{0}'", message));

			var messageName = GetMessageName(message);

			if (_bindings.ContainsKey(message.SubscriptionId))
			{
				_logger.Debug(string.Format("cannot remove topic {0} because we don't know who consumes off of it",
				                            messageName));

				_bindings.Remove(message.SubscriptionId);
			}
		}

		static MessageName GetMessageName(Subscriptions.Messages.Subscription message)
		{
			var messageType = Type.GetType(message.MessageName);
			var messageName = new MessageName(messageType);
			return messageName;
		}

		public void OnComplete()
		{
		}
	}
}