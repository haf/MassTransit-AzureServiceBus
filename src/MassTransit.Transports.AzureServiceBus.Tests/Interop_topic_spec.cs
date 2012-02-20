using System;
using Magnum.TestFramework;
using MassTransit.Transports.AzureServiceBus.Tests.Framework;
using Microsoft.ServiceBus;
using NUnit.Framework;

namespace MassTransit.Transports.AzureServiceBus.Tests
{
	[Scenario]
	public class Interop_topic_spec
	{
		NamespaceManager nm;
		MessageNameFormatter _formatter;

		[When]
		public void theres_a_namespace_manager_available()
		{
			var mf = TestConfigFactory.CreateMessagingFactory();
			nm = TestConfigFactory.CreateNamespaceManager(mf);
		}

		[Given]
		public void a_message_name_formatter()
		{
			_formatter = new MessageNameFormatter();
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
			var mname = _formatter.GetMessageName(messageType);
			try
			{
				nm.CreateTopic(mname.Name);
			}
			finally
			{
				if (nm.TopicExists(mname.Name))
					nm.DeleteTopic(mname.Name);
			}
		}

		class Nested
		{
		}
	}
}