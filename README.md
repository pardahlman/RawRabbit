# RawRabbit
                                                        |\    
                                                       / |    
                                             ,       .' /|    
                                            .'\     /  //     
                                           /   \    |  ||     
                                          |   | \,--' ;/      
                                          \  /\ '    `|       
                                           \/ / _   _ (       
                                            .'  a\_/a '.      
                                           { =   <_>   =}     
                                            '-.  _T_ .-'      
                                             .'`----;         
                                           .'        \        
                                          /           ;       
                                         /      ,     |       
                                        ;      -.|    |       
                                      _ |         \  \/       
                                     ( '|         |\  \\      
                                    (   \        /.-\  \\__   
                                     '--'`._  .-'---,\  '--;  
                                          '--------^^ '--^^   
_No über complex abstractions! No configuration needed (but supported for those who want to have 100% control). Just a thin, layer above the [dotnet RabbitMq client](https://github.com/rabbitmq/rabbitmq-dotnet-client)._

* _Publish_, _subscribe_, and _request_/_response_ (a.k.a `RPC`) async
* Targets [dnx runtime](https://github.com/aspnet/dnx) `dnx451` as well as `net451`
* Everything is plugable! Register any custom types with dependecy injection from `Microsoft.Framework.DependencyInjection`

## Quick introduction
The ambision with `RawRabbit` is to provide a simple interface for performing some of the basic things that you typically want to do when working with RabbitMq. Therefore, it is shipped with out-of-the-box functionality for _Publish/Suscibe_ and _Request/Response_. For more information about how to customization, keep on reading.
### Publish & Subscribe
```csharp
// setup subscriber
var subscriber = BusClientFactory.CreateDefault();
await subscriber.SubscribeAsync<BasicMessage>(async (msg, info) =>
{
  Console.WriteLine($"Recieved: {msg.Prop}.");
});

// publish message
var publisher = BusClientFactory.CreateDefault();
await publisher.PublishAsync(new BasicMessage { Prop = "Hello, world!"});
```
### Request & Response
To perform a remote procedure call, create a client that knows how to _respond_ to the specific message type and have another client perform a _request_.

```csharp
// instanciate clients
var requester = BusClientFactory.CreateDefault();
var responder = BusClientFactory.CreateDefault();

// wire up responder
await responder.RespondAsync<BasicRequest, BasicResponse>(async (request, context) =>
{
  return new BasicResponse();
});

// send request
var recieved = await requester.RequestAsync<BasicRequest, BasicResponse>();
```
### Detailed Configuration
`RawRabbit` comes with a set of sensible standard configuration, but sometimes that is not enought. Therefore, all the operations you can do has an optional parameter where you get get nitty-gritty in the details. Here's an example on how you get full controll over queues, exchanges and consumers used:
```csharp
await subscriber.SubscribeAsync<BasicMessage>(async (msg, i) =>
{
  //do stuff..
}, cfg => cfg
  .WithRoutingKey("*.topic.queue")
  .WithPrefetchCount(1)
  .WithNoAck()
  .WithQueue(queue =>
    queue
      .WithName("first.topic.queue")
      .WithAutoDelete()
      .WithDurability()
      .WithArgument("x-dead-letter-exchange", "dead_letter_exchange"))
  .WithExchange(exchange =>
    exchange
      .WithType(ExchangeType.Topic)
      .WithAutoDelete()
      .WithName("raw_exchange"))
  );
```

## Customize Client
From the very first commit, `RawRAbbit` was built with pluggability in mind. Whether or not you want to have controll over how `IConnection` and `IModel` is provided to the client, or if you just want to change the default names of queues or exchanges it all happends in classes that implement interfaces, and you are free to add you own implementation to it. The easist way is to use the optional argument for `BusClientFactory` and just replace the standard implementation with you custom one. 
```csharp
var publisher = BusClientFactory.CreateDefault(ioc => ioc
  .AddSingleton<IMessageSerializer, CustomerSerializer>()
  .AddTransient<IChannelFactory, ChannelFactory>()
);
```
`RawRabbit` uses the `Microsoft.Framework.DependencyInjection` for dependecy injection, which all the major IoC framework uses. It is therefor super-easy to resolve your favorite flavour of the bus client by just passing you `IServiceProvider` to the `CreateDefault` call.
```csharp
var autofacBuilder = new ContainerBuilder();
autofacBuilder
  .RegisterModule<RawRabbitModule>()
  .RegisterModule<AnotherModule>();
var container = autofacBuilder.Build();
var autoClient = BusClientFactory.CreateDefault(container.Resolve<IServiceProvider>());
```