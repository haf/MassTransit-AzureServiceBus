using System;
using System.Runtime.Serialization;
using MassTransit.Util;

namespace MassTransit.AzureServiceBus
{
	/// <summary>
	/// value object, implementing equality of <see cref="Path"/>.
	/// </summary>
	public interface TopicDescription 
		: IEquatable<TopicDescription>, IComparable<TopicDescription>, IComparable
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