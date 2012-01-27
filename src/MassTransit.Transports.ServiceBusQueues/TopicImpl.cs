using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Magnum.Policies;
using MassTransit.Transports.ServiceBusQueues.Internal;
using MassTransit.Util;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.ServiceBusQueues
{
	public class TopicImpl : Topic
	{
		readonly Random r = new Random();
		readonly NamespaceManager _namespaceManager;
		readonly MessagingFactory _messagingFactory;
		readonly TopicDescription _description;

		public TopicImpl(
			[NotNull] NamespaceManager namespaceManager, 
			[NotNull] MessagingFactory messagingFactory,
			[NotNull] Microsoft.ServiceBus.Messaging.TopicDescription description)
		{
			if (namespaceManager == null) throw new ArgumentNullException("namespaceManager");
			if (messagingFactory == null) throw new ArgumentNullException("messagingFactory");
			if (description == null) throw new ArgumentNullException("description");
			_namespaceManager = namespaceManager;
			_messagingFactory = messagingFactory;
			_description = new TopicDescriptionImpl(description);
		}

		public TopicDescription Description
		{
			get { return _description; }
		}

		public Task DrainBestEffort(TimeSpan timeout)
		{
			var retryFiveTimes = ExceptionPolicy.InCaseOf<TimeoutException>().Retry(5);
			return CreateClient(ReceiveMode.ReceiveAndDelete)
				.Then(tuple =>
					{
						var subscriber = tuple.Item2.Item2;
						while (true)
						{
							try
							{
								// perform the drain operation;
								// since there's no protocol for telling the topic 
								// to drain itself and there's no extra meta
								// data in the topic receive operation
								// the specifies whether the topic queue had something
								// inside last time, we can only wait till it doesn't
								// return anything more and then say that we're done
								while (retryFiveTimes.Do(() => subscriber.Receive(timeout).Result) != null)
#pragma warning disable 642
									;
#pragma warning restore 642
								break;
							}
							// happens seventh call (first -> 5 retries -> excep)
							catch (TimeoutException)
							{
								break;
							}
						}
					});
		}

		public Task<Tuple<TopicClient, Tuple<UnsubscribeAction, Subscriber>>> CreateClient(
			ReceiveMode mode = ReceiveMode.PeekLock,
			string subscriberName = null, 
			bool autoSubscribe = true)
		{
			var client = _messagingFactory.TryCreateTopicClient(_namespaceManager, this);
			subscriberName = subscriberName ?? Helper.GenerateRandomName();

			if (!autoSubscribe)
				return client
					.ContinueWith(tClient => 
						Tuple.Create<TopicClient, Tuple<UnsubscribeAction, Subscriber>>(
							client.Result, null));

			return client.Then(topicClient =>
				{
					var subDesc = new SubscriptionDescription(_description.Path, Helper.GenerateRandomName());
					return topicClient
						.Subscribe(subDesc, mode, subscriberName)
						.ContinueWith(tSub => Tuple.Create(topicClient, tSub.Result));
				});
		}

		public Task Delete()
		{
			return _namespaceManager.TryDeleteTopic(_description);
		}

		#region Equality

		public bool Equals(Topic other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Equals(other.Description.Equals(Description));
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != typeof (Topic)) return false;
			return Equals((Topic) obj);
		}

		public override int GetHashCode()
		{
			return _description.GetHashCode();
		}

		#endregion
	}
}