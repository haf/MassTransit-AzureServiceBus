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
using System.ServiceModel.Channels;
using MassTransit.Subscriptions.Coordinator;
using MassTransit.Subscriptions.Messages;
using MassTransit.Util;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using MassTransit.Transports.ServiceBusQueues.Management;
using MassTransit.Transports.ServiceBusQueues.Internal;

namespace MassTransit.Transports.ServiceBusQueues
{
	public class TopicSubscriptionObserver
		: SubscriptionObserver
	{
		readonly TopicClient _client;
		readonly MessagingFactory _fac;
		readonly NamespaceManager _nm;
		readonly ConnectionImpl _connection;
		readonly Dictionary<Guid, UnsubscribeAction> _bindings;

		public TopicSubscriptionObserver(
			TopicClient client,
			MessagingFactory fac,
			NamespaceManager nm,
			ConnectionImpl connection
			)
		{
			_client = client;
			_fac = fac;
			_nm = nm;
			_connection = connection;
		}

		public void OnSubscriptionAdded([NotNull] SubscriptionAdded message)
		{
			if (message == null) throw new ArgumentNullException("message");

			var messageType = Type.GetType(message.MessageName);
			var messageName = new MessageName(messageType);
			var topicName = messageName.ToString();

			var t = _nm.TryCreateTopic(_fac, topicName)
				.Then(topic =>
				      _client.Subscribe(new TopicImpl(_nm, _fac, topic.Description)));
			t.Wait();

			_bindings[message.SubscriptionId] = t.Result.Item1;

			_connection.AddSubscriber(t.Result.Item2);
		}

		public void OnSubscriptionRemoved(SubscriptionRemoved message)
		{
			if (_bindings.ContainsKey(message.SubscriptionId))
			{
				_bindings[message.SubscriptionId]();
				_bindings.Remove(message.SubscriptionId);
			}
		}

		public void OnComplete()
		{
			throw new NotImplementedException();
		}
	}
}