using System;
using System.Threading.Tasks;
using MassTransit.Transports.ServiceBusQueues.Internal;
using MassTransit.Util;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using log4net;
using SBSubDesc = Microsoft.ServiceBus.Messaging.SubscriptionDescription;

namespace MassTransit.Transports.ServiceBusQueues
{
	public delegate Task UnsubscribeAction();

	public class TopicClientImpl : TopicClient
	{
		static readonly ILog _logger = LogManager.GetLogger(typeof (TopicClientImpl));
		
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

			_logger.Debug(string.Format("created @ '{0}'", _topic.Description.Path));
		}

		public Task Send(BrokeredMessage msg)
		{
			_logger.Debug("being send");
			return Task.Factory.FromAsync(_inner.BeginSend, _inner.EndSend, msg, null)
				.ContinueWith(tSend => _logger.Debug("end send"));
		}

		public Task<Tuple<UnsubscribeAction, Subscriber>> Subscribe(
			SubscriptionDescription description,
			ReceiveMode mode,
			string subscriberName)
		{
			var mf = _inner.MessagingFactory;
			description = description ?? new SubscriptionDescriptionImpl(_topic.Description.Path, subscriberName ?? Helper.GenerateRandomName());
			
			// Missing: no mf.BeginCreateSubscriptionClient?
			_logger.Debug(string.Format("being create subscription @ {0}", description));
			return Task.Factory
				.FromAsync<SBSubDesc, SBSubDesc>(
					_namespaceManager.BeginCreateSubscription,
					_namespaceManager.EndCreateSubscription,
					description.IDareYou, null)
				//.Then(tSbSubDesc => Task.Factory.FromAsync<string, ReceiveMode, MessageReceiver>(
				//    mf.BeginCreateMessageReceiver, mf.EndCreateMessageReceiver,
				//    description.TopicPath, mode, /* state */ _namespaceManager))
				.ContinueWith(tSubDesc => mf.CreateSubscriptionClient(description.TopicPath, description.Name, mode))
				.ContinueWith(tMsgR =>
					{
						_logger.Debug(string.Format("end create message receiver @ {0}", description));
						var nm = _namespaceManager;
						return Tuple.Create(
							new UnsubscribeAction(() =>
								{
									_logger.Debug(string.Format("begin delete subscription @ {0}", description));
									return Task.Factory
										.FromAsync(nm.BeginDeleteSubscription, nm.EndDeleteSubscription,
											description.TopicPath, description.Name, null)
										.ContinueWith(tDeleteSub => 
											_logger.Debug(string.Format("end delete subscription @ {0}", description)));
								}), 
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

			_logger.Debug("dispose called");

			if (_isDisposed)
				throw new ObjectDisposedException("TopicClientImpl", "cannot dispose twice");
			
			if (!_inner.IsClosed)
				_inner.Close();

			_isDisposed = true;
		}
	}
}