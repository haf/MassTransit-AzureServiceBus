using Magnum.Extensions;
using Magnum.TestFramework;
using Microsoft.ServiceBus.Messaging;
using NUnit.Framework;

namespace MassTransit.Transports.ServiceBusQueues.Tests.Assumptions
{
	public class When_sending_to_topic_with_subscriber
		: Given_a_sent_message
	{
		UnsubscribeAction unsubscribe;
		Subscriber subscriber;
		BrokeredMessage msg1;

		protected override void BeforeSend(BrokeredMessage msg)
		{
			var subDesc = new SubscriptionDescription(topic.Description.Path, "Peter Svensson listens to the Radio".Replace(" ", "-"))
				{
					EnableBatchedOperations = true,
					LockDuration = 10.Seconds()
				};
			var subscribe = topicClient.Subscribe(subDesc).Result;
			unsubscribe = subscribe.Item1;
			subscriber = subscribe.Item2;
		}

		[Test]
		public void then_the_client_should_have_one_message_only()
		{
			var msg1 = subscriber.Receive().Result;
			var msg1B = msg1.GetBody<A>();
			msg1B.ShouldEqual(message);

			subscriber.Receive().Result.ShouldBeNull();
			msg1.Complete();
		}
	}
}