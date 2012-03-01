using MassTransit.AzureServiceBus.Util;
using MassTransit.Testing.ScenarioBuilders;
using MassTransit.Transports.AzureServiceBus.Configuration;

namespace MassTransit.Testing
{
	public class AzureServiceBusScenarioBuilder
		: BusScenarioBuilderImpl
	{
		public AzureServiceBusScenarioBuilder([NotNull] PreSharedKeyCredentials credentials)
			: base(credentials.BuildUri())
		{
			ConfigureEndpointFactory(x =>
				{
					x.UseAzureServiceBus();
				});

			ConfigureBus(x =>
				{
					x.UseAzureServiceBusRouting();
				});
		}
	}
}