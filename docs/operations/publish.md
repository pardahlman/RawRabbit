# Publish

One of the fundamental operations in RabbitMQ is publishing messages. In order to do so, an exchange needs to be declared onto which the messages are published. The exchange, in turn, has topological features and will probably have one or more queues bound to it - with specific routing keys - in order for messages to be consumed downstream. Simply put, there are alot of configuration that needs to be done, and it is not allways straight forward. RawRabbit solves this by supplying sensible default while providing ways to override just about anything.

## Install Publish Operation packge

the operation `PublishAsync` has its own NuGet package.

```nuget

  PM> Install-Package RawRabbit.Operations.Publish
```

One installed the message can be published by calling

```csharp
await busClient.PublishAsync(new BasicMessage
{
    Prop = "Hello, world!"
});
```

## Override default configuration

Overrides of default configuration can be performed on several levels:

* Naming convention of exchanges and queues can be changed by registering a custom `INamingConvention`
* Routing key convention can be changed by registering a custom `IPublisherConfigurationFactory`

The configuration can also be updated per call by enriching the pipe context with a `PublishConfiguration`

```csharp
var message = new BasicMessage { Prop = "Hello, world!" };

await busClient.PublishAsync(message, ctx => ctx
    .UsePublishConfiguration(cfg => cfg
        .OnDeclaredExchange(e => e
            .WithName("my_topic")
            .WithType(ExchangeType.Topic))
        .WithRoutingKey("my_key")
));
```
