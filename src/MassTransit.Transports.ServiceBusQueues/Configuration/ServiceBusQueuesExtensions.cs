using MassTransit.EndpointConfigurators;

namespace MassTransit.Transports.AzureQueue.Configuration
{
	public static class ServiceBusQueuesExtensions
	{
		/// <summary>
		/// Specifies that MT should be using AppFabric ServiceBus Queues.
		/// </summary>
		public static T UseAppFabricServiceBusQueues<T>(this T configurator)
			where T : EndpointFactoryConfigurator
		{
			var transportFactoryConfigurator = new ServiceBusQueuesFactoryConfigurator();

			//configurator.AddTransportFactory(transportFactoryConfigurator.Build);
			configurator.AddTransportFactory(new ServiceBusQueueTransportFactory());
			
			configurator.UseJsonSerializer();

			return configurator;
		}
	}
}