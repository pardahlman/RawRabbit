# Subscribe

Subscribe is the act of receiving messages and act upon these. In order for a message to trigger the (user defined) message handler method, it has to be published to an exchange that has a queue bound to it with matching routing key to the published message's. By default RawRabbit setup compatible conventions so that any message published without any custom configuration can be subscribed to without any configuration.

## Install Subscribe Operation package

The operation `SubscribeAsync` has its own NuGet package.

```nuget

  PM> Install-Package RawRabbit.Operations.Subscribe
```

Once installed messages can be subscribed to by calling

```csharp
await busClient.SubscribeAsync<BasicMessage>(async message => {
    // handle message here
});
```

## Override default configuration

The configuration can be updated by enriching the pipe context with a `SubscribeConfiguration`

```charp
await busClient.SubscribeAsync<BasicMessage>(async message =>
{
    // handle message here
}, ctx => ctx
    .UseSubscribeConfiguration(cfg => cfg
        .Consume(c => c
            .WithRoutingKey("custom_key")
            .WithConsumerTag("custom_tag")
            .WithPrefetchCount(2)
            .WithNoLocal(false))
        .FromDeclaredQueue(q => q
            .WithName("custom_queue")
            .WithAutoDelete()
            .WithArgument(QueueArgument.DeadLetterExchange, "dlx"))
        .OnDeclaredExchange(e=> e
            .WithName("custom_exchange")
            .WithType(ExchangeType.Topic))
));
```
