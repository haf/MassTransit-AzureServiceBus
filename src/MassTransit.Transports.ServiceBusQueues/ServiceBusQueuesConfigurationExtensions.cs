using MassTransit.BusConfigurators;

namespace MassTransit.Transports.AzureQueue
{
	public static class ServiceBusQueuesConfigurationExtensions
	{
		public static ServiceBusConfigurator UseAzureQueue(this ServiceBusConfigurator configurator)
		{
			configurator.UseJsonSerializer();
			return configurator.AddTransportFactory<ServiceBusQueueTransportFactory>();
		}
	}
}