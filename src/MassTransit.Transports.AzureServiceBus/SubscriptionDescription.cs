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
using MassTransit.Transports.AzureServiceBus.Internal;
using MassTransit.Util;

namespace MassTransit.Transports.AzureServiceBus
{
	public interface SubscriptionDescription
	{
		bool IsReadOnly { get; }
		ExtensionDataObject ExtensionData { get; set; }
		TimeSpan LockDuration { get; set; }
		bool RequiresSession { get; set; }
		TimeSpan DefaultMessageTimeToLive { get; set; }
		bool EnableDeadLetteringOnMessageExpiration { get; set; }
		bool EnableDeadLetteringOnFilterEvaluationExceptions { get; set; }
		long MessageCount { get; }
		string TopicPath { get; set; }
		string Name { get; set; }
		int MaxDeliveryCount { get; set; }
		bool EnableBatchedOperations { get; set; }

		Guid SubscriptionId { get; }

		/// <summary>
		/// Don't touch
		/// </summary>
		Microsoft.ServiceBus.Messaging.SubscriptionDescription IDareYou { get; }
	}

	public class SubscriptionDescriptionImpl
		: SubscriptionDescription
	{
		readonly Microsoft.ServiceBus.Messaging.SubscriptionDescription _subscriptionDescription;
		Guid _subscriptionId;

		/// <summary>
		/// Create a new subscription description instance with a required
		/// topic path and optional name.
		/// </summary>
		public SubscriptionDescriptionImpl([NotNull] string topicPath, 
			string subscriptionName = null)
		{
			if (topicPath == null) throw new ArgumentNullException("topicPath");

			_subscriptionDescription = new Microsoft.ServiceBus.Messaging.SubscriptionDescription(topicPath,
				subscriptionName ?? Helper.GenerateRandomName());
		}

		public bool IsReadOnly
		{
			get { return _subscriptionDescription.IsReadOnly; }
		}

		public ExtensionDataObject ExtensionData
		{
			get { return _subscriptionDescription.ExtensionData; }
			set { _subscriptionDescription.ExtensionData = value; }
		}

		public TimeSpan LockDuration
		{
			get { return _subscriptionDescription.LockDuration; }
			set { _subscriptionDescription.LockDuration = value; }
		}

		public bool RequiresSession
		{
			get { return _subscriptionDescription.RequiresSession; }
			set { _subscriptionDescription.RequiresSession = value; }
		}

		public TimeSpan DefaultMessageTimeToLive
		{
			get { return _subscriptionDescription.DefaultMessageTimeToLive; }
			set { _subscriptionDescription.DefaultMessageTimeToLive = value; }
		}

		public bool EnableDeadLetteringOnMessageExpiration
		{
			get { return _subscriptionDescription.EnableDeadLetteringOnMessageExpiration; }
			set { _subscriptionDescription.EnableDeadLetteringOnMessageExpiration = value; }
		}

		public bool EnableDeadLetteringOnFilterEvaluationExceptions
		{
			get { return _subscriptionDescription.EnableDeadLetteringOnFilterEvaluationExceptions; }
			set { _subscriptionDescription.EnableDeadLetteringOnFilterEvaluationExceptions = value; }
		}

		public long MessageCount
		{
			get { return _subscriptionDescription.MessageCount; }
		}

		public string TopicPath
		{
			get { return _subscriptionDescription.TopicPath; }
			set { _subscriptionDescription.TopicPath = value; }
		}

		public string Name
		{
			get { return _subscriptionDescription.Name; }
			set { _subscriptionDescription.Name = value; }
		}

		public int MaxDeliveryCount
		{
			get { return _subscriptionDescription.MaxDeliveryCount; }
			set { _subscriptionDescription.MaxDeliveryCount = value; }
		}

		public bool EnableBatchedOperations
		{
			get { return _subscriptionDescription.EnableBatchedOperations; }
			set { _subscriptionDescription.EnableBatchedOperations = value; }
		}

		public Guid SubscriptionId
		{
			get { return _subscriptionId; }
			set { _subscriptionId = value; }
		}

		public Microsoft.ServiceBus.Messaging.SubscriptionDescription IDareYou
		{
			get { return _subscriptionDescription; }
		}

		public override string ToString()
		{
			return string.Format("SubscriptionDescription={{ TopicPath:'{0}', Name:'{1}' }}", TopicPath, Name);
		}
	}
}