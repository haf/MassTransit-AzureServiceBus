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
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.AzureServiceBus
{
	/// <summary>
	/// Queue/endpoint consumer.
	/// </summary>
	public interface QueueClient : IDisposable
	{
		ReceiveMode Mode { get; }
		int PrefetchCount { get; }
		string Path { get; }

		/// <summary>
		/// Start receiving a message (possibly null).
		/// </summary>
		/// <returns></returns>
		Task<BrokeredMessage> Receive();


		Task<BrokeredMessage> Receive(TimeSpan serverWaitTime);
		Task Send(BrokeredMessage message);
		
		/// <summary>
		/// Drains the queue by deleting and re-creating it.
		/// </summary>
		void Drain();

		IAsyncResult BeginSend(BrokeredMessage message, AsyncCallback callback, object state);
		void EndSend(IAsyncResult result);

		IAsyncResult BeginReceive(TimeSpan serverWaitTime, AsyncCallback callback, object state);
		BrokeredMessage EndReceive(IAsyncResult result);
	}
}