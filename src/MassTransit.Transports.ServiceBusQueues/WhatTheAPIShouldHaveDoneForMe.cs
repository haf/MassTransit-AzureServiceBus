// Copyright 2011 Henrik Feldt
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
using Magnum.Policies;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Magnum.Extensions;
using log4net;
using SBTopicDesc = Microsoft.ServiceBus.Messaging.TopicDescription;

namespace MassTransit.Transports.ServiceBusQueues
{
	public static class WhatTheAPIShouldHaveDoneForMe
	{
		static readonly ILog _logger = LogManager.GetLogger(typeof (WhatTheAPIShouldHaveDoneForMe));

		public static Task TryDeleteTopic(this NamespaceManager nsm, TopicDescription topic)
		{
			var exists = Task.Factory.FromAsync<string, bool>(
				nsm.BeginTopicExists, nsm.EndTopicExists, topic.Path, null);
			return exists.ContinueWith(tExists =>
				{
					try
					{
						if (tExists.Result)
							// according to documentation this thing doesn't throw anything??!
							return Task.Factory.FromAsync(nsm.BeginDeleteTopic, nsm.EndDeleteTopic, topic.Path, null);
					}
					catch (MessagingEntityNotFoundException)
					{
					}
					return Task.Factory.StartNew(() => { });
				});
		}

		// this one is messed up due to a missing API
		public static Task<TopicClient> TryCreateTopicClient(this MessagingFactory messagingFactory,
			NamespaceManager nm,
			Topic topic)
		{
			var timeoutPolicy = ExceptionPolicy
				.InCaseOf<TimeoutException>()
				.CircuitBreak(50.Milliseconds(), 10);

			return Task.Factory.StartNew<TopicClient>(() =>
				{
					timeoutPolicy.Do(() =>
						{
							// where is the BeginCreateTopicClient??!
							return new TopicClientImpl(messagingFactory.CreateTopicClient(topic.Description.Path), nm, topic);
						});
				});
		}

		public static Task<QueueDescription> TryCreateQueue(this NamespaceManager nsm, string queueName)
		{
			//if (nsm.GetQueue(queueName) == null) 
			// bugs out http://social.msdn.microsoft.com/Forums/en-US/windowsazureconnectivity/thread/6ce20f60-915a-4519-b7e3-5af26fc31e35
			// says it'll give null, but throws!

			Task<bool> exists = Task.Factory.FromAsync<string, bool>(
				nsm.BeginQueueExists, nsm.EndQueueExists, queueName, null);

			// TODO: I should add retry logic as a part of the task async workflow!
			//exists.ContinueWith(tExists => {
			//    }, TaskContinuationOptions.OnlyOnFaulted);

			Func<Task<QueueDescription>> create = () => Task.Factory.FromAsync<string, QueueDescription>(
				nsm.BeginCreateQueue, nsm.EndCreateQueue, queueName, null);

			Func<Task<QueueDescription>> get = () => Task.Factory.FromAsync<string, QueueDescription>(
				nsm.BeginGetQueue, nsm.EndGetQueue, queueName, null);

			return exists.ContinueWith(tExists => tExists.Result ? get() : create()).Unwrap();
		}

		/// <returns> the topic description </returns>
		public static Task<Topic> TryCreateTopic(this NamespaceManager nm,
			MessagingFactory factory,
			string topicName)
		{
			var exists = Task.Factory.FromAsync<string, bool>(
				nm.BeginTopicExists, nm.EndTopicExists, topicName, null);

			Func<Task<Topic>> create = () => Task.Factory.FromAsync<string, SBTopicDesc>(
				nm.BeginCreateTopic, nm.EndCreateTopic, topicName, null)
				.ContinueWith(tCreate => new TopicImpl(nm, factory, tCreate.Result) as Topic);

			Func<Task<Topic>> get = () => Task.Factory.FromAsync<string, SBTopicDesc>(
				nm.BeginGetTopic, nm.EndGetTopic, topicName, null)
				.ContinueWith(tGet => new TopicImpl(nm, factory, tGet.Result) as Topic);

			return exists.ContinueWith(tExists => tExists.Result ? get() : create()).Unwrap();

			//while (true)
			//{
			//    try
			//    {
			//        if (!nm.TopicExists(topicName))
			//            return new TopicImpl(nm, factory, nm.CreateTopic(topicName));
			//    }
			//    catch (MessagingEntityAlreadyExistsException)
			//    {
			//    }
			//    try
			//    {
			//        return new TopicImpl(nm, factory, nm.GetTopic(topicName));
			//    }
			//    catch (MessagingEntityNotFoundException) // someone beat us to removing it
			//    {
			//    }
			//}
		}
	}
}