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
// ReSharper disable InconsistentNaming

using System;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Magnum.Extensions;
using Magnum.TestFramework;
using MassTransit.Util;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using NUnit.Framework;

namespace MassTransit.Transports.ServiceBusQueues.Tests.Assumptions
{
	public class When_getting_client_for_non_existent_topic
	{
		private MessagingFactory mf;

		[SetUp]
		public void given_a_messaging_factory()
		{
			mf = TestFactory.CreateMessagingFactory();
			var nm = TestFactory.CreateNamespaceManager(mf);
			nm.TopicExists("my.topic.here").ShouldBeFalse();
		}

		[Test]
		public void returned_topic_client_should_be_non_null()
		{
			var client = mf.CreateTopicClient("my.topic.here");
			client.ShouldNotBeNull();
		}
	}

	/* These could be thrown, wow:
	 * 
	 * ServerBusyException (please DDoS me ASAP!!)
	 * MessagingCommunicationException (???)
	 * TimeoutException (I'm feeling tired today)
	 * MessagingException (something that we cannot determine went wrong)
	 * NotSupportedException (should never happen?)
	 * InvalidOperationException (duplicate send)
	 * MessagingEntityNotFoundException (for random things such as not finding a message in the queue)
	 */

	public class Utils
	{
		static Random r = new Random();

		public static string GenerateRandomName()
		{
			var characters = "abcdefghijklmnopqrstuvwxyz=".ToCharArray();
			var sb = new StringBuilder();
			for (int i = 0; i < 20; i++)
				sb.Append(characters[r.Next(0, characters.Length - 1)]);
			return sb.ToString();
		}
	}

	public class TopicDescriptionImpl : TopicDescription
	{
		readonly Microsoft.ServiceBus.Messaging.TopicDescription _description;

		public TopicDescriptionImpl(Microsoft.ServiceBus.Messaging.TopicDescription description)
		{
			_description = description;
		}

		#region Delegating Members

		public bool IsReadOnly
		{
			get { return _description.IsReadOnly; }
		}

		public ExtensionDataObject ExtensionData
		{
			get { return _description.ExtensionData; }
		}

		public TimeSpan DefaultMessageTimeToLive
		{
			get { return _description.DefaultMessageTimeToLive; }
		}

		public long MaxSizeInMegabytes
		{
			get { return _description.MaxSizeInMegabytes; }
		}

		public bool RequiresDuplicateDetection
		{
			get { return _description.RequiresDuplicateDetection; }
		}

		public TimeSpan DuplicateDetectionHistoryTimeWindow
		{
			get { return _description.DuplicateDetectionHistoryTimeWindow; }
		}

		public long SizeInBytes
		{
			get { return _description.SizeInBytes; }
		}

		public string Path
		{
			get { return _description.Path; }
		}

		public bool EnableBatchedOperations
		{
			get { return _description.EnableBatchedOperations; }
		}

		#endregion

		#region Equality

		public bool Equals(TopicDescription other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Equals(other.DefaultMessageTimeToLive, DefaultMessageTimeToLive)
			       && Equals(other.DuplicateDetectionHistoryTimeWindow, DuplicateDetectionHistoryTimeWindow)
			       && Equals(other.EnableBatchedOperations, EnableBatchedOperations)
			       && Equals(other.ExtensionData, ExtensionData)
			       && Equals(other.IsReadOnly, IsReadOnly)
			       && Equals(other.MaxSizeInMegabytes, MaxSizeInMegabytes)
			       && Equals(other.Path, Path)
			       && Equals(other.RequiresDuplicateDetection, RequiresDuplicateDetection)
			       && Equals(other.SizeInBytes, SizeInBytes);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != typeof (TopicDescription)) return false;
			return Equals((TopicDescription) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = DefaultMessageTimeToLive.GetHashCode();
				result = (result*397) ^ DuplicateDetectionHistoryTimeWindow.GetHashCode();
				result = (result*397) ^ EnableBatchedOperations.GetHashCode();
				result = (result*397) ^ (ExtensionData != null ? ExtensionData.GetHashCode() : 0);
				result = (result*397) ^ IsReadOnly.GetHashCode();
				result = (result*397) ^ MaxSizeInMegabytes.GetHashCode();
				result = (result*397) ^ Path.GetHashCode();
				result = (result*397) ^ RequiresDuplicateDetection.GetHashCode();
				result = (result*397) ^ SizeInBytes.GetHashCode();
				return result;
			}
		}

