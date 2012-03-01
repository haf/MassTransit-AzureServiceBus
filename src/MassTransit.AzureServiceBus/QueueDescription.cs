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
	public interface QueueDescription
		: IEquatable<QueueDescription>,
		IComparable<QueueDescription>,
		IComparable,
		PathBasedEntity,
		EntityDescription
	{
		TimeSpan LockDuration { get; }

		bool RequiresSession { get; }

		bool EnableDeadLetteringOnMessageExpiration { get; }

		int MaxDeliveryCount { get; }

		long MessageCount { get; }

		Microsoft.ServiceBus.Messaging.QueueDescription Inner { get; }
	}
}