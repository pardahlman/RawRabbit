# Update Topology

Topology features such as queues and exchanges cannot be updated in RabbitMq. However, sometimes it can be desired to change type, durability or other configuration aspects. This can be done with the `UpdateTopology` extension. It removes topology features and re-declares them based on configuration. The extension is available through [`RawRabbit.Extensions`](https://www.nuget.org/packages/RawRabbit.Extensions/) that can be installed via the NuGet console

```nuget

  PM> Install-Package RawRabbit.Extensions

```

## Exchange updates

Updating an exchanges requires two things, the name of the exchange to update and the new desired configuration. Changing the type and durability of exchange `my_exchange` can be done with a few lines of code.

```csharp
await client.UpdateTopologyAsync(t => t
	.ForExchange("my_exchange")
	.UseConfiguration(e => e
		.WithType(ExchangeType.Topic)
		.WithDurability(false))
);
```

The name of the exchange can also be extracted by the message type and the registered `INamingConvention`

```csharp
await client.UpdateTopologyAsync(c => c
	.ExchangeForMessage<BasicMessage>()
	.UseConfiguration(e => e.WithType(ExchangeType.Topic)));
```

Values that are not provided in the configuration builder will default to the values of the `GeneralExchangeConfiguration` on the registered `RawRabbitConfiguration`. If the general exchange configuration has changed and a solution wide update is desired, the `UseConventionForExchange<TMessage>` method can be used

```csharp
var result = await client.UpdateTopologyAsync(c => c
	.UseConventionForExchange<FirstMessage>()
);
```

### Change multiple exchanges
The different signatures can be combined in a number of ways to update exchanges. If multiple update configurations are defined for the same exchange, only the latest one will be used.

```csharp
await client.UpdateTopologyAsync(c => c
	.ForExchange("my_exchange")
	.UseConfiguration(x => x.WithAutoDelete())
	.ExchangeForMessage<BasicMessage>()
	.UseConfiguration(x => x.WithType(ExchangeType.Direct))
	.ExchangeForMessage<SimpleMessage>()
	.UseConventions<BasicMessage>()
	.UseConventionForExchange<FirstMessage>()
	.UseConventionForExchange<SecondMessage>()
	.UseConventionForExchange<ThirdMessage>()
);
```

### Downtime
Updating an exchange consists of three steps

1. Deleting exchange
2. Re-declare exchange
3. Re-add existing queue bindings

It is not until all queue bindings have been re-added to an exchange that everything works as expected. The extension method returns an result object that contains information about what bindings that has been re-added and the execution time.

```csharp
var result = await client.UpdateTopologyAsync(t => t
	.ForExchange(exchangeName)
	.UseConfiguration(e => e
		.WithType(ExchangeType.Topic)
		.WithDurability(false))
);

ExchangeConfiguration exchangeConfig = result.Exchanges[0].Exchange;
TimeSpan executionTime = result.Exchanges[0].ExecutionTime;
List<Binding> bindings = result.Exchanges[0].Bindings;
```

### Binding Key Transformer
In addition to be able to re-define features of the exchange, the binding key can be updated with the optional argument `bindingKeyTransformer`. This can be useful when adding or removing wildcard routing while changing exchange type from one that supports wildcard and one that does not.

```csharp
await currentClient.UpdateTopologyAsync(c => c
	.ExchangeForMessage<BasicMessage>()
	.UseConfiguration(
		exchange => exchange.WithType(ExchangeType.Direct),
		bindingKey => bindingKey.Replace(".*", string.Empty))
);
```

## Queue updates
There are currently no support for updating queues.