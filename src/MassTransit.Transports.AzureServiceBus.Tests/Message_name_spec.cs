using System;
using Magnum.TestFramework;
using NUnit.Framework;

namespace MassTransit.Transports.AzureServiceBus.Tests
{
	[Scenario]
	public class Message_name_spec
	{
		AzureMessageNameFormatter _f;

		[Given]
		public void Message_Name_Formatter()
		{
			_f = new AzureMessageNameFormatter();
		}

		[Then]
		public void Should_handle_an_interface_name()
		{
			_f.GetMessageName(typeof(NameEasyToo)).Name
				.ShouldEqual("MassTransit.Transports.AzureServiceBus.Tests..NameEasyToo");
		}

		[Then]
		public void Should_handle_nested_classes()
		{
			_f.GetMessageName(typeof (Nested)).Name
				.ShouldEqual("MassTransit.Transports.AzureServiceBus.Tests..When_converting_a_type_to_a_message_name-Nested");
		}

		[Then]
		public void Should_handle_regular_classes()
		{
			_f.GetMessageName(typeof (NameEasy)).Name
				.ShouldEqual("MassTransit.Transports.AzureServiceBus.Tests..NameEasy");
		}

		[Then]
		public void Should_throw_an_exception_on_an_open_generic_class_name()
		{
			Assert.Throws<ArgumentException>(() => _f.GetMessageName(typeof(NameGeneric<>)));
		}

		[Then]
		public void Should_handle_a_closed_single_generic()
		{
			_f.GetMessageName(typeof (NameGeneric<string>)).Name
				.ShouldEqual("MassTransit.Transports.AzureServiceBus.Tests..NameGeneric--System..String--");
		}

		[Then]
		public void Should_handle_a_closed_double_generic()
		{
			_f.GetMessageName(typeof (NameDoubleGeneric<string, NameEasy>)).Name
				.ShouldEqual("MassTransit.Transports.AzureServiceBus.Tests..NameDoubleGeneric--System..String....NameEasy--");
		}

		[Then]
		public void Should_handle_a_closed_double_generic_with_a_generic()
		{
			_f.GetMessageName(typeof (NameDoubleGeneric<NameGeneric<NameEasyToo>, NameEasy>)).Name
				.ShouldEqual("MassTransit.Transports.AzureServiceBus.Tests..NameDoubleGeneric--NameGeneric--NameEasyToo--....NameEasy--");
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