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

namespace MassTransit.AzureServiceBus
{
	/// <summary>
	/// DTO with properties relating to a queue. Equatable, comparable
	/// and based on the Azure API's <see cref="PathBasedEntity"/> and <see cref="EntityDescription"/> 
	/// that are internal.
	/// </summary>
	public interface QueueDescription
		: IEquatable<QueueDescription>,
		IComparable<QueueDescription>,
		IComparable,
		PathBasedEntity,
		EntityDescription
	{
		/// <summary>
		/// How long before the consumed message is re-inserted into the queue?
		/// </summary>
		TimeSpan LockDuration { get; }

		/// <summary>
		/// Whether the queue is configured to be session-ful;
		/// allowing a primitive key-value store as well as de-duplication
		/// on a per-receiver basis.
		/// </summary>
		bool RequiresSession { get; }

		/// <summary>
		/// Property talks for itself.
		/// </summary>
		bool EnableDeadLetteringOnMessageExpiration { get; }

		/// <summary>
		/// The maximum times to try to redeliver a message before moving it
		/// to a poision message queue. Note - this property would preferrably be int.MaxValue, since MassTransit
		/// handles the poison message queues itself and there's no need to use the 
		/// AzureServiceBus API for this.
		/// </summary>
		int MaxDeliveryCount { get; }
		
		/// <summary>
		/// Gets the number of messages present in the queue
		/// </summary>
		long MessageCount { get; }

		/// <summary>
		/// Gets the inner <see cref="Microsoft.ServiceBus.Messaging.QueueDescription"/>.
		/// </summary>
		Microsoft.ServiceBus.Messaging.QueueDescription Inner { get; }
	}
}