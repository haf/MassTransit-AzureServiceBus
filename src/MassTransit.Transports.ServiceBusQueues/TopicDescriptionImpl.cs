using System;
using System.Runtime.Serialization;

namespace MassTransit.Transports.ServiceBusQueues
{
	public class TopicDescriptionImpl : TopicDescription
	{
		readonly Microsoft.ServiceBus.Messaging.TopicDescription _description;

		public TopicDescriptionImpl(Microsoft.ServiceBus.Messaging.TopicDescription description)
		{
			_description = description;
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

		#region Equality

		public bool Equals(TopicDescription other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Equals(other.DefaultMessageTimeToLive, DefaultMessageTimeToLive)
			       && Equals(other.DuplicateDetectionHistoryTimeWindow, DuplicateDetectionHistoryTimeWindow)
			       && Equals(other.EnableBatchedOperations, EnableBatchedOperations)
			       && Equals(other.ExtensionData, ExtensionData)
			       && Equals(other.IsReadOnly, IsReadOnly)
			       && Equals(other.MaxSizeInMegabytes, MaxSizeInMegabytes)
			       && Equals(other.Path, Path)
			       && Equals(other.RequiresDuplicateDetection, RequiresDuplicateDetection)
			       && Equals(other.SizeInBytes, SizeInBytes);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != typeof (TopicDescription)) return false;
			return Equals((TopicDescription) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = DefaultMessageTimeToLive.GetHashCode();
				result = (result*397) ^ DuplicateDetectionHistoryTimeWindow.GetHashCode();
				result = (result*397) ^ EnableBatchedOperations.GetHashCode();
				result = (result*397) ^ (ExtensionData != null ? ExtensionData.GetHashCode() : 0);
				result = (result*397) ^ IsReadOnly.GetHashCode();
				result = (result*397) ^ MaxSizeInMegabytes.GetHashCode();
				result = (result*397) ^ Path.GetHashCode();
				result = (result*397) ^ RequiresDuplicateDetection.GetHashCode();
				result = (result*397) ^ SizeInBytes.GetHashCode();
				return result;
			}
		}

		#endregion

		public override string ToString()
		{
			return string.Format("TopicDescription={{ Path:'{0}' }}", Path);
		}
	}
}