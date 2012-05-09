using Magnum.TestFramework;
using MassTransit.Transports.AzureServiceBus.Receiver;
using MassTransit.Transports.AzureServiceBus.Management;
using MassTransit.Transports.AzureServiceBus.Tests.Framework;

namespace MassTransit.Transports.AzureServiceBus.Tests
{
	[Scenario, Integration]
	public class Azure_management_impl_spec
	{
		AzureServiceBusEndpointAddress address;

		[When]
		public void management_purges_queue()
		{
			address = TestDataFactory.GetAddress();
			
			new PurgeImpl(true, address)
				.Purge()
				.Wait();
		}

		[Then]
		public void it_exists_afterwards()
		{
			TestConfigFactory.CreateNamespaceManager(TestConfigFactory.CreateMessagingFactory())
				.ExistsAsync(address.QueueDescription)
				.Result
				.ShouldBeTrue();
		}
	}
}