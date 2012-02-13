# MassTransit AzureServiceBus Transport

A transport over AppFabric Service Bus in Azure. Currently capable of pub-sub and sending to endpoints.

## So wazzup? 

For one thing, this hot moma has **raw, badass WCF** at its heart, so beware of the large crowds of women that will show up outside your doorstep when you use this baby!

First, get your Azure account here: https://www.windowsazure.com/en-us/develop/net/how-to-guides/service-bus-topics/#what-is. It's free the first three months.

## Configuration API

Do it like this, man;

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

or if this tickles your fancy:

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

## Running Perf-tests

If you wish to run the performance tests, you will have to change `src\MassTransit.AzurePerformance\ServiceConfiguration.Cloud.cscfg` to reflect your own values. This is what it looks for me:

```
<?xml version="1.0" encoding="utf-8"?>
<ServiceConfiguration serviceName="MassTransit.AzurePerformance.Sender" xmlns="http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration" osFamily="1" osVersion="*">
	<Role name="MassTransit.AzurePerformance.Receiver">
		<Instances count="1" />
		<ConfigurationSettings>
			<Setting name="Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" value="DefaultEndpointsProtocol=https;AccountName=[YOUR ACCOUNT NAME];AccountKey=[YOUR ACCOUNT KEY]" />
			<Setting name="RampUpCount" value="50"/>
			<Setting name="SampleSize" value="5000"/>
			<Setting name="WaitForNumberOfSenders" value="4"/>
		</ConfigurationSettings>
	</Role>
	<Role name="MassTransit.AzurePerformance.Sender">
		<Instances count="4" />
		<ConfigurationSettings>
			<Setting name="Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" value="DefaultEndpointsProtocol=https;AccountName=[YOUR ACCOUNT NAME];AccountKey=[YOUR ACCOUNT KEY]" />
		</ConfigurationSettings>
	</Role>
</ServiceConfiguration>
```

# Spec
Aims:

 * Sub-Pub over message types
 * `GetEndpoint(...).Send<T>(this ..., T msg)`. [Done]

## How will routing work?

This section deals with how to handle routing.

### Subscriptions

Because SBQS provides subscriptions with topics, we'll try to use those.

Subscriptions subscribe to the *topic* of the URI-normalized message type name. Single MT subscription subscribes to hierarchy by means of multiple SBQS subscription. E.g.

`namespace A { class B : C { B(){} } interface C {} }`

then 

`bus.Subscribe(s => s.Handler<B>( b => ... ));`

causes

<table>
    <tr>
        <td>&nbsp;</td>
        <td><b>SBQS topic</b></td>
        <td><b>Subscriber</b></td>
    </tr>
    <tr>
        <td>1.</td>
        <td>A.B</td>
        <td>Mps.App1*</td>
    </tr>
    <tr>
        <td>2.</td>
        <td>A.C</td>
        <td>Mps.App1</td>
    </tr>
</table>

\* *Mps.App1* equal to the queue that we use for sending messages directly to *App1*. *Mps* is a namespace.

So if B didn't implement C:

<table>
    <tr>
        <td>&nbsp;</td>
        <td><b>SBQS topic</b></td>
        <td><b>Subscriber</b></td>
    </tr>
    <tr>
        <td>1.</td>
        <td>A.B</td>
        <td>Mps.App1*</td>
    </tr>
</table>

E.g. `bus.Publish(new B())` instance *b1* causes state:

<table><tr><td><b>Topic</b></td><td><b>Messages</b></td></tr>
<tr><td>A.B</td><td>{ b1 }</td></tr>
<tr><td>A.C</td><td>{ b1 }</td></tr>
</table>

*App1* subscribes to both these queues because of polymorphic routing. This is problematic because b1 needs to be de-duped at receiver. **Fine** - let's finish the dedup spike and make it an MT service.

**Currently Pub/Sub isn't implemented - because of the above I'm trying to decide whether to use topics or the MT subscription service**.

## On communication w/ AzureServiceBus

 * **LockDuration** - default 30 s
 * **MaxSizeInMegabytes** - default 1024 MiB
 * **RequiresDuplicateDetection** - defaults to false, duplicates are not vetted on message broker
 * **RequiresSession** -  defaults to false, we may have multiple `MessageFactory` instances in a single transport for performance
 * **DefaultMessageTimeToLive** - default
 * **EnableDeadLetteringOnMessageExpiration** - default
 * **EnableBatchedOperations** - non default - **true** - will batch **50 ms** on receive from queue.
 
Further:

 * Currently the project uses async receive and sync send. This is aimed to change to async everything, but requires changes to MT to support the asynchronous flow that is required (with its connection handler and connection policies).
 * Outbound allows for **100 messages in flight** concurrently on async send.
 * Outbound retries messages met with **ServerBusyException** after **10 s**.
 * Transports log to *MassTransit.Messages*.
 
Aiming to bring in log4net appender for Azure.

## Endpoints

Endpoint in MT, tuple: { bus instance, inbound queue/transport, n x outbound queue/transport (one for each target we're sending to) }. In this case the endpoint needs to be, tuple { bus instance, inbound queue, n x inbound subscriptions that is polled round robin, n x outbound queue/transport (`GetEndpoint().Send()`), n x outbound subscription client (send to topic) }

## Thoughts on encryption

We can use the encrypting serializer, like what is displsyed in `PreSharedKeyEncryptedMessageSerializer` in MT. We'll set keys as we set endpoint names, using configuration management.

## Thoughts on reliability

Service Bus doesn't garantuee the uptime that one would get off e.g.
a dedicated Rabbit MQ HA cluster:

 * Forum topic [Azure Service Bus stability](http://social.msdn.microsoft.com/Forums/en-US/windowsazureconnectivity/thread/c1a37655-47ed-47b0-8853-5132330d8213)

It could be possible to create a mechanism for switching to an alternate transport mechanism if the one in use fails. Perhaps failover to ZeroMQ transport if SBQS fails?

This transport will rate-limit on the persistency/quorum of service bus itself; i.e. when a message is considered safe - ZMQ wouldn't write to disk, but rather send the message asynchronously directly, so for example, given messages { 1,2,3,4,5 }, this transport could send { 1, receive ACK 1, 2 receive ACK 2, 3, receive ACK 3, ... } or optionally, { 1, 2, receive ACK 1, 3, 4, 5, receive ACK 2, receive ACK 3, ... }. ZeroMQ would do { 1, 2, 3, 4, 5 } and we'd never know from a sending app whether they arrived. It would be up to the producer to persist its current ACKed message for service bus and the ZMQ transport SHOULD have a PUSH/PULL socket that returns with an ACKs with the message id.

## Thoughts on sending medium/large messages

 * Forum topic [BrokeredMessage 256KB limit](http://social.msdn.microsoft.com/Forums/en-US/windowsazureconnectivity/thread/b804b71e-831d-43b6-a38c-847d01034471)

The way of sending large messages (over 256K) is to upload it to Azure Blob Storage. Another way is to chunk it.

## Thoughts on ServerTooBusyException - retry in 10 seconds

Hmm... 10 seconds: that's a couple of thousand messages, a long time.

## Thoughts on maximum message factories per namespace

Only 100??