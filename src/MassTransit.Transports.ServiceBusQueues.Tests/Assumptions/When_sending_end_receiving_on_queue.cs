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
// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using Magnum.Extensions;
using Magnum.TestFramework;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using NUnit.Framework;

namespace MassTransit.Transports.ServiceBusQueues.Tests.Assumptions
{
	public class When_getting_client_for_non_existent_topic
	{
		private MessagingFactory mf;

		[SetUp]
		public void given_a_messaging_factory()
		{
			mf = TestFactory.CreateMessagingFactory();
			var nm = TestFactory.CreateNamespaceManager(mf);
			nm.TopicExists("my.topic.here").ShouldBeFalse();
		}

		[Test]
		public void returned_topic_client_should_be_non_null()
		{
			var client = mf.CreateTopicClient("my.topic.here");
			client.ShouldNotBeNull();
		}
	}

	/* These could be thrown, wow:
	 * 
	 * ServerBusyException (please DDoS me ASAP!!)
	 * MessagingCommunicationException (???)
	 * TimeoutException (I'm feeling tired today)
	 * MessagingException (something that we cannot determine went wrong)
	 * NotSupportedException (should never happen?)
	 * InvalidOperationException (duplicate send)
	 * MessagingEntityNotFoundException (for random things such as not finding a message in the queue)
	 */

	// WTF: "Microsoft.ServiceBus.Messaging.MessagingEntityNotFoundException 
	//		: Messaging entity 'mt-client:Topic:mytopic|Olof Reading the News' could not be found..TrackingId:4629cd96-18fa-43ff-8bc7-83c1dddf3912_7_1,TimeStamp:1/26/2012 9:39:07 AM"
	// ???
	// wouldn't it be more prudent to make CreateSubscriptionClient TAKE A TopicDescription??
	[Category("NegativeTests")]
	public class Given_a_sent_message
	{
		protected A message;
		protected MessagingFactory mf;
		protected TopicDescription topic;
		protected NamespaceManager nm;

		[TestFixtureSetUp]
		public void making_sure_the_topic_is_there()
		{
			mf = TestFactory.CreateMessagingFactory();
			topic = (nm = TestFactory.CreateNamespaceManager(mf)).TryCreateTopic("my.topic.there");

			// but no subscription for topicClient
			topicClient = mf.CreateTopicClient(topic.Path);

			message = TestFactory.AMessage();
		}

		[TestFixtureTearDown]
		public void finally_close_the_client()
		{
			topicClient.Close();
			nm.TryDeleteTopic(topic);
		}

		TopicClient topicClient;

		[SetUp]
		public void given_a_message_sent_to_the_topic()
		{
			var msg = new BrokeredMessage(message);
			BeforeSend(msg);
			topicClient.Send(msg);
		}

		protected virtual void BeforeSend(BrokeredMessage msg)
		{
		}
	}

	public class When_creating_a_subscription_more_than_once_from_same_namespace_manager
	{
		
	}

	public class When_sending_but_noone_subscribed
		: Given_a_sent_message
	{
		[Test]
		public void there_should_be_no_message_received_when_never_subscribed()
		{
			var sc = mf.CreateSubscriptionClient(topic.Path, "Olof Reading the News", ReceiveMode.PeekLock);

			// this thing may throw MessagingEntityNotFoundException if there's no subscription active first,
			// furthermore, it may return null if there's a subscription, but got no messages, and this needs to be
			// added to docs.
			BrokeredMessage brokeredMessage = null;
			Assert.Throws<MessagingEntityNotFoundException>(() => brokeredMessage = sc.Receive(500.Milliseconds()));
			try
			{
				brokeredMessage.ShouldBeNull();
			}
			finally
			{
				if (brokeredMessage != null)
					brokeredMessage.Complete();
			}
		}
	}

	public class When_sending_to_topic_with_subscriber_and_tiny_lock_span
		: Given_a_sent_message
	{
		SubscriptionClient client;

		protected override void BeforeSend(BrokeredMessage msg)
		{
			var subDesc = new SubscriptionDescription(topic.Path, "Peter Svensson listens to the Radio".Replace(" ", "-"))
			{
				EnableBatchedOperations = true,
				LockDuration = 1.Milliseconds();
			};
			nm.TryCreateSubscription(subDesc);
			client = mf.CreateSubscriptionClient(topic.Path, subDesc.Name);
		}
	}

	public class When_sending_to_topic_with_subscriber
		: Given_a_sent_message
	{
		SubscriptionClient client;

		protected override void BeforeSend(BrokeredMessage msg)
		{
			var subDesc = new SubscriptionDescription(topic.Path, "Peter Svensson listens to the Radio".Replace(" ", "-"))
				{
					EnableBatchedOperations = true,
					LockDuration = 10.Seconds()
				};
			nm.TryCreateSubscription(subDesc);
			client = mf.CreateSubscriptionClient(topic.Path, subDesc.Name);
		}

		[Test]
		public void then_the_client_should_have_one_message_only()
		{
			var msg1 = client.Receive();
			var msg1B = msg1.GetBody<A>();
			msg1B.ShouldEqual(message);

			client.Receive().ShouldBeNull();
			msg1.Complete();
		}
	}

	public class When_sending_end_receiving_on_queue
	{
		Tuple<Action, QueueClient> t;
		A message;

		[SetUp]
		public void when_I_place_a_message_in_the_queue()
		{
			message = TestFactory.AMessage();
			t = TestFactory.SetUpQueue("test-queue");
			t.Item2.Send(new BrokeredMessage(message));
		}

		[Test]
		public void there_should_be_a_message_there_first_time_around_and_return_null_second_time()
		{
			var msg = t.Item2.Receive();
			msg.ShouldNotBeNull();
			try
			{
				var obj = msg.GetBody<A>();
				obj.ShouldEqual(message, "they should have the same contents");
			}
			finally
			{
				if (msg != null)
					msg.Complete();
			}

			var msg2 = t.Item2.Receive(1000.Milliseconds());
			msg2.ShouldBeNull();
		}

		[TearDown]
		public void finally_remove_queue()
		{
			t.Item1();
		}
	}
}