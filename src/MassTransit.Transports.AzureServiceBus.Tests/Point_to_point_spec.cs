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

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable InconsistentNaming

using System;
using Magnum;
using Magnum.Extensions;
using Magnum.TestFramework;
using MassTransit.TestFramework;
using MassTransit.TestFramework.Fixtures;
using MassTransit.Transports.AzureServiceBus.Tests.Framework;
using NUnit.Framework;

namespace MassTransit.Transports.AzureServiceBus.Tests
{
	[Description("Validates the simplest possible behaviour; sending a message " +
	             "from a local bus to a remote endpoint. In this case, we're sending " +
	             "a rat to a hungry cat."),
	 Scenario,
	 Integration]
	public class Point_to_point_spec
		: TwoBusTestFixture<TransportFactoryImpl>
	{
		public Point_to_point_spec()
		{
			var creds = new AccountDetails();
			LocalUri = creds.BuildUri("rat_hole");
			RemoteUri = creds.BuildUri("hungry_cat");
		}

		protected override void ConfigureLocalBus(BusConfigurators.ServiceBusConfigurator configurator)
		{
			configurator.UseJsonSerializer();
			base.ConfigureLocalBus(configurator);
		}

		protected override void ConfigureRemoteBus(BusConfigurators.ServiceBusConfigurator configurator)
		{
			configurator.UseJsonSerializer();
			base.ConfigureRemoteBus(configurator);
		}

		Guid rat_id;
		ConsumerOf<Rat> cat;
		Future<Rat> received_rat;
		MassTransit.UnsubscribeAction cat_nap_unsubscribes;

		[Given]
		public void a_rat_is_sent_to_a_hungry_cat()
		{
			rat_id = CombGuid.Generate();
			received_rat = new Future<Rat>();
			cat = new ConsumerOf<Rat>(a_large_rat_actually =>
				{
					Console.WriteLine("Miaooo!!!");
					Console.WriteLine(a_large_rat_actually.Sound + "!!!");
					Console.WriteLine("Cat: chase! ...");
					Console.WriteLine("*silence*");
					Console.WriteLine("Cat: *Crunch chrunch*");
					received_rat.Complete(a_large_rat_actually);
				});

			cat_nap_unsubscribes = RemoteBus.SubscribeInstance(cat);
			
			// we need to make sure this bus is up before sending to it
			RemoteBus.Endpoint.InboundTransport.Receive(ctx => c => { }, 4.Seconds());
			
			LocalBus.GetEndpoint(RemoteUri).Send<Rat>(new
				{
					Sound = "Eeeek",
					CorrelationId = rat_id
				});
		}

		[Then]
		public void the_rat_got_eaten()
		{
			received_rat
				.WaitUntilCompleted(4.Seconds())
				.ShouldBeTrue();

			received_rat.Value
				.CorrelationId
				.ShouldEqual(rat_id);
		}

		[TearDown]
		public void rats_dance_on_table()
		{
			if (cat_nap_unsubscribes != null) cat_nap_unsubscribes();
		}
	}

	public interface Rat : CorrelatedBy<Guid>
	{
		string Sound { get; }
	}
}