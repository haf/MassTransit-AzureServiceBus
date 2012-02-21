using Magnum.Extensions;
using Magnum.TestFramework;
using MassTransit.Transports.AzureServiceBus.Management;
using MassTransit.Transports.AzureServiceBus.Tests.Framework;

namespace MassTransit.Transports.AzureServiceBus.Tests.Assumptions
{
	[Scenario, Integration]
	class Should_be_possible_to_recieve
	{
		Topic topic;

		[Given]
		public void nsm_mf_and_topic()
		{
			var tp = TestConfigFactory.CreateTokenProvider();
			var mf = TestConfigFactory.CreateMessagingFactory(tp);
			var nm = TestConfigFactory.CreateNamespaceManager(mf, tp);
			topic = nm.TryCreateTopic(mf, "sample-topic").Result;
			topic.ShouldNotBeNull();
		}

		[Then]
		public void by_calling_BeginReceive_EndReceive()
		{
			var client = topic.CreateSubscriber().Result;
			client.ShouldNotBeNull();
			var subscriber = client.Item1.Subscribe(topic).Result;
			subscriber.ShouldNotBeNull();
			subscriber.Receive(20.Milliseconds()).Result.ShouldBeNull();
		}
	}
}