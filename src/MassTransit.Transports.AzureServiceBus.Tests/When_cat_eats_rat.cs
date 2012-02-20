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
using NUnit.Framework;

namespace MassTransit.Transports.AzureServiceBus.Tests
{
	[Description("Validates the simplest possible behaviour; sending a message " +
	             "from a local bus to a remote endpoint. In this case, we're sending " +
	             "a rat to a hungry cat."),
	 Scenario]
	public class When_cat_eats_rat
		: given_a_rat_hole_and_a_cat
	{
		Action<string> cat_sounds = Console.WriteLine,
					   rat_sounds = Console.WriteLine;

		Guid dinner_id;

		ConsumerOf<Rat> the_cat_is;
		Future<Rat> cat_having_dinner;
		MassTransit.UnsubscribeAction take_nap;

		[When]
		public void a_rat_is_sent_to_a_hungry_cat()
		{
			dinner_id = CombGuid.Generate();
			cat_having_dinner = new Future<Rat>();
			the_cat_is = new ConsumerOf<Rat>(a_large_rat_actually =>
				{
					cat_sounds("Miaooo!!!");
					rat_sounds(a_large_rat_actually.Sound + "!!!");
					cat_sounds("Cat: chase! ...");
					cat_sounds("*silence*");
					cat_sounds("Cat: *Crunch chrunch*");
					cat_having_dinner.Complete(a_large_rat_actually);
				});

			cat_prepares_with_napkin();

			var cat = find_hungry_cat();
			cat.Send<Rat>(new
				{
					Sound = "Eeeek",
					CorrelationId = dinner_id
				});
		}

		IEndpoint find_hungry_cat()
		{
			return LocalBus.GetEndpoint(RemoteUri);
		}

		// well behaved cats wear napkins
		void cat_prepares_with_napkin()
		{
			take_nap = RemoteBus.SubscribeInstance(the_cat_is);
			RemoteBus.ShouldHaveSubscriptionFor<Rat>();
			LocalBus.ShouldHaveSubscriptionFor<Rat>();
		}

		[Then]
		public void the_rat_got_eaten()
		{
			cat_having_dinner
				.WaitUntilCompleted(4.Seconds())
				.ShouldBeTrue();

			cat_having_dinner.Value
				.CorrelationId
				.ShouldEqual(dinner_id);
		}

		[Finally]
		public void the_cat_naps()
		{
			if (take_nap != null)
				take_nap();
		}
	}

	public interface Rat : CorrelatedBy<Guid>
	{
		string Sound { get; }
	}
}