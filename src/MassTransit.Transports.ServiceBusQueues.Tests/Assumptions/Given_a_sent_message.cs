using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using NUnit.Framework;

namespace MassTransit.Transports.ServiceBusQueues.Tests.Assumptions
{
	[Category("NegativeTests")]
	public class Given_a_sent_message
	{
		protected A message;
		protected NamespaceManager nm;
		protected Topic topic;
		protected TopicClient topicClient;

		[TestFixtureSetUp]
		public void making_sure_the_topic_is_there()
		{
			message = TestFactory.AMessage();

			var mf = TestFactory.CreateMessagingFactory();
			nm = TestFactory.CreateNamespaceManager(mf);
			topic = nm.TryCreateTopic(mf, "my.topic.here");

			var client = topic.CreateClient();
			topic.Drain().Wait();
			topicClient = client.Result.Item1;
		}

		[TestFixtureTearDown]
		public void finally_close_the_client()
		{
			topic.Delete().Wait();
			topicClient.Dispose();
		}

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
}