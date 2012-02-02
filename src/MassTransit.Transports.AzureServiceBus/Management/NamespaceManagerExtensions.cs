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
using Magnum.Extensions;
using Magnum.Policies;
using MassTransit.Transports.AzureServiceBus.Internal;
using MassTransit.Transports.AzureServiceBus.Util;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using log4net;

namespace MassTransit.Transports.AzureServiceBus.Management
{
	/// <summary>
	/// 	Wrapper over the service bus API that provides a limited amount of retry logic and wraps the APM pattern methods into tasks.
	/// </summary>
	public static class NamespaceManagerExtensions
	{
		static readonly ILog _logger = LogManager.GetLogger(typeof (NamespaceManagerExtensions));

		public static Task TryCreateSubscription([NotNull] this NamespaceManager namespaceManager,
		                                         [NotNull] SubscriptionDescription description)
		{
			return Task.Factory
				.FromAsync<Microsoft.ServiceBus.Messaging.SubscriptionDescription, Microsoft.ServiceBus.Messaging.SubscriptionDescription>(
					namespaceManager.BeginCreateSubscription,
					namespaceManager.EndCreateSubscription,
					description.IDareYou, null);
		}

		public static Task TryDeleteSubscription([NotNull] this NamespaceManager namespaceManager, [NotNull] SubscriptionDescription description)
		{
			if (namespaceManager == null)
				throw new ArgumentNullException("namespaceManager");
			if (description == null)
				throw new ArgumentNullException("description");

			var exists = Task.Factory
				.FromAsync<string, string, bool>(namespaceManager.BeginSubscriptionExists,
				                                 namespaceManager.EndSubscriptionExists,
				                                 description.TopicPath, description.Name, null);

			Func<Task> delete =
				() => Task.Factory
				      	.FromAsync(namespaceManager.BeginDeleteSubscription,
				      	           namespaceManager.EndDeleteSubscription,
				      	           description.TopicPath, description.Name, null)
						.IgnoreExOf<MessagingEntityNotFoundException>();

			return exists.Then(e => e ? delete() : new Task(() => { }));
		}

		public static Task TryDeleteTopic(this NamespaceManager nsm, TopicDescription topic)
		{
			_logger.Debug(string.Format("being topic exists @ {0}", topic.Path));
			var exists = Task.Factory.FromAsync<string, bool>(
				nsm.BeginTopicExists, nsm.EndTopicExists, topic.Path, null)
				.ContinueWith(tExists =>
					{
						_logger.Debug(string.Format("end topic exists @ {0}", topic.Path));
						return tExists.Result;
					});

			return exists.ContinueWith(tExists =>
				{
					_logger.Debug(string.Format("begin delete topic @ {0}", topic.Path));
					// according to documentation this thing doesn't throw anything??!
					return Task.Factory.FromAsync(nsm.BeginDeleteTopic, nsm.EndDeleteTopic, topic.Path, null)
						.ContinueWith(endDelete =>
							{
								_logger.Debug(string.Format("end delete topic @ {0}", topic.Path));
								return endDelete;
							})
						.IgnoreExOf<MessagingEntityNotFoundException>();
				});
		}

		// this one is messed up due to a missing API
		public static Task<TopicClient> TryCreateTopicClient(this MessagingFactory messagingFactory,
		                                                     NamespaceManager nm,
		                                                     Topic topic)
		{
			var timeoutPolicy = ExceptionPolicy
				.InCaseOf<TimeoutException>()
				.CircuitBreak(50.Milliseconds(), 5);

			return Task.Factory.StartNew<TopicClient>(() =>
				{
					return timeoutPolicy.Do(() =>
						{
							// where is the BeginCreateTopicClient??!
							_logger.Debug(string.Format("create topic client for topic @ '{0}'", topic.Description));
							var client = new TopicClientImpl(messagingFactory, nm);
							_logger.Debug(string.Format("create topic client done for topic @ '{0}'", topic.Description));
							return client;
						});
				});
		}

