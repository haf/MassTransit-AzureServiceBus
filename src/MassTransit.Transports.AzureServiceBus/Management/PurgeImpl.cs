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
using MassTransit.Transports.AzureServiceBus.Receiver;
using MassTransit.Logging;
using MassTransit.Util;

#pragma warning disable 1591

namespace MassTransit.Transports.AzureServiceBus.Management
{
	/// <summary>
	/// Handles queue connection purging.
	/// </summary>
	public class PurgeImpl
		: AzureManagement
	{
		static readonly ILog _logger = Logger.Get(typeof (PurgeImpl));

		readonly bool _purgeExistingMessages;
		readonly AzureServiceBusEndpointAddress _address;

		public PurgeImpl(bool purgeExistingMessages,
			[NotNull] AzureServiceBusEndpointAddress address)
		{
			if (address == null)
				throw new ArgumentNullException("address");

			_purgeExistingMessages = purgeExistingMessages;
			_address = address;
		}

		/// <summary>
		/// Purges the queue/topic that this management is managing.
		/// </summary>
		internal Task Purge()
		{
			return _address.NamespaceManager.ToggleQueueAsync(_address.QueueDescription);
		}

		public void Bind(ConnectionImpl connection)
		{
			if (!_purgeExistingMessages)
				return;
			
			_logger.InfoFormat("purging queues for {0}", _address);
			Purge().Wait();
		}

		public void Unbind(ConnectionImpl connection)
		{
		}
	}
}