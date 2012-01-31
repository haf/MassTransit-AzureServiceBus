using System;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.ServiceBusQueues
{
	public interface ServiceBusQueuesAddress : IDisposable
	{
		TokenProvider TokenProvider { get; }
		MessagingFactory MessagingFactory { get; }
		NamespaceManager NamespaceManager { get; }
	}
}