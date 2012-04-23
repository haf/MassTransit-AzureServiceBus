# MassTransit AzureServiceBus Transport

A transport over AppFabric Service Bus in Azure. Currently capable of pub-sub and sending to endpoints.

### First, get your Azure account 

https://www.windowsazure.com/en-us/develop/net/how-to-guides/service-bus-topics/#what-is. It's free the first three months.

### Second, configure MassTransit with the transport

```
using (ServiceBusFactory.New(sbc =>
	{
		sbc.ReceiveFrom(
			"azure-sb://owner:bjOAWQJalkmd9LKas0lsdklkdw4mAHwKZUJ1jKwTLdc=@myNamespace/my-application"
			);
		sbc.UseAzureServiceBusRouting();
	}))
{
	// use the bus here...
}
```

or alternatively:

```
using (ServiceBusFactory.New(sbc =>
{
	sbc.ReceiveFromComponents("owner", "bjOAWQJalkmd9LKas0lsdklkdw4mAHwKZUJ1jKwTLdc=", 
							  "myNamespace", "my-application");
	sbc.UseAzureServiceBusRouting();
}))
{
	// use the bus here...
}
```

## Aims:

 * `GetEndpoint(...).Send<T>(this ..., T msg)`. [Done]
 * Sub-Pub over message types [Done]
 * Binding to specific topic on publish of message if subscriber exists in bus. [Done]
 * Purging of queues by deleting them and re-creating them. [Done]
 * Smooth asynchronous transient fault handling [Done] (AsyncRetry.fs)
 * Maximum performance. A middle ground of low latency (50 ms) and high throughput (batching, concurrent receives). [Done]
 * Polymorphic Publish-Subscribe [Ongoing] 
 
## Compiling the code

In order to compile the tests, you have to have a class called `AccountDetails` similar to this:

```
using System;
using MassTransit.Transports.AzureServiceBus.Configuration;

namespace MassTransit.Transports.AzureServiceBus.Tests.Framework
{
	// WARNING: note: DO NOT RENAME this file, or you'll commit the account details

	/// <summary>
	/// 	Actual AccountDetails
	/// </summary>
	public class AccountDetails
		: PreSharedKeyCredentials
	{
		static readonly PreSharedKeyCredentials _instance = new Credentials(IssuerName, Key, Namespace, Application);

		private static string ENV(string name)
		{
			return Environment.GetEnvironmentVariable(name);
		}

		public static string Key
		{
			get { return ENV("AccountDetailsKey") ?? "adfmald0f9j23lkmsd+Ww4mAHwKZUJ1jKwTLdc="; }
		}

		public static string IssuerName
		{
			get { return ENV("AccountDetailsIssuerName") ?? "owner"; }
		}

		public static string Namespace
		{
			get { return ENV("AccountDetailsNamespace") ?? "mt"; }
		}

		public static string Application
		{
			get { return ENV("Application") ?? "my-application"; }
		}

		string PreSharedKeyCredentials.IssuerName
		{
			get { return _instance.IssuerName; }
		}

		string PreSharedKeyCredentials.Key
		{
			get { return _instance.Key; }
		}

		string PreSharedKeyCredentials.Namespace
		{
			get { return _instance.Namespace; }
		}

		string PreSharedKeyCredentials.Application
		{
			get { return Application; }
		}

		public Uri BuildUri(string application = null)
		{
			return _instance.BuildUri(application);
		}

		public PreSharedKeyCredentials WithApplication(string application)
		{
			return _instance.WithApplication(application);
		}
	}
}
```

Place this file in `src\MassTransit.Transports.AzureServiceBus.Tests\Framework\AccountDetails.cs`.