using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.ServiceBusQueues.Tests.Assumptions
{
	public interface Subscriber
	{
		Task<BrokeredMessage> Receive();
		Task<BrokeredMessage> Receive(TimeSpan timeout);
	}
}