using Magnum.TestFramework;
using MassTransit.Transports.AzureServiceBus.Configuration;

namespace MassTransit.Transports.AzureServiceBus.Tests
{
	public class Credentials_impl_spec
	{
		Credentials subject;

		[Given]
		public void creds()
		{
			subject = new Credentials("owner", "key", "ns", "appx");
		}

		[Then]
		public void should_have_correct_issuer_name()
		{
			subject.IssuerName.ShouldEqual("owner");
		}

		[Then]
		public void should_have_correct_key()
		{
			subject.Key.ShouldEqual("key");
		}

		[Then]
		public void should_have_correct_ns()
		{
			subject.Namespace.ShouldEqual("ns");
		}

		[Then]
		public void should_have_correct_app()
		{
			subject.Application.ShouldEqual("appx");
		}
	}
}