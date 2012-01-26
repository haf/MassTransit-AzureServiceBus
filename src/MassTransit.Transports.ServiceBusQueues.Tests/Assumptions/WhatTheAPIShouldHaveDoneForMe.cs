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
using System.Threading.Tasks;
using Magnum.Policies;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Magnum.Extensions;
using log4net;

namespace MassTransit.Transports.ServiceBusQueues.Tests.Assumptions
{
	public static class WhatTheAPIShouldHaveDoneForMe
	{
		static readonly ILog _logger = LogManager.GetLogger(typeof (WhatTheAPIShouldHaveDoneForMe));

		public static Task TryDeleteTopic(this NamespaceManager nsm, TopicDescription topic)
		{
			var exists = Task.Factory.FromAsync<string, bool>(nsm.BeginTopicExists, nsm.EndTopicExists, topic.Path, null);
			return exists.ContinueWith<Task>((Task<bool> task) =>
				{
					try
					{
						if (task.Result)
							// according to documentation this thing doesn't throw anything??!?!??!?!
							return Task.Factory.FromAsync(nsm.BeginDeleteTopic, nsm.EndDeleteTopic, topic.Path, null);
					}
					catch (MessagingEntityNotFoundException)
					{
					}
					return Task.Factory.StartNew(() => { });
				});
		}

		public static Task<TopicClient> TryCreateTopicClient(this MessagingFactory messagingFactory,
			NamespaceManager nm,
			Topic topic)
		{
			var timeoutPolicy = ExceptionPolicy
				.InCaseOf<TimeoutException>()
				.CircuitBreak(50.Milliseconds(), 10);
			
			while (true)
				try
				{
					return timeoutPolicy.Do(() =>
						{
							// where is the BeginCreateTopicClient???!??!?!?!?!?!
							return Task.Factory.StartNew<TopicClient>(() =>
								// missing BeginXXX method, this is synchronous equivalent!!
								new TopicClientImpl(messagingFactory.CreateTopicClient(topic.Description.Path), nm, topic));
						});
				}
				catch (TimeoutException ex)
				{
					_logger.Error("could not create topic in time", ex);
				}
		}

		public static QueueDescription TryCreateQueue(this NamespaceManager nsm, string queueName)
		{
			while (true)
			{
				try
				{
					//if (nsm.GetQueue(queueName) == null) 
					// bugs out http://social.msdn.microsoft.com/Forums/en-US/windowsazureconnectivity/thread/6ce20f60-915a-4519-b7e3-5af26fc31e35
					// says it'll give null, but throws!
					if (!nsm.QueueExists(queueName))
						return nsm.CreateQueue(queueName);
				}
				catch (MessagingEntityAlreadyExistsException)
				{
				}
				try
				{
					return nsm.GetQueue(queueName);
				}
				catch (MessagingEntityNotFoundException)
				{
				}
			}
		}

		/// <returns> the topic description </returns>
		public static Topic TryCreateTopic(this NamespaceManager nm,
			MessagingFactory factory,
			string topicName)
		{
			while (true)
			{
				try
				{
					if (!nm.TopicExists(topicName))
						return new TopicImpl(nm, factory, nm.CreateTopic(topicName));
				}
				catch (MessagingEntityAlreadyExistsException)
				{
				}
				try
				{
					return new TopicImpl(nm, factory, nm.GetTopic(topicName));
				}
				catch (MessagingEntityNotFoundException) // someone beat us to removing it
				{
				}
			}
		}

		/// <summary>
		/// Tries to create a new subscription
		/// </summary>
		/// <param name="nm"></param>
		/// <param name="description"></param>
		public static void TryCreateSubscription(this NamespaceManager nm, SubscriptionDescription description)
		{
			if (nm.SubscriptionExists(description.TopicPath, description.Name))
				return;

			// does this really never throw (doc says no)??
			nm.CreateSubscription(description); // |> ignore - why do we get another desc a result?

			/* here's on throw - this should be documented!
Test 'MassTransit.Transports.ServiceBusQueues.Tests.Assumptions.When_sending_to_topic_with_subscriber.When_sending_to_topic.there_should_be_no_message_received_when_never_subscribed' failed:
	SetUp : Microsoft.ServiceBus.Messaging.MessagingEntityAlreadyExistsException : The remote server returned an error: (409) Conflict. mt-client:Topic:my.topic.there|Peter-Svensson-listens-to-the-Radio.TrackingId:78a7c7f5-1eb2-4b59-b088-58c56df6ceec_7_1, Timestamp:1/26/2012 3:30:24 PM
  ----> System.Net.WebException : The remote server returned an error: (409) Conflict.
	
	Server stack trace:
	
	
	Exception rethrown at [0]:
	at Microsoft.ServiceBus.Common.AsyncResult.End[TAsyncResult](IAsyncResult result)
	at Microsoft.ServiceBus.NamespaceManager.CreateSubscriptionAsyncResult.OnCreateSubscription(IAsyncResult result)
	at Microsoft.ServiceBus.Common.AsyncResult.AsyncCompletionWrapperCallback(IAsyncResult result)
	
	Exception rethrown at [1]:
	at Microsoft.ServiceBus.Common.AsyncResult.End[TAsyncResult](IAsyncResult result)
	at Microsoft.ServiceBus.NamespaceManager.CreateSubscription(SubscriptionDescription description)*/
		}
	}
}