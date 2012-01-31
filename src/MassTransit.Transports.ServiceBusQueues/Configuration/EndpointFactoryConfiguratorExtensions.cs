using System;
using MassTransit.EndpointConfigurators;

namespace MassTransit.Transports.ServiceBusQueues.Configuration
{
	public static class EndpointFactoryConfiguratorExtensions
	{
		/// <summary>
		/// Specifies that MT should be using AppFabric ServiceBus Queues.
		/// </summary>
		public static T UseServiceBusQueues<T>(this T configurator)
			where T : EndpointFactoryConfigurator
		{
			return UseServiceBusQueues(configurator, x => { });
		}

		/// <summary>
		/// Specifies that MT should be using AppFabric ServiceBus Queues
		/// and allows you to configure custom settings.
		/// </summary>
		public static T UseServiceBusQueues<T>(this T configurator, Action<ServiceBusQueuesFactoryConfigurator> configure)
			where T : EndpointFactoryConfigurator
		{
			var tfacCfg = new ServiceBusQueuesFactoryConfiguratorImpl();

			configure(tfacCfg);

			configurator.AddTransportFactory(tfacCfg.Build);

			return configurator;
		}
	}
}