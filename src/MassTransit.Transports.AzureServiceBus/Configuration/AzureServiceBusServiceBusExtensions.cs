using System;
using Magnum.Extensions;
using MassTransit.AzureServiceBus.Util;
using MassTransit.BusConfigurators;
using MassTransit.Pipeline.Configuration;

#pragma warning disable 1591

namespace MassTransit.Transports.AzureServiceBus.Configuration
{
	public static class AzureServiceBusServiceBusExtensions
	{
		/// <summary>
		/// Specifies that MT should be using AppFabric ServiceBus Queues to receive messages and specifies the
		/// uri by means of its components.
		/// </summary>
		public static void ReceiveFromComponents<T>(this T configurator, 
			[NotNull] string issuerOrUsername, 
			[NotNull] string defaultKeyOrPassword, 
			[NotNull] string serviceBusNamespace, 
			[NotNull] string application)
			where T : ServiceBusConfigurator
		{
			if (issuerOrUsername == null) throw new ArgumentNullException("issuerOrUsername");
			if (defaultKeyOrPassword == null) throw new ArgumentNullException("defaultKeyOrPassword");
			if (serviceBusNamespace == null) throw new ArgumentNullException("serviceBusNamespace");
			if (application == null) throw new ArgumentNullException("application");
			var credentials = new Credentials(issuerOrUsername, defaultKeyOrPassword, serviceBusNamespace, application);
			configurator.ReceiveFrom(credentials.BuildUri());
		}

		/// <summary>
		/// Configure MassTransit to consume from Azure Service Bus.
		/// </summary>
		public static void ReceiveFromComponents<T>(this T configurator, 
			[NotNull] PreSharedKeyCredentials creds)
			where T : ServiceBusConfigurator
		{
			if (creds == null) throw new ArgumentNullException("creds");
			configurator.ReceiveFrom(creds.BuildUri());
		}

		/// <summary>
		/// Configure the service bus to use the queues and topics routing semantics with
		/// Azure ServiceBus.
		/// </summary>
		public static void UseAzureServiceBusRouting<T>(this T configurator)
			where T : ServiceBusConfigurator
		{
			configurator.SetSubscriptionObserver((sb, router) =>
				{
					var inboundTransport = sb.Endpoint.InboundTransport.CastAs<InboundTransportImpl>();
					return new TopicSubscriptionObserver(inboundTransport.MessageNameFormatter, inboundTransport);
				});

			var busConf = new PostCreateBusBuilderConfigurator(bus =>
				{
					var interceptorConf = new OutboundMessageInterceptorConfigurator(bus.OutboundPipeline);

					interceptorConf.Create(new PublishEndpointInterceptor(bus));
				});

			configurator.AddBusConfigurator(busConf);

			// configurator.UseAzureServiceBus();
		}
	}
}