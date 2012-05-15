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
using System.Collections.Generic;
using System.Threading;
using MassTransit.Services.Routing.Configuration;
using MassTransit.Transports.AzureServiceBus.Configuration;
using MassTransit.Transports.AzureServiceBus.Tests.Framework;
using NUnit.Framework;

namespace MassTransit.Transports.AzureServiceBus.Tests.BugTrack
{
	[TestFixture]
	public class AzureServiceBusTests
	{
		private IServiceBus _publisher;
		private IServiceBus _host;
		private IList<TestMessage> _messages;

		[SetUp]
		public void SetUp()
		{
			var hostReceiveFrom = getEndpointUrl("host");
			var publisherReceiveFrom = getEndpointUrl("publisher");

			configurePublisher(publisherReceiveFrom, hostReceiveFrom);
			configureHost(hostReceiveFrom);

			_messages = new List<TestMessage>();
		}

		[Test]
		public void Message_should_be_sent_once()
		{
			_messages.Clear();
			var msg = new TestMessage();

			_publisher.Publish(msg);

			Thread.Sleep(1500);

			Assert.AreEqual(1, _messages.Count);
		}

		private void configureHost(Uri hostUrl)
		{
			_host = ServiceBusFactory.New(config =>
				{
					config.SetPurgeOnStartup(true);
				config.ReceiveFrom(hostUrl);
				config.UseAzureServiceBus();
				config.UseAzureServiceBusRouting();
					config.Subscribe(
						s => s.Handler<TestMessage>(msg => _messages.Add(msg))
						);
				});

			var bus = new RoutingConfigurator().Create(_host);
			bus.Start(_host);
		}

		private void configurePublisher(Uri receiveFrom, Uri hostUrl)
		{
			//var configurator = new RoutingConfigurator();
			//configurator.Route<TestMessage>().To(hostUrl);

			_publisher = ServiceBusFactory.New(config =>
				{
					config.ReceiveFrom(receiveFrom);
					config.UseAzureServiceBus();
					config.UseAzureServiceBusRouting();
				});

			//var bus = configurator.Create(_publisher);
			//bus.Start(_publisher);
		}

		private static Uri getEndpointUrl(string applicationName)
		{
			var details = new AccountDetails();
			return details.BuildUri(applicationName);
		}
	}

	public class TestMessage
	{
		public TestMessage()
		{
			Identifier = Guid.NewGuid();
		}

		public Guid Identifier { get; set; }
	}
}