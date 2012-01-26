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
using Magnum.Extensions;
using Magnum.TestFramework;
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

	/* These could be thrown:
	 * 
	 * ServerBusyException
	 * MessagingCommunicationException
	 * TimeoutException
	 * MessagingException
	 * NotSupportedException
	 * InvalidOperationException (duplicate send)
	 * MessagingEntityNotFoundException (for random things)
	 */

	[Category("Surprises")]
	public class When_sending_to_topic
	{
		string theTopic = "my.topic.there";
		A message;
		TopicClient topicClient;
		MessagingFactory mf;

		[TestFixtureSetUp]
		public void making_sure_the_topic_is_there()
		{
			mf = TestFactory.CreateMessagingFactory();
			var topic = TestFactory.CreateNamespaceManager(mf).TryCreateTopic(theTopic); // |> ignore

			// but no subscription!
			topicClient = mf.CreateTopicClient(topic.Path);

			message = TestFactory.AMessage();
		}

		[SetUp]
		public void given_a_message_sent_to_the_topic()
		{
			topicClient.Send(new BrokeredMessage(message));
			topicClient.Close();
		}

		[Test]
		public void there_should_be_no_message_received_when_never_subscribed()
		{
			// WTF: "Microsoft.ServiceBus.Messaging.MessagingEntityNotFoundException 
			//		: Messaging entity 'mt-client:Topic:mytopic|Olof Reading the News' could not be found..TrackingId:4629cd96-18fa-43ff-8bc7-83c1dddf3912_7_1,TimeStamp:1/26/2012 9:39:07 AM"
			// ???
			// wouldn't it be more prudent to make CreateSubscriptionClient TAKE A TopicDescription??
			var sc = mf.CreateSubscriptionClient(theTopic, "Olof Reading the News", ReceiveMode.PeekLock);
			var brokeredMessage = sc.Receive(2.Seconds());
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