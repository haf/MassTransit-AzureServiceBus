using System;
using System.Threading.Tasks;
using MassTransit.Util;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.ServiceBusQueues.Tests.Assumptions
{
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
			var send = Task.Factory.FromAsync(_inner.BeginSend, _inner.EndSend, msg, null);
			send.Start();
			return send;
		}

		public Task<Tuple<UnsubscribeAction, Subscriber>> Subscribe(SubscriptionDescription description, ReceiveMode mode, string subscriberName)
		{
			var mf = _inner.MessagingFactory;
			description = description ?? new SubscriptionDescription(_topic.Description.Path, subscriberName ?? Utils.GenerateRandomName());

			return Task.Factory.FromAsync<string, MessageReceiver>(
				mf.BeginCreateMessageReceiver, mf.EndCreateMessageReceiver,
				description.TopicPath, _namespaceManager)
				.ContinueWith(tMsgR =>
					{
						var nm = tMsgR.AsyncState as NamespaceManager;
						return Tuple.Create(new UnsubscribeAction(() =>
							{
								nm.DeleteSubscription(description.TopicPath, description.Name);
								return true;
							}), new SubscriberImpl(tMsgR.Result) as Subscriber);
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