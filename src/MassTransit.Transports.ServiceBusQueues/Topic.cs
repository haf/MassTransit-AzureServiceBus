using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.ServiceBusQueues
{
	public interface Topic : IEquatable<Topic>
	{
		TopicDescription Description { get; }

		Task Drain();
		
		Task<Tuple<TopicClient, Tuple<UnsubscribeAction, Subscriber>>> 
			CreateClient(ReceiveMode mode = ReceiveMode.PeekLock, string subscriberName = null, bool autoSubscribe = true);

		Task Delete();
	}
}