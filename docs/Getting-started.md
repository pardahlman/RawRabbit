# Getting Started

## Installation

Install the latest version of [`RawRabbit`](https://www.nuget.org/packages/RawRabbit/) and [`RawRabbit.vNext`](https://www.nuget.org/packages/RawRabbit.vNext/) from NuGet.

```nuget

  PM> Install-Package RawRabbit
  PM> Install-Package RawRabbit.vNext

```
The `vNext` package contains the convenience class `BusClientFactory` that can be used to create a default instance of the `RawRabbit` client. It makes life easier, but is not necesary.

## Creating instanse
Depending on the scenario, there are a few different ways to instansiate the `RawRabbit` client. The methods described below all have optional arguments for registering specific subdependeices.

### vNext Application wire-up
If the application is bootstrapped from a `vNext` application, the dependecies and client can be registed by using the `AddRawRabbit` extension for `IServiceCollection`

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddRawRabbit(); //optional overrides here, too.
}
```
### Instance from factory

Create a new client by calling `BusClientFactory.CreateDefault`. If no arguments are provided, the local configuration will be used (`guest` user on `localhost:5672` with virtual host `/`).

```csharp
var raw = BusClientFactory.CreateDefault();
```

### Autofac

The package [`RawRabbit.DependencyInjection.Autofac`](https://www.nuget.org/packages/RawRabbit.DependencyInjection.Autofac) contains modules and extension methods for registering `RawRabbit`.

```csharp
var builder = new ContainerBuilder();
builder.RegisterRawRabbit("guest:guest@localhost:5672/");
var container = builder.Build();
``` 

## Broker connection
As soon as the client is instansiated, it will try to connect to the broker.  By default `RawRabbit` will try to connect to `localhost`. Configuration can be provided in different ways.

### Configuration object
The main configuration object for `RawRabbit` is `RawRabbitConfiguration`.
```csharp
var config = new RawRabbitConfiguration
{
	Username = "user",
	Password = "password",
	Port = 5672,
	VirtualHost = "/vhost",
	Hostnames = { "production" }
	// more props here.
};
var client = BusClientFactory.CreateDefault(config);
``` 

Configuration can be supplied in configuration files. See the [configuration section](configuration.html) for more information.

## Messaging pattern
Two of the main messaging patterns for RabbitMq are [remote procedure calls](https://www.rabbitmq.com/tutorials/tutorial-six-dotnet.html) (sometimes refered to as `RPC` or _request/reply_) and [publish/subscribe](https://www.rabbitmq.com/tutorials/tutorial-three-dotnet.html).

### Publish/Subscribe
Implementing the publish/subscribe pattern can be done with just a few lines of code. The `SubscribeAsyn<TMessage>` method takes one argument `Func<TMessage,TMessageContext,Task>` that will be invoked as the message is recived. Read more about the `TMessageContext` in the [Message Context](fixme) section. Publish a message by calling `PublishAsync<TMessage>` with an instance of the message as argument.
```csharp
var client = BusClientFactory.CreateDefault();
client.SubscribeAsync<BasicMessage>(async (msg, context) =>
{
  Console.WriteLine($"Recieved: {msg.Prop}.");
});

await client.PublishAsync(new BasicMessage { Prop = "Hello, world!"});
```
### Request/Reply
Similar to [publish/subscribe](#publish-subscribe), the message handler for a `RequestAsync<TRequest, TResponse>` in invoked with the request and message context. It returns a `Task<TResponse>` that is sent back to the waiting requester.

```csharp
var client = BusClientFactory.CreateDefault();
client.RespondAsync<BasicRequest, BasicResponse>(async (request, context) =>
{
  return new BasicResponse();
});

var response = await client.RequestAsync<BasicRequest, BasicResponse>();
```
### Other patterns
While publish/subscribe and request/reply lays in the core of `RawRabbit`, there are other ways to work with messages. The [BulkGet extension](Bulk-fetching-messages.html) (from NuGet `RawRabbit.Extensions`) allows for retrieving multiple messages from multiple queues and `Ack`/`Nack` them in bulk:
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