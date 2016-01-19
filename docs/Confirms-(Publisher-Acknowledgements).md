RabbitMq has support for [_Confirms/Publisher Acknowledgements_](https://www.rabbitmq.com/confirms.html), meaning that a publisher gets a `basic-ack` when the message has been accepted by all queues (or the broker verified that the message is unroutable). `RawRabbit` uses this feature when performing `PublishAsync<TMessage>` calls. The `Task` returned from publish call is completed once the broker has confirmed the published message.

If the message hasn't been confirmed within a specified amount of time, the task will fault with a `PublishConfirmException`. To change the timeout, change the `PublishConfirmTimeout` property on the configuration object.

```csharp
var config = new RawRabbitConfiguration
{
	PublishConfirmTimeout = TimeSpan.FromMilliseconds(500)
};
var publisher = BusClientFactory.CreateDefault(config);
```

There is a _slight_ performance hit using using this feature. If you want to disable it, just register the `NoAckAcknowledger` when instantiating the bus client.

```csharp
var publisher = BusClientFactory.CreateDefault(s =>
	s.AddSingleton<IPublishAcknowledger, NoAckAcknowledger>()
);
```