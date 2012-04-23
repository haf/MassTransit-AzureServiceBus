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
using MassTransit.Util;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.AzureServiceBus
{
	/// <summary>
	/// Thing picking brokered messages from the network.
	/// </summary>
	[UsedImplicitly] // in F#
	public interface MessageReceiver
	{
		/// <summary>
		/// <see cref="Microsoft.ServiceBus.Messaging.MessageReceiver.BeginReceive(System.TimeSpan,System.AsyncCallback,object)"/>
		/// </summary>
		IAsyncResult BeginReceive(TimeSpan timeout, AsyncCallback callback, object state);
		
		/// <summary>
		/// <see cref="Microsoft.ServiceBus.Messaging.MessageReceiver.EndReceive"/>.
		/// </summary>
		BrokeredMessage EndReceive(IAsyncResult result);

		/// <summary>
		/// <see cref="MessageClientEntity.IsClosed"/>
		/// </summary>
		bool IsClosed { get; }

		/// <summary>
		/// Closes the message receiver.
		/// </summary>
		void Close();
	}
}