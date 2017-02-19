# Message Context

The _Message Context_ is an object that accompanies the actual message. In earlier versions of RawRabbit, the Message Context was mandatory and had to implement the interface `IMessageContext`. Since 2.0, this is an optional feature.

## What makes sense to pass in the Message Context?

It depends on the application. If messages are published on behalf of an authenticated user, it might contain claims or tokens that can be used to verify access rights. If the message is published as part of a scheduled job, it could contain a job id, initiator and more. As a rule of thumb, it makes sense to add properties there that describes the domain or context.

## Outgoing messages
Message Context is enabled for outgoing messages by registering it as a plugin

```csharp
new RawRabbitOptions
{
    Plugins = p => p
        .UseMessageContext(ctx => new CustomContext
        {
            MachineName = Environment.MachineName,
            Sent = DateTime.Now
        })
}
```

If no factory method is provided, the message context is just new’ed up.

The outgoing message context can be overridden when the message is published. Note that the plugin must be registered for that to work.

```csharp
await publisher.PublishAsync(new BasicMessage(), ctx => ctx
    .UseMessageContext(new AnotherContext {
        SomeProp = "SomeValue"
    }));
```

## Incomming messages

The Subscribe and Respond operation each have dedicated enrichers that adds new methods to the bus client for consuming a message with message context

```csharp
await c.SubscribeAsync<BasicMessage, MessageContext>(async (message, context) =>
{
    //code goes here
});
```

If desired, the message context passed in to the message handler can be replaced by any property found in the pipe.

```csharp
await client.SubscribeAsync<BasicMessage, BasicDeliverEventArgs>(async (msg, args) =>
{
    if (args.Redelivered) // example usage of delivery args.
    {
        return new Nack();
    }
    // code goes here
    return new Ack();
}, ctx => ctx.UseMessageContext(c => c.GetDeliveryEventArgs()));
```
In the example above, the subscriber method gets access to the entire `BasicDeliveryEventArgs`, giving access to headers, body and other metadata.

## Message Context Forwarding

RawRabbit can be configured to forward received message contexts in publishes that happens within the message handler.

```csharp
await firstClient.SubscribeAsync<FirstMessage, CustomContext>((async msg, context) =>
{
    // the context here...
    await subscriber.PublishAsync(new SecondMessage());
});

await secondClient.SubscribeAsync<SecondMessage, CustomContext>((async msg, context) =>
{
    // ...is the same as here.
});
```

The feature is enabled by register it as a plugin. As of the current version, both `UseMessageContext<TMessageContext>` and `UseContextForwarding` needs to be registed for this to work.

```csharp
new RawRabbitOptions
{
    Plugins = p => p
        .UseMessageContext<TodoContext>()
        .UseContextForwarding()
}
```