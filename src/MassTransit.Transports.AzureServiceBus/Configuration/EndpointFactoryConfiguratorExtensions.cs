using System;
using MassTransit.EndpointConfigurators;
using MassTransit.Util;

#pragma warning disable 1591

namespace MassTransit.Transports.AzureServiceBus.Configuration
{
	public static class EndpointFactoryConfiguratorExtensions
	{
		/// <summary>
		/// Specifies that MT should be using AppFabric ServiceBus Queues.
		/// </summary>
		public static T UseAzureServiceBus<T>(this T configurator)
			where T : EndpointFactoryConfigurator
		{
			return UseAzureServiceBus(configurator, x => { });
		}

		/// <summary>
		/// Specifies that MT should be using AppFabric ServiceBus Queues
		/// and allows you to configure custom settings.
		/// </summary>
		public static T UseAzureServiceBus<T>(this T configurator, 
			[NotNull] Action<AzureServiceBusFactoryConfigurator> configure)
			where T : EndpointFactoryConfigurator
		{
			if (configure == null) throw new ArgumentNullException("configure");

			var tfacCfg = new AzureAzureServiceBusFactoryConfiguratorImpl();

			configure(tfacCfg);

			configurator.AddTransportFactory(tfacCfg.Build);

			configurator.UseJsonSerializer();

			return configurator;
		}
	}
}