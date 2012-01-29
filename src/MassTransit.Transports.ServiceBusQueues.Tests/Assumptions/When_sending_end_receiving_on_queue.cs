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
using System.Threading.Tasks;
using Magnum.Extensions;
using Magnum.TestFramework;
using Microsoft.ServiceBus.Messaging;
using NUnit.Framework;

namespace MassTransit.Transports.ServiceBusQueues.Tests.Assumptions
{
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

	// value object

	// WTF: "Microsoft.ServiceBus.Messaging.MessagingEntityNotFoundException 
	//		: Messaging entity 'mt-client:Topic:mytopic|Olof Reading the News' could not be found..TrackingId:4629cd96-18fa-43ff-8bc7-83c1dddf3912_7_1,TimeStamp:1/26/2012 9:39:07 AM"
	// ???
	// wouldn't it be more prudent to make CreateSubscriptionClient TAKE A TopicDescription??

	public class When_sending_end_receiving_on_queue
	{
		Tuple<DeleteQueueAction, QueueClient> t;
		A message;

		[SetUp]
		public void when_I_place_a_message_in_the_queue()
		{
			message = ConfigFactory.AMessage();
			var setup = ConfigFactory.SetUpQueue("test-queue");
			setup.Wait();
			t = setup.Result;
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
			t.Item1().Wait();
		}
	}
}