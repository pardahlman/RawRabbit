# RawRabbit

[![Build Status](https://img.shields.io/appveyor/ci/pardahlman/rawrabbit.svg?style=flat-square)](https://ci.appveyor.com/project/pardahlman/rawrabbit) [![Documentation Status](https://readthedocs.org/projects/rawrabbit/badge/?version=latest&style=flat-square)](http://rawrabbit.readthedocs.org/) [![NuGet](https://img.shields.io/nuget/v/RawRabbit.svg?style=flat-square)](https://www.nuget.org/packages/RawRabbit) [![GitHub release](https://img.shields.io/github/release/pardahlman/rawrabbit.svg?style=flat-square)](https://github.com/pardahlman/rawrabbit/releases/latest)
[![Slack Status](https://rawrabbit.herokuapp.com/badge.svg)](https://rawrabbit.herokuapp.com)
## Quick introduction
`RawRabbit` is a modern .NET client for communication over [RabbitMq](http://rabbitmq.com/). It is written for [`.NET Core`](http://dot.net) and uses Microsoft’s new frameworks for [logging](https://github.com/aspnet/Logging), [configuration](https://github.com/aspnet/Configuration) and [dependecy injection](https://github.com/aspnet/DependencyInjection). Full documentation available at [`rawrabbit.readthedocs.org`](http://rawrabbit.readthedocs.org/).

### Publish/Subscribe
Setting up publish/subscribe in just a few lines of code.

```csharp
var client = BusClientFactory.CreateDefault();
client.SubscribeAsync<BasicMessage>(async (msg, context) =>
{
  Console.WriteLine($"Recieved: {msg.Prop}.");
});

await client.PublishAsync(new BasicMessage { Prop = "Hello, world!"});
```

### Request/Response
`RawRabbits` request/response (`RPC`) implementation uses the [direct reply-to feature](https://www.rabbitmq.com/direct-reply-to.html) for better performance and lower resource allocation.
```csharp
var client = BusClientFactory.CreateDefault();
client.RespondAsync<BasicRequest, BasicResponse>(async (request, context) =>
{
  return new BasicResponse();
});

var response = await client.RequestAsync<BasicRequest, BasicResponse>();
```
### Message Context
_Message context_ are passed through to the registered message handler. The context is customizable, meaning that domain specific metadata (like _originating country_, _user claims_, _global message id_ etc) can be stored there. In adition to this, `RawRabbit` comes with an `AdvancedMessageContext` that is wired up to support more advanced scenarios

#### Negative Acknowledgement (`Nack`)
The `AdvancedMessageContext` has a method `Nack()` that will perform a [`basic.reject`](https://www.rabbitmq.com/nack.html) for the message.
```csharp
var client = service.GetService<IBusClient<AdvancedMessageContext>>();
client.RespondAsync<BasicRequest, BasicResponse>((req, context) =>
{
    context.Nack(); // the context implements IAdvancedMessageContext.
    return Task.FromResult<BasicResponse>(null);
}, cfg => cfg.WithNoAck(false));
```

#### Requeue/Retry
For some scenarios it makes sense to schedule a message for later handling.
```csharp
subscriber.SubscribeAsync<BasicMessage>(async (msg, context) =>
{
    if (CanNotProcessRightNow(msg))
    {
      context.RetryLater(TimeSpan.FromMinutes(5));
      return;
    }
    // five minutes later, we're here...
});
```
The `AdvancedMessageContext` has  the properties `OriginalSent` and `NumberOfRetries` that can be used to create a _"retry x times then nack"_ strategy.

### Extensions
It is easy to write extensions for `RawRabbit`. The [`RawRabbit.Extensions`](https://www.nuget.org/packages/RawRabbit.Extensions/) NuGet package contains useful extensions, like [`BulkGet`](http://rawrabbit.readthedocs.org/en/master/Bulk-fetching-messages.html) for retrieving multiple messages from multiple queues and `Ack`/`Nack` them in bulk:
```csharp
var bulk = client.GetMessages(cfg => cfg
    .ForMessage<BasicMessage>(msg => msg
        .FromQueues("first_queue", "second_queue")
        .WithBatchSize(4))
    .ForMessage<SimpleMessage>(msg => msg
        .FromQueues("another_queue")
        .GetAll()
        .WithNoAck()
    ));
```

### Customization & Configuration
#### Dependecy Injection
From the very first commit, `RawRabbit` was built with pluggability in mind. Registering custom implementations by using the optional argument when calling `BusClientFactory.CreateDefault`. 

```csharp
var publisher = BusClientFactory.CreateDefault(ioc => ioc
  .AddSingleton<IMessageSerializer, CustomerSerializer>()
  .AddTransient<IChannelFactory, ChannelFactory>()
);
```
#### Specified topology settings
All operations have a optional configuration builder that opens up for granular control over the topology

```csharp
subscriber.SubscribeAsync<BasicMessage>(async (msg, i) =>
{
  //do stuff..
}, cfg => cfg
  .WithRoutingKey("*.topic.queue")
  .WithPrefetchCount(1)
  .WithNoAck()
  .WithQueue(queue =>
    queue
      .WithName("first.topic.queue")
      .WithArgument("x-dead-letter-exchange", "dead_letter_exchange"))
  .WithExchange(exchange =>
    exchange
      .WithType(ExchangeType.Topic)
      .WithAutoDelete()
      .WithName("raw_exchange"))
  );
```
## Project status

[![Throughput Graph](https://graphs.waffle.io/pardahlman/RawRabbit/throughput.svg)](https://waffle.io/pardahlman/RawRabbit/metrics) 
