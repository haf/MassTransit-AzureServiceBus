using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.AzureServiceBus
{
	public interface TopicClient : IDisposable
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="msg"></param>
		/// <param name="topic"> </param>
		Task Send(BrokeredMessage msg, Topic topic);

		Task<Subscriber> Subscribe(
			Topic subscriptionTopic,
			SubscriptionDescription description = null,
		    ReceiveMode mode = ReceiveMode.PeekLock,
			string subscriberName = null);
	}
}