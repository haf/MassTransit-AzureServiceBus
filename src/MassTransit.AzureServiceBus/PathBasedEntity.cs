using MassTransit.AzureServiceBus.Util;

namespace MassTransit.AzureServiceBus
{
	/// <summary>
	/// A messaging entity based on a path, such as a topic/subscription or queue.
	/// </summary>
	public interface PathBasedEntity
	{
		/// <summary>
		/// Gets the entity path
		/// </summary>
		[NotNull]
		string Path { get; }
	}
}