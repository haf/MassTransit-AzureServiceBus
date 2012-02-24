using System;
using System.Threading.Tasks;
using MassTransit.Transports.AzureServiceBus.Util;
using Microsoft.ServiceBus.Messaging;
using TopicDescription = MassTransit.AzureServiceBus.TopicDescription;

namespace MassTransit.Transports.AzureServiceBus
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
		/// Create a subscriber.
		/// </summary>
		/// <param name="mode"></param>
		/// <param name="subscriberName"></param>
		/// <param name="prefetch"></param>
		/// <returns></returns>
		Task<Tuple<TopicClient, Subscriber>>
			CreateSubscriber(ReceiveMode mode = ReceiveMode.PeekLock, string subscriberName = null,
			int prefetch = 100);

		/// <summary>
		/// Deletes the topic.
		/// </summary>
		/// <returns></returns>
		Task Delete();
	}
}