using System;
using Magnum.TestFramework;
using NUnit.Framework;

namespace MassTransit.Transports.AzureServiceBus.Tests
{
	[Scenario]
	public class When_converting_a_type_to_a_message_name
	{
		[Then]
		public void Should_handle_an_interface_name()
		{
			var name = new MessageName(typeof(NameEasyToo));
			name.ToString().ShouldEqual("MassTransit.Transports.AzureServiceBus.Tests..NameEasyToo");
		}

		[Then]
		public void Should_handle_nested_classes()
		{
			var name = new MessageName(typeof(Nested));
			name.ToString().ShouldEqual("MassTransit.Transports.AzureServiceBus.Tests..When_converting_a_type_to_a_message_name-Nested");
		}

		[Then]
		public void Should_handle_regular_classes()
		{
			var name = new MessageName(typeof(NameEasy));
			name.ToString().ShouldEqual("MassTransit.Transports.AzureServiceBus.Tests..NameEasy");
		}

		[Then]
		public void Should_throw_an_exception_on_an_open_generic_class_name()
		{
			Assert.Throws<ArgumentException>(() => new MessageName(typeof(NameGeneric<>)));
		}

		[Then]
		public void Should_handle_a_closed_single_generic()
		{
			var name = new MessageName(typeof(NameGeneric<string>));
			name.ToString().ShouldEqual("MassTransit.Transports.AzureServiceBus.Tests..NameGeneric--System..String--");
		}

		[Then]
		public void Should_handle_a_closed_double_generic()
		{
			var name = new MessageName(typeof(NameDoubleGeneric<string, NameEasy>));
			name.ToString().ShouldEqual("MassTransit.Transports.AzureServiceBus.Tests..NameDoubleGeneric--System..String....NameEasy--");
		}

		[Then]
		public void Should_handle_a_closed_double_generic_with_a_generic()
		{
			var name = new MessageName(typeof(NameDoubleGeneric<NameGeneric<NameEasyToo>, NameEasy>));
			name.ToString().ShouldEqual("MassTransit.Transports.AzureServiceBus.Tests..NameDoubleGeneric--NameGeneric--NameEasyToo--....NameEasy--");
		}

		class Nested
		{ }
	}

	class NameEasy
	{
	}

	interface NameEasyToo
	{
	}

	class NameGeneric<T>
	{
	}

	class NameDoubleGeneric<T1, T2>
	{
	}
}