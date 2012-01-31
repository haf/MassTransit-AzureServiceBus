using Magnum.Extensions;
using Magnum.TestFramework;
using MassTransit.Transports.AzureServiceBus.Configuration;
using MassTransit.Transports.AzureServiceBus.Management;
using Microsoft.ServiceBus.Messaging;
using log4net.Config;

namespace MassTransit.Transports.AzureServiceBus.Tests.Assumptions
{
	class Should_be_possible_to_recieve_ordinarily_through_API
	{
		Topic topic;

		[Given]
		public void nsm_mf_and_topic()
		{
			BasicConfigurator.Configure();
			var tp = ConfigFactory.CreateTokenProvider();
			var mf = ConfigFactory.CreateMessagingFactory(tp);
			var nm = ConfigFactory.CreateNamespaceManager(mf, tp);
			topic = nm.TryCreateTopic(mf, "sample-topic").Result;
			topic.ShouldNotBeNull();
		}

		[Then]
		public void by_calling_BeginReceive_EndReceive()
		{
			var client = topic.CreateClient(ReceiveMode.PeekLock, autoSubscribe: false).Result;
			client.ShouldNotBeNull();
			var subscriber = client.Item1.Subscribe(topic).Result;
			subscriber.ShouldNotBeNull();
			subscriber.Item2.Receive(20.Milliseconds()).Result.ShouldBeNull();
		}
	}
}