# Attribute based configuration
`RawRabbit` has support for attribute based configuration in the NuGet package [`RawRabbit.Attributes`](https://www.nuget.org/packages/RawRabbit.Attributes/).

## Setting up the client

In order to get the client to scan messages for attributes, register `AttributeConfigEvaluator` as the `IConfigurationEvaluator`

```csharp
var client = BusClientFactory.CreateDefault(ioc => ioc
	.AddSingleton<IConfigurationEvaluator, AttributeConfigEvaluator>()
);
```
## Configure Messages
There are different attributes that configure different configuration aspects: `QueueAttribute`, `ExchangeAttribute` and `RoutingAttribute`. Note that for the Request/Respond pattern only the attributes of the request message type is scanned.

```csharp
[Queue(Name = "my_queue", MessageTtl = 300, DeadLeterExchange = "dlx", Durable = false)]
[Exchange(Name = "my_topic", Type = ExchangeType.Topic)]
[Routing(RoutingKey = "my_key", NoAck = true, PrefetchCount = 50)]
private class AttributedMessage
{
	public string Property { get; set; }
}
```

## Override with custom configuration
The `AttributeConfigEvaluator` looks for configuration attributes and fallback to the default `ConfigurationEvaluator`. It also honors the custom configuration provided in the optional configuraiton argument.

```csharp
client.SubscribeAsync<AttributedMessage>((message, context) =>
{
	tcs.TrySetResult(message);
	return Task.FromResult(true);
}, c => c.WithRoutingKey("overridden"));
```