using System;
using MassTransit.TestFramework.Fixtures;
using MassTransit.Transports.AzureServiceBus.Configuration;
using log4net.Config;

namespace MassTransit.Transports.AzureServiceBus.Tests
{
	public abstract class given_two_buses
		: TwoBusTestFixture<TransportFactoryImpl>
	{
		protected given_two_buses()
		{
			BasicConfigurator.Configure();
			LocalUri = new Uri(string.Format("azure-sb://owner:{0}@{1}/local_bus", AccountDetails.Key, AccountDetails.Namespace));
			RemoteUri = new Uri(string.Format("azure-sb://owner:{0}@{1}/remote_bus", AccountDetails.Key, AccountDetails.Namespace));
		}
	}
}