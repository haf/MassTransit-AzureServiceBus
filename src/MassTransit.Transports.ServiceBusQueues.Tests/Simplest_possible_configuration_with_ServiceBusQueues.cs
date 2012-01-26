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

using MassTransit.Transports.ServiceBusQueues.Configuration;
using NUnit.Framework;

namespace MassTransit.Transports.ServiceBusQueues.Tests
{
	public class Simplest_possible_configuration_with_ServiceBusQueues
	{
		[Test]
		public void configuring_the_message_bus()
		{
			using (ServiceBusFactory.New(sbc =>
				{
					sbc.ReceiveFrom("sb-queues://owner:sharedKeyTopSecret@myNamespace/my-application");
					sbc.UseServiceBusQueues();
				}))
			{
			}
		}

		[Test]
		public void alternative()
		{
			using (ServiceBusFactory.New(sbc =>
			{
				sbc.ReceiveFrom("owner", "sharedKeyTopSecret", "myNamespace", "my-application");
				sbc.UseServiceBusQueues();
			}))
			{
			}
		}
	}
}