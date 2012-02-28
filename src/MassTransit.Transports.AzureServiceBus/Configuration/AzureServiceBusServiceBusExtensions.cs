using Magnum.Extensions;
using MassTransit.AzureServiceBus;
using MassTransit.BusConfigurators;

namespace MassTransit.Transports.AzureServiceBus.Configuration
{
	public static class AzureServiceBusServiceBusExtensions
	{
		/// <summary>
		/// Specifies that MT should be using AppFabric ServiceBus Queues to receive messages and specifies the
		/// uri by means of its components.
		/// </summary>
		public static void ReceiveFromComponents<T>(this T configurator, 
			string issuerOrUsername, 
			string defaultKeyOrPassword, string serviceBusNamespace,
			string application)
			where T : ServiceBusConfigurator
		{
			var credentials = new Credentials(issuerOrUsername, defaultKeyOrPassword, serviceBusNamespace, application);
			configurator.ReceiveFrom(credentials.BuildUri());
		}

		public static void ReceiveFromComponents<T>(this T configurator,
			PreSharedKeyCredentials creds)
			where T : ServiceBusConfigurator
		{
			configurator.ReceiveFrom(creds.BuildUri());
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
					return new TopicSubscriptionObserver(inboundTransport.Address.CastAs<AzureServiceBusEndpointAddress>(),
						inboundTransport.MessageNameFormatter,
						inboundTransport);
				});

			configurator.UseAzureServiceBus();

			return configurator;
		}
	}
}