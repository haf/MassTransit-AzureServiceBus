using Magnum.TestFramework;
using MassTransit.Transports.AzureServiceBus.Configuration;
using Microsoft.ServiceBus.Messaging;
using NUnit.Framework;

namespace MassTransit.Transports.AzureServiceBus.Tests.Assumptions
{
	public class When_getting_client_for_non_existent_topic
	{
		private MessagingFactory mf;

		[SetUp]
		public void given_a_messaging_factory()
		{
			mf = TestConfigFactory.CreateMessagingFactory();
			var nm = TestConfigFactory.CreateNamespaceManager(mf);

			nm.TopicExists("my.topic.here").ShouldBeFalse();
		}

		[Test]
		public void returned_topic_client_should_be_non_null()
		{
			var client = mf.CreateTopicClient("my.topic.here");
			client.ShouldNotBeNull();
		}
	}
}