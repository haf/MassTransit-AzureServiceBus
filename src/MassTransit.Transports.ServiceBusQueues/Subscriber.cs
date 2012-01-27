using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.ServiceBusQueues
{
	public interface Subscriber
	{
		Task<BrokeredMessage> Receive();
		Task<BrokeredMessage> Receive(TimeSpan timeout);
	}
}