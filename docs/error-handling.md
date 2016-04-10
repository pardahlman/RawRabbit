# Error Handling

The error handling pipeline for `RawRabbit` is contained in the `IErrorHandlingStrategy`. It is granular in the sense that different strategies can be employed depending on messaging pattern. All methods in the `DefaultStrategy` are marked as `virtual` and can easierly be overriden in derived classes.

## Publish/Subscribe

There is no error handling in the _publish phase_, as there are only a few things that can go wrong here, and exceptions thrown here would most probably need to be resolved (like Topology missmatch).

Any unhandled exception in an subscriber results in the message being published in the default error exchange, together with the exception and other useful metadata.

### The default error exchange

The default error exchange name is resolved from the registered `INamingConventions`. By default, no queues are bound to this exchange, and the message will be dropped by the message broker.

To consume messages from the default error queue, setup a consumer for `HandlerExceptionMessage`

```csharp
client.SubscribeAsync<HandlerExceptionMessage>((message, context) =>
{
	var originalMsg = message.Message;
	var originalContext = context;
	var unhandled = message.Exception;
	return HandleAsync(originalMsg, originalContext, unhandled);
}, c => c
	.WithExchange(e => e.WithName(conventions.ErrorExchangeNamingConvention()))
	.WithQueue(q => q.WithArgument(QueueArgument.MessageTtl, 1000))
	.WithRoutingKey("#"));
```

The routing key `#` secures that all unhandled exceptions are recieved in the message handler. However, the message is published with its original routing key, so it is possible to change the routing key to `SendOrderRequest` or any other message that exists in the solution.

It is optional to use the [Queue Time To Live](https://www.rabbitmq.com/ttl.html) attribute and it might be adjusted for different queues depending on the importance of the message.

Messages can also be fetch in a more batch like behaviour by using the [Bulk Get Extension](Bulk-fetching-messages.html).

## Request/Respond

Exceptions thrown in the responder message handler is by default caught and sent back to the requester where it is re-thrown. The re-thrown exception is then again caught by `OnResponseRecievedException`, which does nothing be default. Since the request/respond pattern is synchronious. The behaviour could easerly be change to send the message to the default exchange, but remember that the caller is waiting for a task to finish, otherwise the application itself will stall.