		#endregion
	}

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

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
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

	public class SubscriberImpl : Subscriber
	{
		readonly MessageReceiver _receiver;

		public SubscriberImpl([NotNull] MessageReceiver receiver)
		{
			if (receiver == null) throw new ArgumentNullException("receiver");
			_receiver = receiver;
		}

		public Task<BrokeredMessage> Receive()
		{
			return Task.Factory.FromAsync<BrokeredMessage>(_receiver.BeginReceive, _receiver.EndReceive, null);
		}

		public Task<BrokeredMessage> Receive(TimeSpan timeout)
		{
			return Task.Factory.FromAsync<TimeSpan, BrokeredMessage>(_receiver.BeginReceive, _receiver.EndReceive, timeout, null);
		}
	}

	public interface TopicClient : IDisposable
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="msg"></param>
		Task Send(BrokeredMessage msg);

		Task<Tuple<UnsubscribeAction, Subscriber>> Subscribe(SubscriptionDescription description = null, 
			ReceiveMode mode = ReceiveMode.PeekLock, string subscriberName = null);
	}

	// value object
	public interface Topic : IEquatable<Topic>
	{
		TopicDescription Description { get; }

		Task Drain();
		
		Task<Tuple<TopicClient, Tuple<UnsubscribeAction, Subscriber>>> 
			CreateClient(ReceiveMode mode = ReceiveMode.PeekLock, string subscriberName = null, bool autoSubscribe = true);

		Task Delete();
	}

	/// <summary>
	/// value object
	/// </summary>
	public interface TopicDescription : IEquatable<TopicDescription>
	{
		bool IsReadOnly { get; }
		ExtensionDataObject ExtensionData { get; }
		TimeSpan DefaultMessageTimeToLive { get; }
		long MaxSizeInMegabytes { get; }
		bool RequiresDuplicateDetection { get; }
		TimeSpan DuplicateDetectionHistoryTimeWindow { get; }
		long SizeInBytes { get; }
		string Path { get; }
		bool EnableBatchedOperations { get; }
	}

	public interface Subscriber
	{
		Task<BrokeredMessage> Receive();
		Task<BrokeredMessage> Receive(TimeSpan timeout);
	}

	// WTF: "Microsoft.ServiceBus.Messaging.MessagingEntityNotFoundException 
	//		: Messaging entity 'mt-client:Topic:mytopic|Olof Reading the News' could not be found..TrackingId:4629cd96-18fa-43ff-8bc7-83c1dddf3912_7_1,TimeStamp:1/26/2012 9:39:07 AM"
	// ???
	// wouldn't it be more prudent to make CreateSubscriptionClient TAKE A TopicDescription??
	[Category("NegativeTests")]
	public class Given_a_sent_message
	{
		protected A message;
		protected NamespaceManager nm;
		protected Topic topic;
		protected TopicClient topicClient;

		[TestFixtureSetUp]
		public void making_sure_the_topic_is_there()
		{
			message = TestFactory.AMessage();

			var mf = TestFactory.CreateMessagingFactory();
			nm = TestFactory.CreateNamespaceManager(mf);
			topic = nm.TryCreateTopic(mf, "my.topic.here");

			var client = topic.CreateClient();
			topic.Drain().Wait();
			topicClient = client.Result.Item1;
		}

		[TestFixtureTearDown]
		public void finally_close_the_client()
		{
			topic.Delete().Wait();
			topicClient.Dispose();
		}

		[SetUp]
		public void given_a_message_sent_to_the_topic()
		{
			var msg = new BrokeredMessage(message);
			BeforeSend(msg);
			topicClient.Send(msg);
		}

		protected virtual void BeforeSend(BrokeredMessage msg)
		{
		}
	}

	public class When_creating_a_subscription_more_than_once_from_same_namespace_manager
	{
		
	}

	public class When_sending_to_topic_with_subscriber_and_tiny_lock_span
		: Given_a_sent_message
	{
		UnsubscribeAction unsubscribe;
		Subscriber subscriber;
		BrokeredMessage msg1;

		protected override void BeforeSend(BrokeredMessage msg)
		{
			var subDesc = new SubscriptionDescription(topic.Description.Path, "Nils Sture listens to the Radio".Replace(" ", "-"))
			{
				EnableBatchedOperations = false,
				LockDuration = 1.Milliseconds(),
				MaxDeliveryCount = 1
			};
			var subscribe = topicClient.Subscribe(subDesc).Result;
			unsubscribe = subscribe.Item1;
			subscriber = subscribe.Item2;
		}

		[SetUp]
		public void given_we_get_first_message()
		{
			msg1 = subscriber.Receive().Result;
			var msg1B = msg1.GetBody<A>();
			msg1B.ShouldEqual(message);
		}

		[TearDown]
		public void Finally_TearDown()
		{
			unsubscribe();
		}

		[Test]
		public void then_the_clients_should_receive_duplicates()
		{
			subscriber.Receive().Result.GetBody<A>().ShouldEqual(message);
			msg1.Complete();
		}
	}

	public class When_sending_to_topic_with_subscriber
		: Given_a_sent_message
	{
		UnsubscribeAction unsubscribe;
		Subscriber subscriber;
		BrokeredMessage msg1;

		protected override void BeforeSend(BrokeredMessage msg)
		{
			var subDesc = new SubscriptionDescription(topic.Description.Path, "Peter Svensson listens to the Radio".Replace(" ", "-"))
				{
					EnableBatchedOperations = true,
					LockDuration = 10.Seconds()
				};
			var subscribe = topicClient.Subscribe(subDesc).Result;
			unsubscribe = subscribe.Item1;
			subscriber = subscribe.Item2;
		}

		[Test]
		public void then_the_client_should_have_one_message_only()
		{
			var msg1 = subscriber.Receive().Result;
			var msg1B = msg1.GetBody<A>();
			msg1B.ShouldEqual(message);

			subscriber.Receive().Result.ShouldBeNull();
			msg1.Complete();
		}
	}

	public class When_sending_end_receiving_on_queue
	{
		Tuple<Action, QueueClient> t;
		A message;

		[SetUp]
		public void when_I_place_a_message_in_the_queue()
		{
			message = TestFactory.AMessage();
			t = TestFactory.SetUpQueue("test-queue");
			t.Item2.Send(new BrokeredMessage(message));
		}

		[Test]
		public void there_should_be_a_message_there_first_time_around_and_return_null_second_time()
		{
			var msg = t.Item2.Receive();
			msg.ShouldNotBeNull();
			try
			{
				var obj = msg.GetBody<A>();
				obj.ShouldEqual(message, "they should have the same contents");
			}
			finally
			{
				if (msg != null)
					msg.Complete();
			}

			var msg2 = t.Item2.Receive(1000.Milliseconds());
			msg2.ShouldBeNull();
		}

		[TearDown]
		public void finally_remove_queue()
		{
			t.Item1();
		}
	}
}