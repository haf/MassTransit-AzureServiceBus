using Magnum.TestFramework;
using MassTransit.Transports.AzureServiceBus.Configuration;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.AzureServiceBus.Tests.Assumptions
{
	class Should_be_possible_to_use_message_receiver_ordinarily
	{
		MessagingFactory mf;

		[Given]
		public void nsm_mf_and_topic()
		{
			var tp = TestConfigFactory.CreateTokenProvider();
			mf = TestConfigFactory.CreateMessagingFactory(tp);
		}

		[Then]
		public void Smoke()
		{
			mf.CreateSubscriptionClient("non-existing", "lukas").ShouldNotBeNull();
		}
	}
}