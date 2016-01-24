# Message Context
## Introduction
Messages that are sent through `RawRabbit` are delivered with a _message context_. Any class that implements `IMessageContext` can be used as a message context. This means that it is possible to replace the default context with a domain specific context. The goal is to seperate the message and its metadata/context.

## Forwarding Context
Message context can be forwarded to subsequent message handlers. This is useful when a consumer communicates with other services that needs the message context to process the message correctly.

```csharp
firstResponder.RespondAsync<FirstRequest, FirstResponse>((req, c) =>
{
   firstResponder
     .PublishAsync(new BasicMessage(), c.GlobalRequestId) //forward context.
     .ContinueWith(t => new FirstReponse());
});
```

Another useful aspect of forwarding message contexts is that the global request id can be traced though the different systems, making it easy to folllow a request from its originator to all systems that handle the message.

### Example: User authorization
A user requests data from a UI. The user is authenticated and has a set of claims that allows the user to do access some _(but not all)_ data. The request arrives at the backend of the UI. The endpoint knows what claims the user has, but the data is fetched from multiple underlying services communicate with over RabbitMq. Things like authentication and authorization doesn't have anything to do with the _request_ itself, but it is something that the services needs to know of for filtering data. The message context for this setup should contain a list of the users claims, so that the service can evaluate if the requested action is authorized.

## Default Context
The default message context, `MessageContext`, has only one member; `GlobalRequestId`.

## Advanced Context
The `AdvancedMessageContext` contains properties that can be used to [requeue message with delay](delayed-requeue-of-messages.html) and send negative acknowledgements. Note that there is *nothing magical* with the `AdvancedMessageContext`. It is just a [custom context]("#custom-context").

### Instansiate bus with advanced context
The easiest way to create an instance of a `RawRabbit` client that uses an advanced context is to use the generic `CreateDefault<TMessageContext>` method on `BusClientFactory` (from `RawRabbit.vNext`).
```csharp
var client = BusClientFactory.CreateDefault<AdvancedMessageContext>();
```
## Custom Context

### The Message Context
There are only two requirements for a message context class. It needs to implement `IMessageContext` and it needs to be serializable/deserializable by the registered `IMessageContextProvider<TMessageContext>` (by default `Newtonsoft.Json`).

```csharp
public class CustomContext : IMessageContext
{
  public string CustomProperty { get; set; }
  public ulong DeliveryTag {get; set;}
  public Guid GlobalRequestId { get; set; }
}
```
### The Context Provider
Message contexts are provided to the messages by the registered `IMessageContextProvider`. The default implementation, `MessageContextProvider<TMessageContext>` can be used for most context (typically `POCO` classes).

### The Context Enhancer
A recieved message passes through the registered `IContextEnhancer` before any message handler is invoked. The method `WireUpContextFeatures` is called with the current context, consumer and `BasicDeliverEventArgs` (from `RabbitMQ.Client`).
```csharp
public class CustomContextEnhancer : IContextEnhancer
{
  public void WireUpContextFeatures<TMessageContext>(TMessageContext context, IRawConsumer consumer, BasicDeliverEventArgs args)
    where TMessageContext : IMessageContext
  {
    var customContext = context as CustomContext;
    if (customContext == null)
    {
      return;
    }
    customContext.DeliveryTag = args.DeliveryTag;
  }
}
```

### The RawRabbit Client
The easist way to create a client is by using the generic `CreateDefault<TMessageContext>` method on `BusClientFactory`.

```csharp
var client = BusClientFactory.CreateDefault<AdvancedMessageContext>();
```
The client can also be resolved from the service collection.
```csharp
var service = new ServiceCollection()
  .AddRawRabbit<CustomContext>()
  .BuildServiceProvider();
var client = service.GetService<IBusClient<CustomContext>>();
```
