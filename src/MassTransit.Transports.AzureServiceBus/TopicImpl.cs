using System;
using System.Threading.Tasks;
using MassTransit.Logging;
using MassTransit.Transports.AzureServiceBus.Internal;
using MassTransit.Transports.AzureServiceBus.Management;
using MassTransit.Util;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using TopicDescription = MassTransit.AzureServiceBus.TopicDescription;

namespace MassTransit.Transports.AzureServiceBus
{
	public class TopicImpl : Topic
	{
		static readonly ILog _logger = Logger.Get(typeof (TopicImpl));
		
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

		Task<Tuple<TopicClient, Subscriber>> Topic.CreateSubscriber(
			ReceiveMode mode,
			string subscriberName,
			int prefetch)
		{
			_logger.Debug(string.Format("create client called( mode: PeekMode.{0}, name: '{1}')",
			              mode, subscriberName));

			var client = _messagingFactory.TryCreateTopicClient(_namespaceManager, this);
			subscriberName = subscriberName ?? NameHelper.GenerateRandomName();

			return client.Then(topicClient =>
				{
					var subDesc = new SubscriptionDescriptionImpl(_description.Path, NameHelper.GenerateRandomName())
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