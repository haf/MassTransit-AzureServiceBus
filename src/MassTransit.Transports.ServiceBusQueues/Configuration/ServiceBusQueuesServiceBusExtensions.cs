using System;
using Magnum.Extensions;
using MassTransit.BusConfigurators;

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
				{
					var inboundTransport = sb.Endpoint.InboundTransport.CastAs<InboundTransportImpl>();
					return new TopicSubscriptionObserver(inboundTransport.Address.CastAs<ServiceBusQueuesEndpointAddress>());
				});

			configurator.UseServiceBusQueues();

			return configurator;
		}
	}
}