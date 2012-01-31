# MassTransit Microsoft Windows Azure AppFabric Service Bus Queues and Topics Message Broker Transport

No, Microsoft **does** have a sane product naming policy. In this case we'll be calling it *MassTransit Service Bus Queues*.

## So wazzup? 

For one thing, this hot moma has **raw, badass WCF** at its heart, so beware of the large crowds of women that will show up outside your doorstep when you use this baby!

## Configuration API

Do it like this, man;

```
using (ServiceBusFactory.New(sbc =>
	{
		sbc.ReceiveFrom(
			"sb-queues://owner:bjOAWQJalkmd9LKas0lsdklkdw4mAHwKZUJ1jKwTLdc=@myNamespace/my-application"
			);
		sbc.UseServiceBusQueuesRouting();
	}))
{
}
```

or if this tickles your fancy:

```
using (ServiceBusFactory.New(sbc =>
{
	sbc.ReceiveFromComponents("owner", "bjOAWQJalkmd9LKas0lsdklkdw4mAHwKZUJ1jKwTLdc=", 
							  "myNamespace", "my-application");
	sbc.UseServiceBusQueuesRouting();
}))
{
}
```

## Compiling the code

In order to compile the tests, you have to have a class called `AccountDetails` similar to this:

```
namespace MassTransit.Transports.ServiceBusQueues.Configuration
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

Place this file in `src\MassTransit.Transports.ServiceBusQueues\Configuration`.

# Spec
Aims:

 * Sub-Pub over message types
 * `GetEndpoint(...).Send<T>(this ..., T msg)`.

# Technical notes

Using the MS service bus: harder than expected. Orchestrations of Tasks is what we've done as an API wrapper over the MS API.

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

### Endpoints

Queue-per-service bus ReceiveFrom(...) call?

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

The way of sending large messages (over 256K) is to upload it to Azure Blob Storage.

