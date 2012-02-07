// Copyright 2012 Henrik Feldt
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

using Magnum.Extensions;
using Magnum.TestFramework;
using MassTransit.Transports.AzureServiceBus.Management;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.AzureServiceBus.Tests.Assumptions
{
	public class Should_be_possible_to_delete_and_recreate_queue
	{
		QueueClient client;

		[Given]
		public void nsm_mf_and_topic()
		{
			var tp = ConfigFactory.CreateTokenProvider();
			var mf = ConfigFactory.CreateMessagingFactory(tp);
			var nm = ConfigFactory.CreateNamespaceManager(mf, tp);
			var qd = nm.TryCreateQueue("to-be-drained").Result;
			client = mf.TryCreateQueueClient(nm, qd, 1).Result;

			// sanity checks; I can now place a message in the queue
			client.Send(new BrokeredMessage(new A("My Contents", new byte[] {1, 2, 3}))).Wait();

		}

		[Then]
		public void I_can_drain_the_topic()
		{
			client.Drain();

			client.Receive(1.Seconds()).Result.ShouldBeNull();
		}
	}
}