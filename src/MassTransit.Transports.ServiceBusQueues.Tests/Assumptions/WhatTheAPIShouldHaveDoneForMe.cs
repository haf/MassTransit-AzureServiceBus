using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.ServiceBusQueues.Tests.Assumptions
{
	public static class WhatTheAPIShouldHaveDoneForMe
	{
		public static QueueDescription TryCreateQueue(this NamespaceManager nsm, string queueName)
		{
			while (true)
			{
				try
				{
					//if (nsm.GetQueue(queueName) == null) 
					// bugs out http://social.msdn.microsoft.com/Forums/en-US/windowsazureconnectivity/thread/6ce20f60-915a-4519-b7e3-5af26fc31e35
					// says it'll give null, but throws!
					if (!nsm.QueueExists(queueName))
						return nsm.CreateQueue(queueName);
				}
				catch (MessagingEntityAlreadyExistsException)
				{
				}
				try
				{
					nsm.GetQueue(queueName);
				}
				catch (MessagingEntityNotFoundException)
				{
				}
			}
		}


		/// <returns>the topic description</returns>
		public static TopicDescription TryCreateTopic(this NamespaceManager nm, string topicName)
		{
			while (true)
			{
				try
				{
					if (!nm.TopicExists(topicName))
						return nm.CreateTopic(topicName);
				}
				catch (MessagingEntityAlreadyExistsException)
				{
				}
				try
				{
					return nm.GetTopic(topicName);
				}
				catch (MessagingEntityNotFoundException) // someone beat us to removing it
				{
				}
			}
		}
	}
}