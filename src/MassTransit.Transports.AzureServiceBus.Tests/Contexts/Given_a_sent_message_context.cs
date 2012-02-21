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

using Magnum.TestFramework;
using MassTransit.Transports.AzureServiceBus.Management;
using MassTransit.Transports.AzureServiceBus.Tests.Framework;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using NLog;
using NUnit.Framework;

namespace MassTransit.Transports.AzureServiceBus.Tests.Assumptions
{
	[Scenario, Category("NegativeTests")]
	public abstract class Given_a_sent_message_context
	{
		static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		protected A message;
		protected NamespaceManager nm;
		protected Topic topic;
		protected TopicClient topicClient;

		[Given]
		public void a_drained_topic_and_a_message()
		{
			message = TestDataFactory.AMessage();

			var mf = TestConfigFactory.CreateMessagingFactory();
			nm = TestConfigFactory.CreateNamespaceManager(mf);

			var createTopic = nm.TryCreateTopic(mf, "my.topic.here");
			createTopic.Wait();
			topic = createTopic.Result;

			var client = topic.CreateSubscriber();
			client.Wait();
			topicClient = client.Result.Item1;
			_logger.Debug("[Given] done");
		}

		[Finally]
		public void finally_close_the_client()
		{
			_logger.Debug("[Finally]");
			topic.Delete().Wait();
			topicClient.Dispose();
		}

		[SetUp]
		public void given_a_message_sent_to_the_topic()
		{
			_logger.Debug("[SetUp] draining");

			var msg = new BrokeredMessage(message);
			BeforeSend(msg);
			_logger.Debug("[SetUp] sending test message");
			topicClient.Send(msg, topic).Wait();
		}

		protected virtual void BeforeSend(BrokeredMessage msg)
		{
		}
	}
}