using System;
using System.Threading.Tasks;
using MassTransit.Util;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.ServiceBusQueues.Tests.Assumptions
{
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
}