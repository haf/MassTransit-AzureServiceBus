using System;
using Magnum.TestFramework;
using MassTransit.BusConfigurators;
using MassTransit.TestFramework.Fixtures;

namespace MassTransit.Transports.AzureQueue.Tests
{
	public class given_a_servicebusbroker_servicebus
		: LocalTestFixture<ServiceBusQueueTransportFactory>
	{
		protected given_a_servicebusbroker_servicebus()
		{
			StompServer = new StompServer(new StompWsListener(new Uri("ws://localhost:8181")));
			StompServer.Start();

			LocalUri = new Uri("stomp://localhost:8181/test_queue");
		}

		protected override void ConfigureServiceBus(Uri uri, ServiceBusConfigurator configurator)
		{
			configurator.UseAzureQueue();
		}

		[After]
		protected void Stop()
		{
			StompServer.Stop();
		}

		protected readonly StompServer StompServer;
	}
}