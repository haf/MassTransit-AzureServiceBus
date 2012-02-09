using System;
using System.Threading.Tasks;
using Magnum.Policies;
using MassTransit.Transports.AzureServiceBus.Internal;
using MassTransit.Transports.AzureServiceBus.Management;
using MassTransit.Util;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using log4net;

namespace MassTransit.Transports.AzureServiceBus
{
	public class TopicImpl : Topic
	{
		static readonly ILog _logger = LogManager.GetLogger(typeof (TopicImpl));
		
		readonly NamespaceManager _namespaceManager;
		readonly MessagingFactory _messagingFactory;
		readonly TopicDescription _description;
		readonly Topic _self;

		public TopicImpl(
			[NotNull] NamespaceManager namespaceManager, 
			[NotNull] MessagingFactory messagingFactory,
			[NotNull] TopicDescription description)
		{
			if (namespaceManager == null) throw new ArgumentNullException("namespaceManager");
			if (messagingFactory == null) throw new ArgumentNullException("messagingFactory");
			if (description == null) throw new ArgumentNullException("description");
			_namespaceManager = namespaceManager;
			_messagingFactory = messagingFactory;
			_description = description;
			_self = this;
		}

		public TopicDescription Description
		{
			get { return _description; }
		}

		Task Topic.DrainBestEffort(TimeSpan timeout)
		{
			var retryFiveTimes = ExceptionPolicy.InCaseOf<TimeoutException>().Retry(5);
			return _self.CreateSubscriber(ReceiveMode.ReceiveAndDelete)
				.Then(tuple =>
					{
						var subscriber = tuple.Item2;
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
								while (retryFiveTimes.Do<BrokeredMessage>(() =>
									{
										var receive = subscriber.Receive(timeout);
										receive.Wait();
										return receive.Result;
									}) != null)
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

						_logger.Debug("unsubscribe action wait start");
						tuple.Item2.Dispose();
						_logger.Debug("unsubscribe action wait end");
					});
		}
		
		Task<Tuple<TopicClient, Subscriber>> Topic.CreateSubscriber(
			ReceiveMode mode,
			string subscriberName,
			int prefetch)
		{
			_logger.Debug(string.Format("create client called( mode: PeekMode.{0}, name: '{1}')",
			              mode, subscriberName));

			var client = _messagingFactory.TryCreateTopicClient(_namespaceManager, this);
			subscriberName = subscriberName ?? Helper.GenerateRandomName();

			return client.Then(topicClient =>
				{
					var subDesc = new SubscriptionDescriptionImpl(_description.Path, Helper.GenerateRandomName())
						{
							EnableBatchedOperations = true,
							MaxDeliveryCount = prefetch
						};
					return topicClient.Subscribe(this, subDesc, mode, subscriberName)
						.Then(tSub => Tuple.Create(topicClient, tSub));
				});
		}

		Task Topic.Delete()
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