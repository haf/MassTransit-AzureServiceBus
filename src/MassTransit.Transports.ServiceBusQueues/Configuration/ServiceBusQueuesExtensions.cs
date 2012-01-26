using System;
using Magnum.Extensions;
using MassTransit.BusConfigurators;
using MassTransit.EndpointConfigurators;

namespace MassTransit.Transports.ServiceBusQueues.Configuration
{
	public static class ServiceBusQueuesExtensions
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


		/// <summary>
		/// Specifies that MT should be using AppFabric ServiceBus Queues.
		/// </summary>
		public static T UseServiceBusQueues<T>(this T configurator)
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