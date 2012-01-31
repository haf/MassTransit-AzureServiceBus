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

using System;
using Magnum.Extensions;
using MassTransit.TestFramework.Fixtures;
using MassTransit.Transports.ServiceBusQueues.Configuration;

namespace MassTransit.Transports.ServiceBusQueues.Tests
{
	public abstract class given_a_broker
		: LocalTestFixture<TransportFactoryImpl>
	{
		protected given_a_broker()
		{
			LocalUri = new Uri("sb-queues://{0}:{1}@{2}/test-client"
			                   	.FormatWith(
			                   		AccountDetails.IssuerName, // username
			                   		AccountDetails.Key, // password
			                   		AccountDetails.Namespace)); // namespace

			ConfigureEndpointFactory(x => x.UseServiceBusQueues());
		}

		protected override void ConfigureServiceBus(Uri uri, BusConfigurators.ServiceBusConfigurator configurator)
		{
			base.ConfigureServiceBus(uri, configurator);

			configurator.UseServiceBusQueuesRouting();
		}
	}
}