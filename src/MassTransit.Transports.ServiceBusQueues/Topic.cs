using System;
using System.Threading.Tasks;
using MassTransit.Transports.ServiceBusQueues.Utils;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.ServiceBusQueues
{
	public interface Topic : IEquatable<Topic>
	{
		/// <summary>
		/// Gets the topic description - i.e. its meta
		/// data.
		/// </summary>
		[NotNull]
		TopicDescription Description { get; }

		/// <summary>
		/// Consumes and deletes until there is no message for the specified timeout.
		/// </summary>
		/// <returns>The hot computation of the drain</returns>
		Task DrainBestEffort(TimeSpan timeout);
		
		Task<Tuple<TopicClient, Tuple<UnsubscribeAction, Subscriber>>>
			CreateClient(ReceiveMode mode = ReceiveMode.PeekLock, string subscriberName = null,
			bool autoSubscribe = true,
			int prefetch = 100);

		Task Delete();
	}
}