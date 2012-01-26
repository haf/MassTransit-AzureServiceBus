// Copyright 2011 Henrik Feldt
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.

namespace MassTransit.Transports.AzureQueue.Tests
{
	//public class given_a_stomp_bus_with_a_subscriptionservice
	//    : SubscriptionServiceTestFixture<StompTransportFactory>
	//{
	//    protected given_a_stomp_bus_with_a_subscriptionservice()
	//    {
	//        StompServer = new StompServer(new StompWsListener(new Uri("ws://localhost:8181")));
	//        StompServer.Start();

	//        LocalUri = new Uri("wazq://localhost:8181/test_queue");
	//        RemoteUri = new Uri("wazq://localhost:8181/test_queue_control");
	//        SubscriptionUri = new Uri("wazq://localhost:8181/subscriptions");
	//    }

	//    protected override void ConfigureServiceBus(Uri uri, ServiceBusConfigurator configurator)
	//    {
	//        configurator.UseControlBus();
	//        configurator.UseAzureQueue();
	//        configurator.UseSubscriptionService("wazq://localhost:8181/subscriptions");
	//    }

	//    [After]
	//    public void Stop()
	//    {
	//        StompServer.Stop();
	//    }

	//    protected readonly StompServer StompServer;
	//}
}