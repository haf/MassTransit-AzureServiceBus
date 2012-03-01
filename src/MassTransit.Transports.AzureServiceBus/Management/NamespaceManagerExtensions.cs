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
using MassTransit.Transports.AzureServiceBus.Util;
using Microsoft.ServiceBus.Messaging;
using MessageSender = MassTransit.AzureServiceBus.MessageSender;
using QueueDescription = MassTransit.AzureServiceBus.QueueDescription;
using SBQClient = Microsoft.ServiceBus.Messaging.QueueClient;
using TopicDescription = MassTransit.AzureServiceBus.TopicDescription;

namespace MassTransit.Transports.AzureServiceBus.Management
{
	/// <summary>
	/// 	Wrapper over the service bus API that provides a limited amount of retry logic and wraps the APM pattern methods into tasks.
	/// </summary>
	public static class NamespaceManagerExtensions
	{
		public static Task<MessageSender> TryCreateMessageSender(
			[NotNull] this MessagingFactory mf,
			[NotNull] QueueDescription description,
			int prefetchCount)
		{
			if (mf == null) throw new ArgumentNullException("mf");
			if (description == null) throw new ArgumentNullException("description");

			return Task.Factory.StartNew(() =>
				{
					var qc = mf.CreateQueueClient(description.Path);
					qc.PrefetchCount = prefetchCount;
					return new MessageSenderImpl(qc) as MessageSender;
				});
		}

		public static Task<MessageSender> TryCreateMessageSender(
			[NotNull] this MessagingFactory mf,
			[NotNull] TopicDescription description)
		{
			return Task.Factory.StartNew(() =>
				{
					var s = mf.CreateTopicClient(description.Path);
					return new MessageSenderImpl(s) as MessageSender;
				});
		}
	}
}