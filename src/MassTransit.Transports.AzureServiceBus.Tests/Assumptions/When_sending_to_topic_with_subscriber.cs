using Magnum.Extensions;
using Magnum.TestFramework;
using Microsoft.ServiceBus.Messaging;
using NUnit.Framework;
using log4net;

namespace MassTransit.Transports.AzureServiceBus.Tests.Assumptions
{
	public class When_sending_to_topic_with_subscriber
		: Given_a_sent_message
	{
		static readonly ILog _logger = LogManager.GetLogger(typeof (When_sending_to_topic_with_subscriber));
		
		UnsubscribeAction unsubscribe;
		Subscriber subscriber;

		protected override void BeforeSend(BrokeredMessage msg)
		{
			_logger.Debug("[BeforeSend] subscribing new client");
			var awaitSub = topicClient.Subscribe(
				topic,
				new SubscriptionDescriptionImpl(topic.Description.Path, "Peter Svensson listens to the Radio".Replace(" ", "-"))
				{
					EnableBatchedOperations = true,
					LockDuration = 10.Seconds()
				});

			awaitSub.Wait();

			unsubscribe = awaitSub.Result.Item1;
			subscriber = awaitSub.Result.Item2;
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

		[TearDown]
		public void unsubscribe_subscriber()
		{
			if (unsubscribe != null)
				unsubscribe();
		}
	}
}