using System;
using System.Runtime.Serialization;
using MassTransit.AzureServiceBus;

namespace MassTransit.Transports.AzureServiceBus
{
	public class TopicDescriptionImpl : TopicDescription
	{
		readonly Microsoft.ServiceBus.Messaging.TopicDescription _description;

		public TopicDescriptionImpl(Microsoft.ServiceBus.Messaging.TopicDescription description)
		{
			_description = description;
		}

		public bool Equals(TopicDescription other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Equals(other.Path, Path);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (!(obj is TopicDescription)) return false;
			return Equals((TopicDescription)obj);
		}

		public override int GetHashCode()
		{
			return Path.GetHashCode();
		}

		public int CompareTo(object obj)
		{
			return CompareTo(obj as TopicDescription);
		}

		public int CompareTo(TopicDescription other)
		{
			if (other == null) return 1;
			return string.CompareOrdinal(Path, other.Path);
		}

		#region Delegating Members

		public bool IsReadOnly
		{
			get { return _description.IsReadOnly; }
		}

		public ExtensionDataObject ExtensionData
		{
			get { return _description.ExtensionData; }
		}

		public TimeSpan DefaultMessageTimeToLive
		{
			get { return _description.DefaultMessageTimeToLive; }
		}

		public long MaxSizeInMegabytes
		{
			get { return _description.MaxSizeInMegabytes; }
		}

		public bool RequiresDuplicateDetection
		{
			get { return _description.RequiresDuplicateDetection; }
		}

		public TimeSpan DuplicateDetectionHistoryTimeWindow
		{
			get { return _description.DuplicateDetectionHistoryTimeWindow; }
		}

		public long SizeInBytes
		{
			get { return _description.SizeInBytes; }
		}

		public string Path
		{
			get { return _description.Path; }
		}

		public bool EnableBatchedOperations
		{
			get { return _description.EnableBatchedOperations; }
		}

		#endregion

		public override string ToString()
		{
			return string.Format("TopicDescription={{ Path:'{0}' }}", Path);
		}
	}
}