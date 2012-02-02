using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.AzureServiceBus
{
	/// <summary>
	/// Endpoint address for the service bus transport. Instances
	/// encapsulate the authentication and factory details for the specific endpoint.
	/// </summary>
	public interface AzureServiceBusEndpointAddress 
		: IEndpointAddress, IDisposable
	{
		/// <summary>
		/// Gets the token provider for this address.
		/// </summary>
		TokenProvider TokenProvider { get; }

		/// <summary>
		/// Gets a per-transport messaging factory.
		/// Warning: Don't use from connection! (http://msdn.microsoft.com/en-us/library/windowsazure/hh528527.aspx)
		/// </summary>
		MessagingFactory MessagingFactory { get; }
		
		/// <summary>
		/// Gets the namespace manager in use for this transport/bus.
		/// </summary>
		NamespaceManager NamespaceManager { get; }

		/// <summary>
		/// Creates a new queue.
		/// </summary>
		/// <returns></returns>
		Task<QueueDescription> CreateQueue();
	}
}