using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.ServiceBusQueues
{
	/// <summary>
	/// Endpoint address for the service bus transport. Instances
	/// encapsulate the authentication and factory details for the specific endpoint.
	/// </summary>
	public interface ServiceBusQueuesEndpointAddress 
		: IEndpointAddress, IDisposable
	{
		//TokenProvider TokenProvider { get; }
		MessagingFactory MessagingFactory { get; }
		NamespaceManager NamespaceManager { get; }

		Task<MessageReceiver> CreateQueueClient();
	}
}