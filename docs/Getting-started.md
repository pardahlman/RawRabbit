# Getting Started

Getting started to work with `RawRabbit` is easy. Download the [latest version on NuGet](https://www.nuget.org/packages/RawRabbit/) and instantiate a new client with the factory class `BusClientFactory`.

```csharp
var raw = BusClientFactory.CreateDefault();
```

Next up, it's time to wire up message handlers and publishers. We're going to look at [remote procedure calls](https://www.rabbitmq.com/tutorials/tutorial-six-dotnet.html) and [publish/subscribe](https://www.rabbitmq.com/tutorials/tutorial-three-dotnet.html). If you're not familiar with these concept, head over to RabbitMq's homepage and check out there great tutorial. We'll still be here when you're back.

## Request/Response (RPC)

It takes two to tango; in order to get the _remote procedure calls_ going, we need to set up a bus client that knows how to _respond_ to a (request) message, and another client that can _produce_ a (request) message. A message in the world of `RawRabbit` is a [POCO class](https://en.wikipedia.org/wiki/Plain_Old_CLR_Object).

```csharp
var requester = BusClientFactory.CreateDefault();
var responder = BusClientFactory.CreateDefault();

responder.RespondAsync<BasicRequest, BasicResponse>(async (request, ctx) =>
{
	//do some stuff...
	return new BasicResponse();
});
var recieved = await requester.RequestAsync<BasicRequest, BasicResponse>();
```

A few things happened here. First off, we created two bus clients. Since we haven't specified any configuration, it defaults to connecting to `localhost:5672` with the user name and password `guest`/`guest` ([the default user](https://www.rabbitmq.com/access-control.html)). Next up, we wired up the event handler for the responder; saying that it knows how to respond to `BasicRequest` with `BasicReponse`.

All message handlers in `RawRabbit` has two arguments. The first argument is the actual message (or request in this case). The second argument is the _message context_. We'll get back to that one later. A responder responds to a request by returning a response. The overhead of RPC calls are almost neglectable. On an average computer, 10'000 RPC calls are executed in approximately 2 seconds (that is 0.2 millisecond per call).

## Publish/Subscribe
Using a publish/subscribe pattern is a great message driven, asynchronous programming. Programmatically, it does not differ much from the _remote procedure call_ above.
```csharp
var publisher = BusClientFactory.CreateDefault();
var subscriber = BusClientFactory.CreateDefault();
			
subscriber.SubscribeAsync<BasicMessage>((async msg, ctx) =>
{
	Console.WriteLine(msg.Prop); // Hello, world!
});
publisher.PublishAsync(new BasicMessage {Prop = "Hello, world!"});
```
There is no return statement in the subscriber's message handler (or there would be if the method wasn't marked with `async`). If the properties in the message isn't important you could send a message just by typing `publisher.PublishAsync<BasicMessage>()`.