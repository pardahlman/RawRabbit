# Chaining Messages
## Introduction to MessageContext
All message handlers in `RawRabbit` have two arguments. The first argument is the message, easy-piecy. The second argument is the _message context_. A message context is a class that implements `IMessageContext`. The only required property of a message context is the `GlobalRequestId`. This entire context can be forwarded from within a message handler if the requests are part of the same logical unit. This way contextual metadata about the request can reach underlying services. To forward a message, just pass it as an optional parameter the `PublishAsync` or `RequestAsync` cals

```csharp
firstResponder.RespondAsync<FirstRequest, FirstResponse>(async (req, c) =>
{
   //forward global id here.
   await firstResponder.PublishAsync(new BasicMessage(), c.GlobalRequestId);
   return new FirstResponse();
});
```

When is this useful? Authorization is a perfect example; a user requests data from a ui. The user is authenticated and has a set of claims that allows the user to do access some (but not all!) data. The request arrives at the backend of the ui. The endpoint knows what claims the user has, but the data is fetched from multiple underlying services communicate with over RabbitMq. Things like authentication and authorization doesn't have anything to do with the _request_ itself, but it is something that the services needs to know of for filtering data. In this scenario, a custom context can be useful.

## Use a custom message context
_Message contexts_ are provided to the message handler through the registered `IMessageContextProvider`. This class is responsible for creating/retrieving a message context, as well as serialize it for sending. If you're not planing to do anything to fancy, the default `MessageContextProvider<TContext>` will probably be enough (it is initialized with a `Func<TContext>` that you can specify in your IoC wire-up). For more information about register and resolving bus clients, see the Register DependecyInjection section or the [integration tests for Nack](https://github.com/pardahlman/RawRabbit/blob/master/src/RawRabbit.IntegrationTests/Features/NackingTests.cs).

## Advanced message context
`RawRabbit` has support for some advanced concepts, like `Nack` and `RetryLater`. An assumption is that in many cases, the message handler that recieves a message can always handle it, and does not need to retry/nack. However, there are times when you need to use this. Therefore, `RawRabbit` is shipped with an `AdvancedMessageContext` that lets the handler indicate these things..

```csharp
subscriber.SubscribeAsync<BasicMessage>(async (message, context) =>
{
	context?.Nack();
});
```

Note that there is *nothing magical* with the `AdvancedMessageContext`. The `Nack` method is wired up in the [`IContextEnhancer`](https://github.com/pardahlman/RawRabbit/tree/master/src/RawRabbit/Context/Enhancer). By registering you own context, context provider and context enhancer, you can do just about anything in the message handler.