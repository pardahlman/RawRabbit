# Message Priority
## Priority for specific messages

To be able to leverage the [Priority Queue feature](https://www.rabbitmq.com/priority.html) in RabbitMq, you first have to indicate that the queue to which you are subscribing to has the `x-max-priority` argument. This can be done by using the optional configuration argument on the `SubscribeAsync` method

```csharp
subscriber.SubscribeAsync<BasicMessage>(async (message, context) =>
{
	// do stuff
}, cfg => cfg
	.WithQueue(q => q.WithArgument(QueueArgument.MaxPriority, 3))
	.WithPrefetchCount(1)
);
```
In this example, the prefetch count is sets to one, since the already prefetched messages would be processed before a not prefetched message with higher priority.

Now that you have a queue that honours the priority property, you can send messages to it with priority set. This is also done with the fluent configuration builder. In fact, with the builder you get access to all `BasicProperties` for a message.

```csharp
publisher.PublishAsync(new BasicMessage
{
	Prop = "I am important"
}, configuration: cfg =>
	cfg.WithProperties(p => p.Priority = 9)
);
```

## Setting priority based on message type
Sometime you want more of a policy like approach, like "All messages of type X is important". This can be achieved by implementing a custom `IBasicPropertiesProvider`. In the method `GetProperties<TMessage>(Action<IBasicProperties> custom )` you have access to the message type and it returns the properties that will be set in all outgoing messages.