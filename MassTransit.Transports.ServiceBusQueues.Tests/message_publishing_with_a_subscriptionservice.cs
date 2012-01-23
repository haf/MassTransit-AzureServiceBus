using System;
using MassTransit.BusConfigurators;
using Magnum.Extensions;
using Magnum.TestFramework;
using MassTransit.TestFramework;

namespace MassTransit.Transports.AzureQueue.Tests
{
	public class message_publishing_with_a_subscriptionservice
        : given_a_stomp_bus_with_a_subscriptionservice
    {
        private Future<A> _received;

        protected override void ConfigureServiceBus(Uri uri, ServiceBusConfigurator configurator)
        {
            base.ConfigureServiceBus(uri, configurator);

            _received = new Future<A>();
            configurator.Subscribe(s => s.Handler<A>(message => _received.Complete(message)));
        }

        [When]
        public void A_message_is_published()
        {
            LocalBus.Publish(new A
            {
                StringA = "ValueA",
            });
        }

        [Then]
        public void Should_be_received_by_the_queue()
        {
            _received.WaitUntilCompleted(3.Seconds()).ShouldBeTrue();
            _received.Value.StringA.ShouldEqual("ValueA");
        }

        private class A
        {
            public string StringA { get; set; }
        }
    }
}