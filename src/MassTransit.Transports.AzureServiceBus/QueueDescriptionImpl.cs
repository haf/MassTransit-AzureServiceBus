// Copyright 2012 Henrik Feldt
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.

using System;
using System.Runtime.Serialization;
using MassTransit.AzureServiceBus;

#pragma warning disable 1591

namespace MassTransit.Transports.AzureServiceBus
{
	/// <summary>
	/// See <see cref="QueueDescription"/>
	/// </summary>
	public class QueueDescriptionImpl : QueueDescription
	{
		readonly Microsoft.ServiceBus.Messaging.QueueDescription _inner;

		/// <summary>
		/// Creates a new queue description and enables batched operations.
		/// </summary>
		/// <param name="queueName"></param>
		public QueueDescriptionImpl(string queueName)
		{
			_inner = new Microsoft.ServiceBus.Messaging.QueueDescription(queueName);
			_inner.EnableBatchedOperations = true;
		}

		public bool Equals(QueueDescription other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Equals(other.Path, Path);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (!(obj is QueueDescription)) return false;
			return Equals((QueueDescription) obj);
		}

		public override int GetHashCode()
		{
			return Path.GetHashCode();
		}

		public int CompareTo(object obj)
		{
			return CompareTo(obj as QueueDescription);
		}

		public int CompareTo(QueueDescription other)
		{
			if (other == null) return 1;
			return string.CompareOrdinal(Path, other.Path);
		}

		public override string ToString()
		{
			return Path;
		}

		public bool IsReadOnly
		{
			get { return _inner.IsReadOnly; }
		}

		public ExtensionDataObject ExtensionData
		{
			get { return _inner.ExtensionData; }
			set { _inner.ExtensionData = value; }
		}

		public TimeSpan LockDuration
		{
			get { return _inner.LockDuration; }
			set { _inner.LockDuration = value; }
		}

		public long MaxSizeInMegabytes
		{
			get { return _inner.MaxSizeInMegabytes; }
			set { _inner.MaxSizeInMegabytes = value; }
		}

		public bool RequiresDuplicateDetection
		{
			get { return _inner.RequiresDuplicateDetection; }
			set { _inner.RequiresDuplicateDetection = value; }
		}

		public bool RequiresSession
		{
			get { return _inner.RequiresSession; }
			set { _inner.RequiresSession = value; }
		}

		public TimeSpan DefaultMessageTimeToLive
		{
			get { return _inner.DefaultMessageTimeToLive; }
			set { _inner.DefaultMessageTimeToLive = value; }
		}

		public bool EnableDeadLetteringOnMessageExpiration
		{
			get { return _inner.EnableDeadLetteringOnMessageExpiration; }
			set { _inner.EnableDeadLetteringOnMessageExpiration = value; }
		}

		public TimeSpan DuplicateDetectionHistoryTimeWindow
		{
			get { return _inner.DuplicateDetectionHistoryTimeWindow; }
			set { _inner.DuplicateDetectionHistoryTimeWindow = value; }
		}

		public string Path
		{
			get { return _inner.Path; }
			set { _inner.Path = value; }
		}

		public int MaxDeliveryCount
		{
			get { return _inner.MaxDeliveryCount; }
			set { _inner.MaxDeliveryCount = value; }
		}

		public bool EnableBatchedOperations
		{
			get { return _inner.EnableBatchedOperations; }
			set { _inner.EnableBatchedOperations = value; }
		}

		public long SizeInBytes
		{
			get { return _inner.SizeInBytes; }
		}

		public long MessageCount
		{
			get { return _inner.MessageCount; }
		}

		public Microsoft.ServiceBus.Messaging.QueueDescription Inner
		{
			get { return _inner; }
		}
	}
}