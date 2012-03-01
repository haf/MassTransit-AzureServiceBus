using System;
using System.Threading.Tasks;
using MassTransit.Transports.AzureServiceBus.Util;
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
		/// Deletes the topic.
		/// </summary>
		/// <returns></returns>
		Task Delete();
	}
}