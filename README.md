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
 * Maximum performance. A middle ground of low latency (50 ms) and high throughput (batching, concurrent receives). [Ongoing]
 * Polymorphic Publish-Subscribe [Ongoing]
 
## Compiling the code

In order to compile the tests, you have to have a class called `AccountDetails` similar to this:

```
namespace MassTransit.Transports.AzureServiceBus.Configuration
{
	// WARNING: note: DO NOT RENAME this file, or you'll commit the account details

	/// <summary>
	/// 	Configuration handler for account details
	/// </summary>
	public static class AccountDetails
	{
		private static string ENV(string name)
		{
			return Environment.GetEnvironmentVariable(name);
		}

		public static string IssuerName
		{
			get { return ENV("AccountDetailsIssuerName") ?? "owner"; }
		}

		public static string Key
		{
			get { return ENV("AccountDetailsKey") ?? "bjOAWQJalkmd9LKas0lsdklkdw4mAHwKZUJ1jKwTLdc="; }
		}

		public static string Namespace
		{
			get { return ENV("AccountDetailsNamespace") ?? "my-namespace"; }
		}
	}
}
```

Place this file in `src\MassTransit.Transports.AzureServiceBus\Configuration`.