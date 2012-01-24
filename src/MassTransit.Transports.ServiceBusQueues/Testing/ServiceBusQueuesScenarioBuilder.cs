using System;
using MassTransit.Testing.ScenarioBuilders;

namespace MassTransit.Transports.AzureQueue.Testing
{
	public class ServiceBusQueuesScenarioBuilder
		: BusScenarioBuilderImpl
	{
		protected ServiceBusQueuesScenarioBuilder(Uri uri) : base(uri)
		{
		}
	}
}