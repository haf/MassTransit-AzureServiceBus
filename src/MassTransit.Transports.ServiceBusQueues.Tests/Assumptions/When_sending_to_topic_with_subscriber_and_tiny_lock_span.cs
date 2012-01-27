using Magnum.Extensions;
using Magnum.TestFramework;
using Microsoft.ServiceBus.Messaging;
using NUnit.Framework;

namespace MassTransit.Transports.ServiceBusQueues.Tests.Assumptions
{
	public class When_sending_to_topic_with_subscriber_and_tiny_lock_span
		: Given_a_sent_message
	{
		UnsubscribeAction unsubscribe;
		Subscriber subscriber;
		BrokeredMessage msg1;

		protected override void BeforeSend(BrokeredMessage msg)
		{
			var subDesc = new SubscriptionDescriptionImpl(topic.Description.Path, "Nils Sture listens to the Radio".Replace(" ", "-"))
				{
					EnableBatchedOperations = false,
					LockDuration = 1.Milliseconds(),
					MaxDeliveryCount = 1
				};
			var subscribe = topicClient.Subscribe(subDesc).Result;
			unsubscribe = subscribe.Item1;
			subscriber = subscribe.Item2;
		}

		[SetUp]
		public void given_we_get_first_message()
		{
			msg1 = subscriber.Receive().Result;
			var msg1B = msg1.GetBody<A>();
			msg1B.ShouldEqual(message);
		}

		[TearDown]
		public void Finally_TearDown()
		{
			if (unsubscribe != null) unsubscribe();
		}

		[Test]
		public void then_the_clients_should_receive_duplicates()
		{
			subscriber.Receive(200.Milliseconds()).Result.GetBody<A>().ShouldEqual(message);
			msg1.Complete();
		}
	}
}