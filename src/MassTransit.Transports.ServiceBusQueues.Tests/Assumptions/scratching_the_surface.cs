using System;
using Magnum.Extensions;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using NUnit.Framework;
using Magnum.TestFramework;

namespace MassTransit.Transports.AzureQueue.Tests.Assumptions
{
	[Serializable]
	public class A
	{
		public A(string messageContents)
		{
			Contents = messageContents;
		}

		public string Contents { get; protected set; }
	}

	[Category("Integration")]
	public class scratching_the_surface
	{
		private QueueClient queueClient;
		private NamespaceManager nsm;
		private string queueName = "test-queue";

		private void CreateQueue()
		{
			try
			{
				//if (nsm.GetQueue(queueName) == null) 
				// bugs out http://social.msdn.microsoft.com/Forums/en-US/windowsazureconnectivity/thread/6ce20f60-915a-4519-b7e3-5af26fc31e35
				// says it'll give null, but throws!
				nsm.CreateQueue(queueName);
			}
			catch (MessagingEntityAlreadyExistsException)
			{
			}
		}

		[SetUp]
		public void when_I_place_a_message_in_the_queue()
		{
			var tokenProvider = TokenProvider.CreateSharedSecretTokenProvider(AccountDetails.IssuerName, AccountDetails.Key);
			var busUri = ServiceBusEnvironment.CreateServiceUri("sb", AccountDetails.Namespace, string.Empty);
			var factory = MessagingFactory.Create(busUri, tokenProvider);
			nsm = new NamespaceManager(busUri, new NamespaceManagerSettings { TokenProvider = tokenProvider });
			CreateQueue();
			queueClient = factory.CreateQueueClient(queueName);
			Console.WriteLine(queueClient.Path);
			queueClient.Send(new BrokeredMessage(new A("message contents")));
		}

		[TearDown]
		public void finally_remove_queue()
		{
			try
			{
				nsm.DeleteQueue(queueName);
			}
			catch (MessagingEntityNotFoundException)
			{
			}
		}

		[Test]
		public void there_should_be_a_message_there_first_time_around_and_return_null_second_time()
		{
			var msg = queueClient.Receive();
			msg.ShouldNotBeNull();
			try
			{
				var obj = msg.GetBody<A>();
				obj.Contents.ShouldBeEqualTo("message contents");
			}
			finally
			{
				if (msg != null) 
					msg.Complete();
			}

			var msg2 = queueClient.Receive(1000.Milliseconds());
			msg2.ShouldBeNull();
		}
	}
}