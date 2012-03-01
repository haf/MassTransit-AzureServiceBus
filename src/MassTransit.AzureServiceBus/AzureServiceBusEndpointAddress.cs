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
using System.Threading.Tasks;
using MassTransit.AzureServiceBus.Util;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.AzureServiceBus
{
	/// <summary>
	/// 	Endpoint address for the service bus transport. Instances encapsulate the authentication and factory details for the specific endpoint.
	/// </summary>
	public interface AzureServiceBusEndpointAddress
		: IEndpointAddress, IDisposable
	{
		/// <summary>
		/// 	Gets the token provider for this address.
		/// </summary>
		[NotNull]
		TokenProvider TokenProvider { get; }

		/// <summary>
		/// 	Gets a per-transport messaging factory. Warning: Don't use from connection! (http://msdn.microsoft.com/en-us/library/windowsazure/hh528527.aspx)
		/// </summary>
		[UsedImplicitly, NotNull] // in F#
		Func<MessagingFactory> MessagingFactoryFactory { get; }

		/// <summary>
		/// 	Gets the namespace manager in use for this transport/bus.
		/// </summary>
		[NotNull]
		NamespaceManager NamespaceManager { get; }

		/// <summary>
		/// 	Creates the queue corresponding to this address.
		/// </summary>
		/// <returns> </returns>
		/// <exception cref="InvalidOperationException">if QueueDescription == null</exception>
		Task<Unit> CreateQueue();

		/// <summary>
		/// Gets the queue description. This may be null. Null -> TopicDescription != null.
		/// </summary>
		[CanBeNull]
		QueueDescription QueueDescription { get; }

		/// <summary>
		/// Gets the topic description. This may be null. Null -> QueueDescription != null. Things 
		/// that hold this property as non-null, are concerned with pushing messages into the 
		/// corresponding in the outbound pipeline; the inbound pipeline is handled with an endpoint
		/// address with only a queue description (and hence multiple topic descriptions, but that is
		/// inside of the transport itself).
		/// </summary>
		[CanBeNull]
		TopicDescription TopicDescription { get; }

		/// <summary>
		/// Creates an address for the message name (topic), that has the
		/// key-value "topic=true" as a query-string parameter.
		/// </summary>
		/// <param name="topicName">The message topic name.</param>
		/// <returns>A new endpoint address</returns>
		[NotNull]
		AzureServiceBusEndpointAddress ForTopic([NotNull] string topicName);
	}
}