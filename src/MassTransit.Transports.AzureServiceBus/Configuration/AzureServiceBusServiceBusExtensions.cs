using System;
using Magnum.Extensions;
using MassTransit.BusConfigurators;

namespace MassTransit.Transports.AzureServiceBus.Configuration
{
	public static class AzureServiceBusServiceBusExtensions
	{
		/// <summary>
		/// Specifies that MT should be using AppFabric ServiceBus Queues to receive messages and specifies the
		/// uri by means of its components.
		/// </summary>
		public static T ReceiveFromComponents<T>(this T configurator,
			string issuerOrUsername, string defaultKeyOrPassword,
			string serviceBusNamespace, string applicationName)
			where T : ServiceBusConfigurator
		{
			configurator.ReceiveFrom(
				new Uri("azure-sb://{0}:{1}@{2}/{3}".FormatWith(issuerOrUsername, 
					defaultKeyOrPassword, serviceBusNamespace,
				    applicationName)));

			return configurator;
		}

		/// <summary>
		/// Configure the service bus to use the queues and topics routing semantics with
		/// Azure ServiceBus.
		/// </summary>
		public static T UseAzureServiceBusRouting<T>(this T configurator)
			where T : ServiceBusConfigurator
		{
			configurator.UseJsonSerializer();
			
			configurator.SetSubscriptionObserver((sb, router) =>
				{
					var inboundTransport = sb.Endpoint.InboundTransport.CastAs<InboundTransportImpl>();
					return new TopicSubscriptionObserver(inboundTransport.Address.CastAs<AzureServiceBusEndpointAddress>());
				});

			configurator.UseAzureServiceBus();

			return configurator;
		}
	}
}