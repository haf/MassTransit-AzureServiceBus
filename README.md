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

## Future:

 * Add topics routing for subscriptions
 * Generally make anything work...