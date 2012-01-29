using System;
using System.Threading.Tasks;
using MassTransit.Transports.ServiceBusQueues.Internal;
using MassTransit.Transports.ServiceBusQueues.Tests.Assumptions;
using MassTransit.Util;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.ServiceBusQueues.Tests
{
	public delegate Task DeleteQueueAction();

	// in general their API should be using interfaces that carry both data and operations
	static class ConfigFactory
	{
		public static TokenProvider CreateTokenProvider()
		{
			var tokenProvider = TokenProvider.CreateSharedSecretTokenProvider(AccountDetails.IssuerName, AccountDetails.Key);
			return tokenProvider;
		}

		public static MessagingFactory CreateMessagingFactory(TokenProvider tokenProvider = null)
		{
			var busUri = ServiceBusEnvironment.CreateServiceUri("sb", AccountDetails.Namespace, string.Empty);
			return MessagingFactory.Create(busUri, tokenProvider ?? CreateTokenProvider());
		}

		public static NamespaceManager CreateNamespaceManager(
			[NotNull] MessagingFactory factory, 
			TokenProvider tokenProvider = null)
		{
			if (factory == null) throw new ArgumentNullException("factory");
			return new NamespaceManager(factory.Address, new NamespaceManagerSettings
				{
					TokenProvider = tokenProvider ?? CreateTokenProvider()
				});
		}

		public static Task<Tuple<DeleteQueueAction, QueueClient>> SetUpQueue(
			[NotNull] string queueName,
			TokenProvider tokenProvider = null,
			MessagingFactory factory = null)
		{
			if (queueName == null) throw new ArgumentNullException("queueName");

			factory = factory ?? CreateMessagingFactory();
			var nsm = CreateNamespaceManager(factory, tokenProvider);

			return nsm.TryCreateQueue(queueName)
				.ContinueWith(tQ => Tuple.Create<DeleteQueueAction, QueueClient>(
					() => Task.Factory.FromAsync(nsm.BeginDeleteQueue, nsm.EndDeleteQueue, queueName, null),
					factory.CreateQueueClient(queueName)), TaskContinuationOptions.AttachedToParent);
		}

		public static A AMessage()
		{
			return new A("Ditten datten", new byte[] {2, 4, 6, 7, byte.MaxValue});
		}
	}
}