using System;
using System.Threading.Tasks;
using MassTransit.Transports.AzureServiceBus.Configuration;
using MassTransit.Transports.AzureServiceBus.Management;
using MassTransit.Transports.AzureServiceBus.Util;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.AzureServiceBus.Tests
{
	public delegate Task DeleteQueueAction();

	// in general their API should be using interfaces that carry both data and operations
	public static class ConfigFactory
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
	}
}