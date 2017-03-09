# Subscribe

The extension methods again come to the rescue! Subscriptions again very much resembles the 1.x way of doing things with a cleaner separation of concerns.

## BasicConsume

First get the `Subscribe` operation package to enrich the BusClient with BasicConsumeAsync, used to perform a BasicConsumeAsync

```nuget

  PM> Install-Package RawRabbit.Operations.Subscribe
```

then you can

```csharp

await _rabbitBus.BasicConsumeAsync(async args =>
	{
		await
			Task.Run(() => MyTask());

		return new Ack();

	}, ctx => ctx
		.UseConsumerConfiguration(cfg => cfg
			.Consume(c => c
					.WithRoutingKey("custom_routing_key")
					.OnExchange("custom_exchange")
					.WithPrefetchCount(100)
			)
			.FromDeclaredQueue(q => q
					.WithName("custom_queue_name")
			))
);
```