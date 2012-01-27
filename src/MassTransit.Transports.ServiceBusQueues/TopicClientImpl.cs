using System;
using System.Threading.Tasks;
using MassTransit.Util;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.ServiceBusQueues.Tests.Assumptions
{
	public delegate Task UnsubscribeAction();

	public class TopicClientImpl : TopicClient
	{
		readonly Microsoft.ServiceBus.Messaging.TopicClient _inner;
		readonly NamespaceManager _namespaceManager;
		readonly Topic _topic;
		bool _isDisposed;

		public TopicClientImpl([NotNull] Microsoft.ServiceBus.Messaging.TopicClient inner,
		                       [NotNull] NamespaceManager namespaceManager, 
		                       [NotNull] Topic topic)
		{
			if (inner == null) throw new ArgumentNullException("inner");
			if (namespaceManager == null) throw new ArgumentNullException("namespaceManager");
			if (topic == null) throw new ArgumentNullException("topic");
			_inner = inner;
			_namespaceManager = namespaceManager;
			_topic = topic;
		}

		public Task Send(BrokeredMessage msg)
		{
			return Task.Factory.FromAsync(_inner.BeginSend, _inner.EndSend, msg, null);
		}

		public Task<Tuple<UnsubscribeAction, Subscriber>> Subscribe(
			SubscriptionDescription description,
			ReceiveMode mode,
			string subscriberName)
		{
			var mf = _inner.MessagingFactory;
			description = description ?? new SubscriptionDescription(_topic.Description.Path, subscriberName ?? Utils.GenerateRandomName());
			
			// Missing: no mf.BeginCreateSubscriptionClient? Where do I use mode?
			return Task.Factory
				.FromAsync<string, MessageReceiver>(
					mf.BeginCreateMessageReceiver, mf.EndCreateMessageReceiver,
					description.TopicPath, /* state */ _namespaceManager)
				.ContinueWith(tMsgR =>
					{
						var nm = tMsgR.AsyncState as NamespaceManager;
						return Tuple.Create(
							new UnsubscribeAction(() =>
								Task.Factory.FromAsync(nm.BeginDeleteSubscription, nm.EndDeleteSubscription,
													   description.TopicPath, description.Name, null)), 
							new SubscriberImpl(tMsgR.Result) as Subscriber);
					});
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool managed)
		{
			if (!managed) return;
			
			if (_isDisposed)
				throw new ObjectDisposedException("TopicClientImpl", "cannot dispose twice");
			
			if (!_inner.IsClosed)
				_inner.Close();

			_isDisposed = true;
		}
	}
}