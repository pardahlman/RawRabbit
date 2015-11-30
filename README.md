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
_A modern, vNext based, C# framework for communication over [RabbitMq](http://rabbitmq.com/), based on [the official dotnet RabbitMq client](https://github.com/rabbitmq/rabbitmq-dotnet-client)._

* Lightning  fast `async` Request/Response with [direct reply-to](https://www.rabbitmq.com/direct-reply-to.html).
* [`dead-letter-exchange`](https://www.rabbitmq.com/dlx.html) based retry/delay.
* Out-of-the-box support for `Nack` messages.
* Targets [dnx runtime](https://github.com/aspnet/dnx) `dnx451` to `dnx50` as well as `net451` to `net50`
* Everything is plugable! Register any custom types with dependecy injection from [`Microsoft.Extensions.DependencyInjection`](https://github.com/aspnet/DependencyInjection)
* Easy-piecy configuration with [`Microsoft.Extensions.Configuration`](https://github.com/aspnet/Configuration) (but support for old skool, too)

## Quick introduction
The ambision with `RawRabbit` is to provide a simple interface for performing some of the basic things that you typically want to do when working with RabbitMq. Performing standard _Publish/Suscibe_ and _Request/Response_ is easy. In fact, you don't need to configure anything. _Like the controll you get by configuring yourself? Keep on reading!_
### Publish & Subscribe
```csharp
// setup subscriber
var subscriber = BusClientFactory.CreateDefault();
await subscriber.SubscribeAsync<BasicMessage>(async (msg, context) =>
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
### Detailed Control
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
From the very first commit, `RawRabbit` was built with pluggability in mind. Whether or not you want to have controll over how `IConnection` and `IModel` is provided to the client, or if you just want to change the default names of queues or exchanges it all happends in classes that implement interfaces, and you are free to add you own implementation to it. The easist way is to use the optional argument for `BusClientFactory` and just replace the standard implementation with you custom one. 
```csharp
var publisher = BusClientFactory.CreateDefault(ioc => ioc
  .AddSingleton<IMessageSerializer, CustomerSerializer>()
  .AddTransient<IChannelFactory, ChannelFactory>()
);
```
`RawRabbit` uses the `Microsoft.Extensions.DependencyInjection` for dependecy injection, which all the major IoC framework uses. It is therefor super-easy to register and resolve your favorite flavour of the bus client by calling `AddRawRabbit()` on you `IServiceCollection`.
```csharp
public void ConfigureServices(IServiceCollection services)
{
  services.AddRawRabbit(); //optional overrides here, too.
}
```
### Advanced concepts
In the default client, messages are _ack_'ed once the message handler has executed. There might be some scenarios where you want to _nack_ the message and let another consumer handle the message. This is easily done by registrating bus client that uses an `IAdvancedMessageContext`

```csharp
var client = service.GetService<IBusClient<AdvancedMessageContext>>();
client.RespondAsync<BasicRequest, BasicResponse>((req, ctx) =>
{
  ctx?.Nack(); // the context implements IAdvancedMessageContext.
  return Task.FromResult<BasicResponse>(null);
}, cfg => cfg.WithNoAck(false));
```
_For implementation info check the [`NackTests`](https://github.com/pardahlman/RawRabbit/blob/master/src/RawRabbit.IntegrationTests/Features/NackingTests.cs)._

There are times where you're unable to process a message right away, and might want to retry at a later time. This approach can be employed as error handling for when a message handler throws exception. `RawRabbit`'s advanced context does also have methods for retrying after given `TimeSpan`. It follows [yuserinterface's solution](http://yuserinterface.com/dev/2013/01/08/how-to-schedule-delay-messages-with-rabbitmq-using-a-dead-letter-exchange/) that leverages the dead-letter-exchange functionality .

```csharp
subscriber.SubscribeAsync<BasicMessage>(async (message, ctx) =>
{
  if (CanNotProcessRightNow())
  {
    ctx.RetryLater(TimeSpan.FromMinutes(5));
    return;
  }
  // five minutes later, we're here...
});
```


## Configuration
With the configuration framework `Microsoft.Extensions.Configuration`, we get the ability to structure our configuration in a nice and readable way. The `RawRabbit` configuration contains information about brokers to connect to, as well as some default behaviour on queues, exchanges and timeouts. Below is a full configuration example. ([read more about configuration here](http://whereslou.com/2014/05/23/asp-net-vnext-moving-parts-iconfiguration/))
```js
{
  "Data": {
    "RawRabbit": {
      "RequestTimeout": "00:02:00",
      "Exchange": {
        "Durable": true,
        "AutoDelete": true,
        "Type" :  "Topic"
      },
      "Queue": {
        "AutoDelete": true,
        "Durable": true,
        "Exclusive":  true
      },
      "Brokers": [
        {
          "Hostname": "localhost",
          "VirtualHost": "/",
          "Port" :  "5672",
          "UserName": "guest",
          "Password": "guest"
        },
        {
          "Hostname": "production",
          "VirtualHost": "/prod",
          "UserName": "admin",
          "Password": "admin"
        }
      ]
    }
  }
}
```
What about connection strings in `csproj` projects? There is support for that, too. The expected format for the connection string is `brokers={comma-seperated list of brokers};requestTimeout=3600`
where:

* `broker` contains a comma seperated lists of brokers (defined below)
* `requestTimeout` is the number of seconds before `RPC` request times out.

The format for a broker is `username:password@host:port/virtualHost`, for example `guest:guest@localhost:4562/`.