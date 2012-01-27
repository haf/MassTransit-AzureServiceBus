using System;
using System.Threading.Tasks;
using MassTransit.Util;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.ServiceBusQueues.Tests.Assumptions
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

		public Task Drain()
		{
			return CreateClient(ReceiveMode.ReceiveAndDelete)
				.ContinueWith(tClient =>
					{
					});
		}

		public Task<Tuple<TopicClient, Tuple<UnsubscribeAction, Subscriber>>> CreateClient(
			ReceiveMode mode = ReceiveMode.PeekLock,
			string subscriberName = null, 
			bool autoSubscribe = true)
		{
			var client = _messagingFactory.TryCreateTopicClient(_namespaceManager, this);
			subscriberName = subscriberName ?? Utils.GenerateRandomName();

			if (!autoSubscribe)
				return client.ContinueWith(tClient => Tuple.Create<TopicClient, Tuple<UnsubscribeAction, Subscriber>>(client.Result, null));

			return client.ContinueWith(tClient =>
				{
					return tClient.Result.Subscribe(new SubscriptionDescription(_description.Path, Utils.GenerateRandomName()), mode, subscriberName)
						.ContinueWith((Task<Tuple<UnsubscribeAction, Subscriber>> tSub) =>
							{
								return Tuple.Create(tClient.Result, tSub.Result);
							});
				}).Unwrap();
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