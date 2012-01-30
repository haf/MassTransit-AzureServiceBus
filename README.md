# MassTransit Microsoft Windows Azure AppFabric Service Bus Queues and Topics Message Broker Transport

No, Microsoft **does** have a sane product naming policy. In this case we'll be calling it *MassTransit Service Bus Queues*.

## So wazzup? 

For one thing, this hot moma has **raw, badass WCF** at its heart, so beware of the large crowds of women that will show up outside your doorstep when you use this baby!

## Configuration API

Do it like this, man;

```
using (ServiceBusFactory.New(sbc =>
	{
		sbc.ReceiveFrom("sb-queues://owner:sharedKeyTopSecret@myNamespace/my-application");
		sbc.UseServiceBusQueues();
	}))
{
}
```

or if this tickles your fancy:

```
using (ServiceBusFactory.New(sbc =>
{
	sbc.ReceiveFrom("owner", "sharedKeyTopSecret", "myNamespace", "my-application");
	sbc.UseServiceBusQueues();
}))
{
}
```

# Spec
Aims:

 * Sub-Pub over message types
 * `GetEndpoint(...).Send<T>(this ..., T msg)`.

# Technical notes

Using the MS service bus: harder than expected. Orchestrations of Tasks is what we've done as an API wrapper over the MS API.

## How will routing work?

This section deals with how to handle routing.

### Subscriptions

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

Queue-per-process?

### Thoughts on encryption

We can use the encrypting serializer.