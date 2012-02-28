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
using MassTransit.Transports.AzureServiceBus.Tests.Framework;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using MassTransit.Async;
using QueueDescription = MassTransit.AzureServiceBus.QueueDescription;

namespace MassTransit.Transports.AzureServiceBus.Tests.Assumptions
{
	[Scenario, Integration]
	public class When_toggling_queue_its_empty
	{
		MessageSender client;
		NamespaceManager nm;
		QueueDescription desc;

		[Given]
		public void nsm_mf_and_topic()
		{
			var tp = TestConfigFactory.CreateTokenProvider();
			var mf = TestConfigFactory.CreateMessagingFactory(tp);
			
			desc = new QueueDescriptionImpl("to-be-drained");
			nm = TestConfigFactory.CreateNamespaceManager(mf, tp);

			client = mf.NewSenderAsync(nm, desc).Result;

			// sanity checks; I can now place a message in the queue
			client.Send(new BrokeredMessage(new A("My Contents", new byte[] {1, 2, 3})));
		}

		[Then]
		public void I_can_consume_the_message()
		{
			nm.ToggleQueueAsync(desc).Wait();

			var r = ReceiverModule.StartReceiver(TestDataFactory.GetAddress(), null);

			BrokeredMessage message = null;
			try
			{
				message = r.Get(1.Seconds()).ShouldBeNull();
			}
			finally
			{
				if (message != null)
					message.Complete();
			}
		}
	}
}