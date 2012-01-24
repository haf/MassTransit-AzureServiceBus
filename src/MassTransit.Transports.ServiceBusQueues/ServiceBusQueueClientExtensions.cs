using System;
using System.Threading;

namespace MassTransit.Transports.AzureQueue
{
	public static class ServiceBusQueueClientExtensions
	{
		public static void WaitForSubscriptionConformation(this Azure client, string queue)
		{
			var subscribed = false;
			var retryCount = 20;
			var message = "connected to:" + queue;
			var originalMessageHandler = client.OnMessage;

			client.OnMessage = null;
			client.OnMessage = msg => subscribed = msg.Body == message;

			client.Send(queue, message);

			while (!subscribed && retryCount > 0)
			{
				Thread.Sleep(1500);
				retryCount--;
			}

			client.OnMessage = originalMessageHandler;

			if (retryCount == 0)
			{
				throw new InvalidOperationException("Timeout waiting for stomp broker to respond");
			}
		}
	}
}