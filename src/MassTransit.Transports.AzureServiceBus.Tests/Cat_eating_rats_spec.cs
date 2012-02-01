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

using System;
using Magnum;
using Magnum.Extensions;
using Magnum.TestFramework;
using MassTransit.TestFramework;
using MassTransit.Transports.AzureServiceBus.Util;

namespace MassTransit.Transports.AzureServiceBus.Tests
{
	public class Cat_eating_rats_spec
		: given_a_rat_hole_and_a_cat
	{
		Guid dinner_id;

		[When]
		public void a_lot_of_rats_are_sent_to_a_cat()
		{
			Consumer = new ConsumerOf<Rat>(r => Console.WriteLine(string.Format("Received rat: {0}!", r.Sound)));

			_unsubscribeAction = RemoteBus.SubscribeInstance(Consumer);
			RemoteBus.ShouldHaveSubscriptionFor<Rat>();

			dinner_id = CombGuid.Generate();

			LocalBus.Publish<Rat>(new Ratty("peep", dinner_id));
			LocalBus.Publish<Rat>(new Ratty("eeky", dinner_id));
		}

		[Then]
		public void The_cat_should_have_consumed_two_rats()
		{
			new Rat[] {new Ratty("peep", dinner_id), new Ratty("eeky", dinner_id)}
				.Each(rat => 
				      Consumer.ShouldHaveReceived(rat, 8.Seconds()));
		}

		MassTransit.UnsubscribeAction _unsubscribeAction;

		protected ConsumerOf<Rat> Consumer { get; private set; }

		[Finally]
		public void Finally()
		{
			_unsubscribeAction();
		}

		// eq-impl of a rat
		class Ratty : Rat, IEquatable<Rat>
		{
			public Ratty(string sound, Guid correlationId)
			{
				CorrelationId = correlationId;
				Sound = sound;
			}

			public Guid CorrelationId { get; private set; }
			public string Sound { get; private set; }

			public override bool Equals(object obj)
			{
				return obj != null && (obj is Rat) && Equals(obj as Rat);
			}

			public bool Equals([NotNull] Rat other)
			{
				if (other == null) throw new ArgumentNullException("other");
				return other.Sound.Equals(Sound)
				       && other.CorrelationId.Equals(CorrelationId);
			}
		}
	}
}