using Magnum.Extensions;
using Magnum.TestFramework;
using MassTransit.Logging;
using MassTransit.Transports.AzureServiceBus.Management;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using NUnit.Framework;

namespace MassTransit.Transports.AzureServiceBus.Tests.Assumptions
{
	[Scenario, Category("NegativeTests")]
	public abstract class Given_a_sent_message
	{
		static readonly ILog _logger = LogManager.GetLogger(typeof (Given_a_sent_message));
		
		protected A message;
		protected NamespaceManager nm;
		protected Topic topic;
		protected TopicClient topicClient;

		[Given]
		public void a_drained_topic_and_a_message()
		{
			message = MyFactory.AMessage();

			var mf = ConfigFactory.CreateMessagingFactory();
			nm = ConfigFactory.CreateNamespaceManager(mf);

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
			if (!topic.DrainBestEffort(3.Seconds()).Wait(5.Seconds()))
			{
				_logger.Debug("failed to complete drain and delete in time");
				Assert.Fail("failure with drain, didn't complete in time");
			}

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