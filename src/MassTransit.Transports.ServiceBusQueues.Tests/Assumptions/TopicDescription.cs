using System;
using System.Runtime.Serialization;

namespace MassTransit.Transports.ServiceBusQueues.Tests.Assumptions
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
		string Path { get; }
		bool EnableBatchedOperations { get; }
	}
}