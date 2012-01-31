using Magnum.TestFramework;
using MassTransit.Transports.ServiceBusQueues.Configuration;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.ServiceBusQueues.Tests.Assumptions
{
	class Should_be_possible_to_use_message_receiver_ordinarily
	{
		MessagingFactory mf;

		[Given]
		public void nsm_mf_and_topic()
		{
			var tp = ConfigFactory.CreateTokenProvider();
			mf = ConfigFactory.CreateMessagingFactory(tp);
		}

		[Then]
		public void Smoke()
		{
			mf.CreateSubscriptionClient("non-existing", "lukas").ShouldNotBeNull();
		}
	}
}