using System;
using MassTransit.Util;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.ServiceBusQueues.Tests.Assumptions
{
	static class TestFactory
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

		public static Tuple<Action, QueueClient> SetUpQueue([NotNull] string queueName,
		                                                    TokenProvider tokenProvider = null,
		                                                    MessagingFactory factory = null)
		{
			if (queueName == null) throw new ArgumentNullException("queueName");

			factory = factory ?? CreateMessagingFactory();

			var nsm = CreateNamespaceManager(factory, tokenProvider);
			nsm.TryCreateQueue(queueName);
			return Tuple.Create<Action, QueueClient>(() => nsm.DeleteQueue(queueName), factory.CreateQueueClient(queueName));
		}

		public static NamespaceManager CreateNamespaceManager([NotNull] MessagingFactory factory, TokenProvider tokenProvider = null)
		{
			if (factory == null) throw new ArgumentNullException("factory");
			return new NamespaceManager(factory.Address, new NamespaceManagerSettings
				{
					TokenProvider = tokenProvider ?? CreateTokenProvider()
				});
		}

		public static A AMessage()
		{
			return new A("Ditten datten", new byte[] {2, 4, 6, 7, byte.MaxValue});
		}
	}
}