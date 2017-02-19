# Global Execution Id

The Global Message Id is a unique id that is created when a message first is published, and forwarded for messages published within the subscriber of the received message. It can be very helpful to follow execution flows over multiple applications.

## Registration

The Global Message Id is registered as a plugin in the RawRabbitOptions

```csharp
new RawRabbitOptions
{
    Plugins = p => p.UseGlobalExecutionId()
});
```

## Route with Global Message Id

The plugin updates all outgoing message’s routing key with the global message id, like `routingkey.global-message-id`. This way, a consumer can subscribe to `#.global-message-id` to get all messages corresponding to that execution.

To match this, all subscription routing keys are suffixed with `#` (zero or more words), making sure that the publish message is recived.

A client can subscribe to messages published with the global message id suffix, even though it does not has the plugin registered. This is achieved by using the fluent configuration builder and override the routing key.

## Header property

The property is added to the `IPipeContext` and passed in the header of the BasicProperties as `global_execution_id`. The plugin is executed in the `ProducerInitialized` state, making it available throughout the execution pipe. It can, for example, be used as a property in the message context

```csharp
new RawRabbitOptions
{
    Plugins = p => p
        .UseGlobalExecutionId()
        .UseMessageContext(ctx => new CustomContext
        {
            ExecutionId = ctx.GetGlobalExecutionId()
        })
});
```