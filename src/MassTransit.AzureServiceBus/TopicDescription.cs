using System;
using System.Runtime.Serialization;
using MassTransit.Util;

namespace MassTransit.AzureServiceBus
{
	public interface PathBasedEntity
	{
		/// <summary>
		/// Gets the entity path
		/// </summary>
		[NotNull]
		string Path { get; }
	}

	public interface EntityDescription
	{
		bool IsReadOnly { get; }
		ExtensionDataObject ExtensionData { get; }

		TimeSpan DefaultMessageTimeToLive { get; }
		long MaxSizeInMegabytes { get; }
		bool RequiresDuplicateDetection { get; }
		TimeSpan DuplicateDetectionHistoryTimeWindow { get; }
		long SizeInBytes { get; }
		bool EnableBatchedOperations { get; }
	}

	/// <summary>
	/// value object, implementing equality of <see cref="PathBasedEntity.Path"/>.
	/// </summary>
	public interface TopicDescription
		: IEquatable<TopicDescription>, IComparable<TopicDescription>, 
		IComparable, PathBasedEntity,
		EntityDescription
	{
	}
}