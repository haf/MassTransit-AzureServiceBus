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
// ReSharper disable InconsistentNaming

using System;
using Magnum;
using Magnum.Extensions;
using Magnum.TestFramework;
using MassTransit.Pipeline.Inspectors;
using MassTransit.Services.Graphite.Configuration;
using MassTransit.TestFramework;
using MassTransit.TestFramework.Fixtures;
using MassTransit.Transports.AzureServiceBus.Tests.Framework;
using MassTransit.Transports.AzureServiceBus.Util;
using MassTransit.Transports.AzureServiceBus.Configuration;

namespace MassTransit.Transports.AzureServiceBus.Tests
{
	[Integration]
	public class When_publishing_concrete_type_and_subscribing_interface
		: EndpointTestFixture<TransportFactoryImpl>
	{
		Guid dinner_id;
		Future<Rat> _receivedAnyRat;

		[When]
		public void a_large_rat_is_published()
		{
			_receivedAnyRat = new Future<Rat>();

			var details = new AccountDetails();

			PublisherBus = SetupServiceBus(details.BuildUri("publisher"), cfg =>
				{
					cfg.UseGraphite(g => 
						g.SetGraphiteDetails("192.168.81.130", 8125, "mt.asb.pubspec.publisher"));

					cfg.UseAzureServiceBusRouting();
				});

			SubscriberBus = SetupServiceBus(details.BuildUri("subscriber"), cfg =>
				{
					cfg.Subscribe(s => s.Handler<Rat>(_receivedAnyRat.Complete).Transient());

					cfg.UseGraphite(g => 
						g.SetGraphiteDetails("192.168.81.130", 8125, "mt.asb.pubspec.subscriber"));

					cfg.UseAzureServiceBusRouting();
				});

			dinner_id = CombGuid.Generate();

			Console.WriteLine("Inbound:");
			Console.WriteLine();
			PipelineViewer.Trace(SubscriberBus.InboundPipeline);

			// wait for the inbound transport to become ready before publishing
			SubscriberBus.Endpoint.InboundTransport.Receive(c1 => c2 => { }, TimeSpan.MaxValue);

			PublisherBus.Publish<Rat>(new SmallRat("peep", dinner_id));

			PipelineViewer.Trace(PublisherBus.OutboundPipeline);
		}
	
		protected IServiceBus PublisherBus { get; private set; }
		protected IServiceBus SubscriberBus { get; private set; }

		[Then]
		public void cat_ate_rat()
		{
			_receivedAnyRat.WaitUntilCompleted(15.Seconds()).ShouldBeTrue();
			_receivedAnyRat.Value.ShouldEqual(new SmallRat("peep", dinner_id));
		}

		class SmallRat : Rat, IEquatable<Rat>
		{
			public SmallRat(string sound, Guid correlationId)
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

			public override int GetHashCode()
			{
				return Sound.GetHashCode() + CorrelationId.GetHashCode();
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