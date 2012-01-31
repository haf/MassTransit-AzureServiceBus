using System;
using MassTransit.Testing.ScenarioBuilders;

namespace MassTransit.Transports.AzureServiceBus.Testing
{
	public class AzureServiceBusScenarioBuilder
		: BusScenarioBuilderImpl
	{
		protected AzureServiceBusScenarioBuilder(Uri uri) : base(uri)
		{
		}
	}
}