using MassTransit.AzureServiceBus.Util;
using MassTransit.Testing.ScenarioBuilders;
using MassTransit.Transports.AzureServiceBus.Configuration;

#pragma warning disable 1591

namespace MassTransit.Testing
{
	public class AzureServiceBusScenarioBuilder
		: BusScenarioBuilderImpl
	{
		public AzureServiceBusScenarioBuilder([NotNull] PreSharedKeyCredentials credentials)
			: base(credentials.BuildUri())
		{
			ConfigureEndpointFactory(x => x.UseAzureServiceBus());
			ConfigureBus(x => x.UseAzureServiceBusRouting());
		}
	}
}