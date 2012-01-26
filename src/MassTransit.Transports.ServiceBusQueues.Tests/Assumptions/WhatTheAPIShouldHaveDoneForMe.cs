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

using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.ServiceBusQueues.Tests.Assumptions
{
	public static class WhatTheAPIShouldHaveDoneForMe
	{
		public static void TryDeleteTopic(this NamespaceManager nsm, TopicDescription topic)
		{
			try
			{
				if (nsm.TopicExists(topic.Path))
					// according to documentation this thing doesn't throw anything??!?!??!?!
					nsm.DeleteTopic(topic.Path);
			}
			catch (MessagingEntityNotFoundException)
			{
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
		public static TopicDescription TryCreateTopic(this NamespaceManager nm, string topicName)
		{
			while (true)
			{
				try
				{
					if (!nm.TopicExists(topicName))
						return nm.CreateTopic(topicName);
				}
				catch (MessagingEntityAlreadyExistsException)
				{
				}
				try
				{
					return nm.GetTopic(topicName);
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