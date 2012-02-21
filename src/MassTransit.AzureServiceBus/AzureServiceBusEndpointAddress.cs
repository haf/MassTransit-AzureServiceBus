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
		TokenProvider TokenProvider { get; }

		/// <summary>
		/// 	Gets a per-transport messaging factory. Warning: Don't use from connection! (http://msdn.microsoft.com/en-us/library/windowsazure/hh528527.aspx)
		/// </summary>
		Func<MessagingFactory> MessagingFactoryFactory { get; }

		/// <summary>
		/// 	Gets the namespace manager in use for this transport/bus.
		/// </summary>
		NamespaceManager NamespaceManager { get; }

		/// <summary>
		/// 	Creates a new queue.
		/// </summary>
		/// <returns> </returns>
		Task<Unit> CreateQueue();

		QueueDescription QueueDescription { get; }
	}
}