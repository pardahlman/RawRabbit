# Publisher Acknowledgements

Publish Acknowledgements (sometimes called _Publish Confirms_) are callbacks from the message broker, verifying that a sent message has been received and handled. When activated, RawRabbit will not complete the corresponding task until the ack is recived. If this has not occured within a given time span (`PublishConfirmTimeout`), the task is cancelled with an `PublishConfirmException` enclosed.

## Override timeout configuration

The time out is one second by default, but can be changed by specifying a different value in the provided `RawRabbitConfiguration`.

```csharp
var client = RawRabbitFactory.CreateSingleton(new RawRabbitOptions
{
    Configuration = new RawRabbitConfiguration
    {
        PublishConfirmTimeout = TimeSpan.FromSeconds(2)
    }
});
```

This timeout can be overriden on a call basis

```csharp
await publisher.PublishAsync(message, ctx => ctx
    .UsePublishAcknowledge(TimeSpan.FromSeconds(3))
);
```

## Disable feature

Publish confirm check will be performend if the `PublishConfirmTimeout` is set to `TimeSpan.Max`. To disable the  featulre, simply update the value of `RawRabbitConfiguration` accordingly.

To disable publish acknowledge for a specific call, simple add appropriate value to the pipe context.

```csharp
await publisher.PublishAsync(message, ctx => ctx
    .UsePublishAcknowledge(false)
);
```

Note that that _Publish Acknowledgements_ is not the same as acknowledging a received message.
