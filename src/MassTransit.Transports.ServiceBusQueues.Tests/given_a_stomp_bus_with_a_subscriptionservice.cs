using System;
using MassTransit.BusConfigurators;
using Magnum.TestFramework;
using MassTransit.TestFramework.Fixtures;

namespace MassTransit.Transports.AzureQueue.Tests
{
	public class given_a_stomp_bus_with_a_subscriptionservice
        : SubscriptionServiceTestFixture<StompTransportFactory>
    {
        protected given_a_stomp_bus_with_a_subscriptionservice()
        {
            StompServer = new StompServer(new StompWsListener(new Uri("ws://localhost:8181")));
            StompServer.Start();

			LocalUri = new Uri("wazq://localhost:8181/test_queue");
			RemoteUri = new Uri("wazq://localhost:8181/test_queue_control");
			SubscriptionUri = new Uri("wazq://localhost:8181/subscriptions");
        }

        protected override void ConfigureServiceBus(Uri uri, ServiceBusConfigurator configurator)
        {
            configurator.UseControlBus();
            configurator.UseAzureQueue();
            configurator.UseSubscriptionService("wazq://localhost:8181/subscriptions");
        }

        [After]
        public void Stop()
        {
            StompServer.Stop();
        }

        protected readonly StompServer StompServer;
    }
}