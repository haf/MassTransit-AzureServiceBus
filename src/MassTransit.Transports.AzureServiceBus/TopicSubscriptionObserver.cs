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
using MassTransit.AzureServiceBus;
using MassTransit.Logging;
using MassTransit.Subscriptions.Coordinator;
using MassTransit.Subscriptions.Messages;
using MassTransit.Transports.AzureServiceBus.Util;
using Magnum.Extensions;

namespace MassTransit.Transports.AzureServiceBus
{
	/// <summary>
	/// 	Monitors the subscriptions from the local bus and subscribes the topics with topic clients when subscriptions occur: when they do; create the appropriate topics for them.
	/// </summary>
	public class TopicSubscriptionObserver
		: SubscriptionObserver
	{
		static readonly ILog _logger = Logger.Get(typeof (TopicSubscriptionObserver));

		readonly IMessageNameFormatter _formatter;
		readonly InboundTransportImpl _inboundTransport;
		readonly Dictionary<Guid, TopicDescription> _bindings;

		public TopicSubscriptionObserver(
			[NotNull] IMessageNameFormatter formatter,
			[NotNull] InboundTransportImpl inboundTransport)
		{
			if (formatter == null) throw new ArgumentNullException("formatter");
			if (inboundTransport == null) throw new ArgumentNullException("inboundTransport");

			_formatter = formatter;
			_inboundTransport = inboundTransport;

			_bindings = new Dictionary<Guid, TopicDescription>();
		}

		public void OnSubscriptionAdded(SubscriptionAdded message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			var messageName = GetMessageName(message);
			_bindings[message.SubscriptionId] = new TopicDescriptionImpl(messageName.ToString());
			_bindings.Each(kv => _inboundTransport.SignalBoundSubscription(kv.Key /* subId */, kv.Value /* topic desc */));
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
				_bindings.Each(kv => _inboundTransport.SignalUnboundSubscription(kv.Key /* subId */, kv.Value /* topic desc */));
			}
		}

		MessageName GetMessageName(Subscription message)
		{
			var messageType = Type.GetType(message.MessageName);
			return _formatter.GetMessageName(messageType);
		}

		public void OnComplete()
		{
		}
	}
}