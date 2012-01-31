using System;
using Magnum.TestFramework;
using MassTransit.Transports.ServiceBusQueues.Configuration;
using MassTransit.Transports.ServiceBusQueues.Tests.Assumptions;
using Microsoft.ServiceBus;
using NUnit.Framework;

namespace MassTransit.Transports.ServiceBusQueues.Tests
{
	[Scenario]
	public class When_creating_topics_for_message_names
	{
		NamespaceManager nm;

		[When]
		public void given_a_namespace_manager()
		{
			var mf = ConfigFactory.CreateMessagingFactory();
			nm = ConfigFactory.CreateNamespaceManager(mf);
		}

		[Then]
		[TestCase(typeof(NameEasyToo))]
		[TestCase(typeof(Nested))]
		[TestCase(typeof(NameEasy))]
		[TestCase(typeof(NameGeneric<string>))]
		[TestCase(typeof(NameDoubleGeneric<string, NameEasy>))]
		[TestCase(typeof(NameDoubleGeneric<NameGeneric<double>, NameEasy>))]
		public void app_fabric_service_bus_accepts_these_names(Type messageType)
		{
			var topicPath = new MessageName(messageType);
			var path = topicPath.ToString();
			try
			{
				nm.CreateTopic(path);
			}
			finally
			{
				if (nm.TopicExists(path))
					nm.DeleteTopic(path);
			}
		}

		class Nested
		{
		}
	}
}