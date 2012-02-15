using System;
using System.Threading.Tasks;
using MassTransit.Logging;
using MassTransit.Transports.AzureServiceBus.Management;
using MassTransit.Util;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.AzureServiceBus
{
	public delegate Task UnsubscribeAction();

	// handles subscribers
	public class TopicClientImpl : TopicClient
	{
		static readonly ILog _logger = Logger.Get(typeof (TopicClientImpl));
		
		readonly MessagingFactory _messagingFactory;
		readonly NamespaceManager _namespaceManager;
		bool _isDisposed;
		Func<string, Microsoft.ServiceBus.Messaging.TopicClient> _clientFac;

		public TopicClientImpl(
			[NotNull] MessagingFactory messagingFactory,
			[NotNull] NamespaceManager namespaceManager)
		{
			if (messagingFactory == null) throw new ArgumentNullException("messagingFactory");
			if (namespaceManager == null) throw new ArgumentNullException("namespaceManager");

			_messagingFactory = messagingFactory;
			_namespaceManager = namespaceManager;
			_clientFac = path => _messagingFactory.CreateTopicClient(path);

			_logger.Debug("created new topic client");
		}

		public Task Send(BrokeredMessage msg, Topic topic)
		{
			// todo: client has Close method... but not dispose.
			var client = _clientFac(topic.Description.Path);

			_logger.Debug("begin send");
			return Task.Factory.FromAsync(client.BeginSend, client.EndSend, msg, null)
				.ContinueWith(tSend => _logger.Debug("end send"));
		}

		public Task<Subscriber> Subscribe([NotNull] Topic topic,
			SubscriptionDescription description,
			ReceiveMode mode,
			string subscriberName)
		{
			if (topic == null) throw new ArgumentNullException("topic");

			description = description ?? new SubscriptionDescriptionImpl(topic.Description.Path, subscriberName);
			
			// Missing: no mf.BeginCreateSubscriptionClient?
			_logger.Debug(string.Format("being create subscription @ {0}", description));
			return _namespaceManager.TryCreateSubscription(description)
				//.Then(tSbSubDesc => Task.Factory.FromAsync<string, ReceiveMode, MessageReceiver>(
				//    mf.BeginCreateMessageReceiver, mf.EndCreateMessageReceiver,
				//    description.TopicPath, mode, /* state */ _namespaceManager))
				.ContinueWith(tSubDesc => _messagingFactory.CreateSubscriptionClient(description.TopicPath, description.Name, mode))
				.ContinueWith(tMsgR =>
					{
						_logger.Debug(string.Format("end create message receiver @ {0}", description));
						return new SubscriberImpl(tMsgR.Result, description.SubscriptionId,
							() => {
								_logger.Debug(string.Format("begin delete subscription @ {0}", description));
								return _namespaceManager.TryDeleteSubscription(description)
									.ContinueWith(tDeleteSub =>  _logger.Debug(string.Format("end delete subscription @ {0}", description)));
							}) as Subscriber;
					});
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool managed)
		{
			if (!managed || _isDisposed) return;
			_logger.Debug("dispose called");
			_isDisposed = true;
		}
	}
}