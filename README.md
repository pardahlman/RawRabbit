_Looking for documentation of 1.x? [Click here](https://github.com/pardahlman/RawRabbit/tree/stable)_
# RawRabbit

[![Build Status](https://img.shields.io/appveyor/ci/pardahlman/rawrabbit.svg?style=flat-square)](https://ci.appveyor.com/project/pardahlman/rawrabbit) [![Documentation Status](https://readthedocs.org/projects/rawrabbit/badge/?version=latest&style=flat-square)](http://rawrabbit.readthedocs.org/) [![NuGet](https://img.shields.io/nuget/v/RawRabbit.svg?style=flat-square)](https://www.nuget.org/packages/RawRabbit) [![GitHub release](https://img.shields.io/github/release/pardahlman/rawrabbit.svg?style=flat-square)](https://github.com/pardahlman/rawrabbit/releases/latest)
[![Slack Status](https://rawrabbit.herokuapp.com/badge.svg)](https://rawrabbit.herokuapp.com)

## Quick introduction
`RawRabbit` is a modern .NET framework for communication over [RabbitMQ](http://rabbitmq.com/). The modular design and middleware oriented architecture makes the client highly customizable while providing sensible default for topology, routing and more. Documentation for version 2.x of the is currently found under [`/docs`](https://github.com/pardahlman/RawRabbit/tree/2.0/docs).

### Configure, enrich and extend

`RawRabbit` is configured with `RawRabbitOptions`, an options object that makes it possible to register client configuration, plugins as well as override internal services

```csharp
var client = RawRabbitFactory.CreateSingleton(new RawRabbitOptions
{
  ClientConfiguration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("rawrabbit.json")
    .Build()
    .Get<RawRabbitConfiguration>(),
  Plugins = p => p
    .UseProtobuf()
    .UsePolly(c => c
        .UsePolicy(queueBindPolicy, PolicyKeys.QueueBind)
        .UsePolicy(queueDeclarePolicy, PolicyKeys.QueueDeclare)
        .UsePolicy(exchangeDeclarePolicy, PolicyKeys.ExchangeDeclare)
    ),
  DependencyInjection = ioc => ioc
    .AddSingleton<IChannelFactory, CustomChannelFactory>()
});
```

### Publish/Subscribe
Set up strongly typed publish/subscribe in just a few lines of code.

```csharp
var client = RawRabbitFactory.CreateSingleton();
await client.SubscribeAsync<BasicMessage>(async msg =>
{
  Console.WriteLine($"Received: {msg.Prop}.");
});

await client.PublishAsync(new BasicMessage { Prop = "Hello, world!"});
```

### Request/Response
`RawRabbits` request/response (`RPC`) implementation uses the [direct reply-to feature](https://www.rabbitmq.com/direct-reply-to.html) for better performance and lower resource allocation.

```csharp
var client = RawRabbitFactory.CreateSingleton();
client.RespondAsync<BasicRequest, BasicResponse>(async request =>
{
  return new BasicResponse();
});

var response = await client.RequestAsync<BasicRequest, BasicResponse>();
```

### Ack, Nack, Reject and Retry

Unlike many other clients, `basic.ack`, `basic.nack` and `basic.reject` are first class citizen in the message handler

```csharp
var client = RawRabbitFactory.CreateSingleton();
await client.SubscribeAsync<BasicMessage>(async msg =>
{
  if(UnableToProcessMessage(msg))
  {
    return new Nack(requeue: true);
  }
  ProcessMessage(msg)
  return new Ack();
});
```

In addition to the basic acknowledgements, RawRabbit also suppoert delayed retries

```csharp
var client = RawRabbitFactory.CreateSingleton();
await client.SubscribeAsync<BasicMessage>(async msg =>
{
  try
  {
    ProcessMessage(msg)
    return new Ack();
  }
  catch (Exception e)
  {
    return Retry.In(TimeSpan.FromSeconds(30));
  }
});
```

### Granular control for each call

Add or change properties in the `IPipeContext` to tailor calls for specific type of messages. This makes it possible to modifly the topology features for calls, publish confirm timeout, consumer concurrency and much more

```csharp
await subscriber.SubscribeAsync<BasicMessage>(received =>
{
  receivedTcs.TrySetResult(received);
  return Task.FromResult(true);
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
