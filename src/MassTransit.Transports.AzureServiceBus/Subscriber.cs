using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.AzureServiceBus
{
	/// <summary>
	/// A consumer that consumes published messages.
	/// </summary>
	public interface Subscriber : IDisposable
	{
		/// <summary>
		/// Gets the subscription id for this subscriber.
		/// </summary>
		Guid SubscriptionId { get; }

		/// <summary>
		/// 
		/// </summary>
		/// <returns>Task with Result = null if no further messages</returns>
		Task<BrokeredMessage> Receive();

		/// <returns>Task with Result = null if no further messages</returns>
		Task<BrokeredMessage> Receive(TimeSpan timeout);
	}
}