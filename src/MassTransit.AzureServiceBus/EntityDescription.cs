using System;
using System.Runtime.Serialization;

namespace MassTransit.AzureServiceBus
{
	/// <summary>
	/// Generic "entity description"; pretty much a queue description,
	/// because subscriptions mean to create individual queues.
	/// </summary>
	public interface EntityDescription
	{
		/// <summary>
		/// TODO - depends on context, strange property
		/// </summary>
		bool IsReadOnly { get; }

		/// <summary>
		/// You can use this if your entity is session-ful, to keep track of 
		/// entity state.
		/// </summary>
		ExtensionDataObject ExtensionData { get; }

		/// <summary>
		/// 
		/// </summary>
		TimeSpan DefaultMessageTimeToLive { get; }
		
		/// <summary>
		/// 
		/// </summary>
		long MaxSizeInMegabytes { get; }
		
		/// <summary>
		/// Only for session-ful work
		/// </summary>
		bool RequiresDuplicateDetection { get; }
		
		/// <summary>
		/// Only for session-ful work
		/// </summary>
		TimeSpan DuplicateDetectionHistoryTimeWindow { get; }
		
		/// <summary>
		/// Gets the queue size in bytes
		/// </summary>
		long SizeInBytes { get; }

		/// <summary>
		/// Should be true in this transport.
		/// </summary>
		bool EnableBatchedOperations { get; }
	}
}