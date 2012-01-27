using Magnum.Extensions;
using Magnum.TestFramework;
using MassTransit.Transports.ServiceBusQueues.Internal;
using Microsoft.ServiceBus.Messaging;
using log4net.Config;

namespace MassTransit.Transports.ServiceBusQueues.Tests.Assumptions
{
	class Should_be_possible_to_recieve_ordinarily_through_API
	{
		Topic topic;

		[Given]
		public void nsm_mf_and_topic()
		{
			BasicConfigurator.Configure();
			var tp = TestFactory.CreateTokenProvider();
			var mf = TestFactory.CreateMessagingFactory(tp);
			var nm = TestFactory.CreateNamespaceManager(mf, tp);
			topic = nm.TryCreateTopic(mf, "sample-topic").Result;
			topic.ShouldNotBeNull();
		}

		[Then]
		public void by_calling_BeginReceive_EndReceive()
		{
			var client = topic.CreateClient(ReceiveMode.PeekLock, autoSubscribe: false).Result;
			client.ShouldNotBeNull();
			var subscriber = client.Item1.Subscribe().Result;
			subscriber.ShouldNotBeNull();
			subscriber.Item2.Receive(20.Milliseconds()).Result.ShouldBeNull();
		}
	}
}