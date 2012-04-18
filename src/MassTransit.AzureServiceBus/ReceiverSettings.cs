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

namespace MassTransit.AzureServiceBus
{
	/// <summary>
	/// Receiver specific settings
	/// </summary>
	[UsedImplicitly] // from f#
	public interface ReceiverSettings
	{
		/// <summary>
		/// Gets the number of concurrently outstanding requests.
		/// </summary>
		uint Concurrency { get; }

		/// <summary>
		/// Gets the maximum size of the buffer
		/// </summary>
		uint BufferSize { get; }

		/// <summary>
		/// forall a. a % nth = 0 -> new mf
		/// where a 0..Concurrency
		/// </summary>
		uint NThAsync { get; }

		/// <summary>
		/// Gets the receive timeout passed to the service bus library.
		/// </summary>
		TimeSpan ReceiveTimeout { get;  }

        /// <summary>
        /// Gets the receiver name.
        /// </summary>
        string ReceiverName { get; }
	}
}