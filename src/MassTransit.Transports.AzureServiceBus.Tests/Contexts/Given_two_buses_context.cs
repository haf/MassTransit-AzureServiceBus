using System;
using MassTransit.TestFramework.Fixtures;
using MassTransit.Transports.AzureServiceBus.Configuration;
using NUnit.Framework;

namespace MassTransit.Transports.AzureServiceBus.Tests.Contexts
{
	[Description("aka, given two buses listening on different endpoints")]
	public abstract class Given_two_buses_context
		: TwoBusTestFixture<TransportFactoryImpl>
	{
		protected Given_two_buses_context()
		{
			LocalUri = new Uri(string.Format("azure-sb://owner:{0}@{1}/rat_hole", AccountDetails.Key, AccountDetails.Namespace));
			RemoteUri = new Uri(string.Format("azure-sb://owner:{0}@{1}/hungry_cat", AccountDetails.Key, AccountDetails.Namespace));
		}

		protected override void ConfigureLocalBus(BusConfigurators.ServiceBusConfigurator configurator)
		{
			//configurator.UseAzureServiceBusRouting();
			base.ConfigureLocalBus(configurator);
		}

		protected override void ConfigureRemoteBus(BusConfigurators.ServiceBusConfigurator configurator)
		{
			//configurator.UseAzureServiceBusRouting();
			base.ConfigureRemoteBus(configurator);
		}
	}
}