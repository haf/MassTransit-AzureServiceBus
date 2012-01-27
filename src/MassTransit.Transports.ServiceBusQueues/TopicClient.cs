using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.ServiceBusQueues
{
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
}