		public static Task<QueueClient> TryCreateQueueClient(
			[NotNull] this MessagingFactory mf, 
			[NotNull] QueueDescription description,
			int prefetchCount)
		{
			if (mf == null) throw new ArgumentNullException("mf");
			if (description == null) throw new ArgumentNullException("description");

			//return Task.Factory.FromAsync<string, MessageReceiver>(
			//    mf.BeginCreateMessageReceiver,
			//    mf.EndCreateMessageReceiver,
			//    description.Path, null);
			// where's the BeginCreateQueueClient??!
			return Task.Factory.StartNew(() =>
				{
					var qc = mf.CreateQueueClient(description.Path);
					qc.PrefetchCount = prefetchCount;
					return qc;
				});
		}

		public static Task<QueueDescription> TryCreateQueue(this NamespaceManager nsm, string queueName)
		{
			//if (nsm.GetQueue(queueName) == null) 
			// bugs out http://social.msdn.microsoft.com/Forums/en-US/windowsazureconnectivity/thread/6ce20f60-915a-4519-b7e3-5af26fc31e35
			// says it'll give null, but throws!

			_logger.Debug(string.Format("being queue exists @ '{0}'", queueName));
			var exists = Task.Factory.FromAsync<string, bool>(
				nsm.BeginQueueExists, nsm.EndQueueExists, queueName, null)
				.ContinueWith(tExists =>
					{
						_logger.Debug(string.Format("end queue exists @ '{0}'", queueName));
						return tExists.Result;
					});

			Func<Task<QueueDescription>> create = () =>
				{
					_logger.Debug(string.Format("being create queue @ '{0}'", queueName));
					return Task.Factory.FromAsync<string, QueueDescription>(
						nsm.BeginCreateQueue, nsm.EndCreateQueue, queueName, null)
						.ContinueWith(tCreate =>
							{
								_logger.Debug(string.Format("end create queue @ '{0}'", queueName));
								return tCreate.Result;
							});
				};

			Func<Task<QueueDescription>> get = () =>
				{
					_logger.Debug(string.Format("begin get queue @ '{0}'", queueName));
					return Task.Factory.FromAsync<string, QueueDescription>(
						nsm.BeginGetQueue, nsm.EndGetQueue, queueName, null)
						.ContinueWith(tGet =>
							{
								_logger.Debug(string.Format("end get queue @ '{0}'", queueName));
								return tGet.Result;
							});
				};

			return exists.Then(doesExist => doesExist ? get() : create());
		}

		/// <returns> the topic description </returns>
		public static Task<Topic> TryCreateTopic(this NamespaceManager nm,
		                                         MessagingFactory factory,
		                                         string topicName)
		{
			_logger.Debug(string.Format("begin topic exists @ '{0}'", topicName));
			var exists = Task.Factory.FromAsync<string, bool>(
				nm.BeginTopicExists, nm.EndTopicExists, topicName, null)
				.ContinueWith(tExists =>
					{
						_logger.Debug(string.Format("end topic exists @ '{0}'", topicName));
						return tExists.Result;
					});

			Func<Task<Topic>> create = () =>
				{
					_logger.Debug(string.Format("begin create topic @ '{0}'", topicName));
					return Task.Factory.FromAsync<string, Microsoft.ServiceBus.Messaging.TopicDescription>(
						nm.BeginCreateTopic, nm.EndCreateTopic, topicName, null)
						.ContinueWith(tCreate =>
							{
								_logger.Debug(string.Format("end create topic @ '{0}'", topicName));
								return new TopicImpl(nm, factory, new TopicDescriptionImpl(tCreate.Result)) as Topic;
							});
				};

			Func<Task<Topic>> get = () =>
				{
					_logger.Debug(string.Format("begin get topic @ '{0}'", topicName));
					return Task.Factory.FromAsync<string, Microsoft.ServiceBus.Messaging.TopicDescription>(
						nm.BeginGetTopic, nm.EndGetTopic, topicName, null)
						.ContinueWith(tGet =>
							{
								_logger.Debug(string.Format("end get topic @ '{0}'", topicName));
								return new TopicImpl(nm, factory, new TopicDescriptionImpl(tGet.Result)) as Topic;
							});
				};

			return exists.Then(doesExist => doesExist ? get() : create());

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