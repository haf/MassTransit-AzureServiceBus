using System;
using System.Runtime.Serialization;
using MassTransit.Util;

namespace MassTransit.Transports.AzureServiceBus
{
	/// <summary>
	/// value object
	/// </summary>
	public interface TopicDescription : IEquatable<TopicDescription>
	{
		bool IsReadOnly { get; }
		ExtensionDataObject ExtensionData { get; }
		TimeSpan DefaultMessageTimeToLive { get; }
		long MaxSizeInMegabytes { get; }
		bool RequiresDuplicateDetection { get; }
		TimeSpan DuplicateDetectionHistoryTimeWindow { get; }

		long SizeInBytes { get; }
		
		[NotNull]
		string Path { get; }
		
		bool EnableBatchedOperations { get; }
	}
}