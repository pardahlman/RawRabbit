# Polly

The Polly enrichers provides an easy way to configure retry policies using [Polly](http://www.thepollyproject.org/). It's possible to register a default policy as well as specialized policies for different specific calls in RawRabbit.

## Register plugin

The Polly enricher can be registered as a plugin

```csharp
var policy = Policy
  .Handle<Exception>()
  .RetryAsync((exception, retryCount, pollyContext) => 
    /* retry implementation */
  );

var busClient = RawRabitFactory.CreateSingleton(new RawRabbitOptions
  {
    Plugins = p => p.UsePolly(c => c.UsePolicy(policy)
    )
  }
);
```

## Configure policies

All available policies are keyed from `PolicyKey.cs` and added to the `IPipeContext` using the `UsePolicy` extension method. Out of the box, the following policies can be registered

* `MessageAcknowledge` used when performing `basic.ack`, `basic.nack` etc of message
* `ConsumerCreate` used when creating consumer (used for consuming messages)
* `ChannelCreate` used when creating a channel to the broker
* `QueueDeclare` used when declaring a queue
* `ExchangeDeclare` used when declaring an exchange
* `QueueBind` used when binding a queue to an exchange
* `BasicPublish` used when publishing a message
* `HandlerInvokation` used when invoking a user defined message handler

Below is an example of how to register multiple policies

```csharp
var options = new RawRabbitOptions
{
  Plugins = p => p.UsePolly(c => c
    .UsePolicy(defaultPolicy)
    .UsePolicy(queueBindPolicy, PolicyKeys.QueueBind)
    .UsePolicy(queueDeclarePolicy, PolicyKeys.QueueDeclare)
    .UsePolicy(exchangeDeclarePolicy, PolicyKeys.ExchangeDeclare)
  )
};
```

Not that the client will fallback to the default policy if a specialized policy is not registered.

### Connection policies

The (only) connection to the broker is handled in the `ChannelFactory`. When registering the Polly plugin, it registers a channel factory that allows the user to get even finer control over connection policies. The `ConnectionPolicies` calss defines policies for when the client tries to `Connect` to the broker, `GetConnection` (once open) and `CreateChannel`.

## Exception types to handle

It is of course possible to handle all exceptions in the same policy by use `Handle<Exception>`. Below is a list of more specific exception that will be thrown

* `OperationInterruptedException` will be thrown when trying to declare queue or exchange that already exists with other features (auto-delete, durable, ...) than provided in the call
* `BrokerUnreachableException` will be thrown if the client can't establish a connection to the broker.

## Polly Context

RawRabbit adds a few keys to the Polly context that is available when defining retry strategies. These keys are defined in `RetryKeys.cs`. The context includes relevant data for the operation. It always includes `Retry.CancellationToken`, which is the cancellation top level cancellation token. This means that the operation can be cancelled at any time in the retry policies. That will halt the execution and throw an `OperatinCancelledException`.