using System;
using Magnum.Extensions;
using MassTransit.BusConfigurators;
using MassTransit.EndpointConfigurators;

namespace MassTransit.Transports.ServiceBusQueues.Configuration
{
	public static class ServiceBusQueuesServiceBusExtensions
	{
		/// <summary>
		/// Specifies that MT should be using AppFabric ServiceBus Queues to receive messages and specifies the
		/// uri.
		/// </summary>
		public static T ReceiveFrom<T>(this T configurator,
			string issuerOrUsername, string defaultKeyOrPassword,
			string serviceBusNamespace, string applicationName)
			where T : ServiceBusConfigurator
		{
			configurator.ReceiveFrom(
				new Uri("sb-queues://{0}:{1}@{2}/{3}".FormatWith(issuerOrUsername, 
					defaultKeyOrPassword, serviceBusNamespace,
				    applicationName)));

			return configurator;
		}

		public static T UseServiceBusQueuesRouting<T>(this T configurator)
			where T : ServiceBusConfigurator
		{
			configurator.UseJsonSerializer();
			
			configurator.SetSubscriptionObserver((sb, router) =>
				new TopicSubscriptionObserver(sb.Endpoint.InboundTransport.Address.CastAs<ServiceBusQueuesAddress>()));

			configurator.UseServiceBusQueues();

			return configurator;
		}
	}

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