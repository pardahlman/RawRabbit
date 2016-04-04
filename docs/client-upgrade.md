# Client upgrade

## 1.9.0

In release `1.9.0`, the default message routing behaviour was changed so that any published message gets its `GlobalMessageId` appended to the routing key. A message that previously was published with routingkey `foo`, will use `foo.870A9C90-CDEC-4D8D-870B-50BA121BD88F`. This is used in the [Message Sequence Extension](message-sequence.html) to route only relevant messages to the different clients. Subscribers to messages use a wildcard routing `foo.#` and the messages will be delivered to the consumer. Previously, the `Direct` exchange type was the default type in RawRabbit, but wildcard routing is not supported there, which is why the new default is `Topic`.

When a consumer is set up, RawRabbit verifies that the exchange to which it want to bind the consumer to exists. If the exchange is exists but it is declared with a different type than the one that exists, an exception will be thrown.

### Using existring configuration
The old configuration can be used by registering a "legacy" (pre 1.9.0) configuration

```csharp
var cfg = RawRabbitConfiguration.Local.AsLegacy();
var client = RawRabbitFactory.GetExtendableClient(ioc => ioc.AddSingleton(s => cfg));
```
The `AsLegacy` extension sets the configuration value `RouteWithGlobalId` to false and resets the default exchange type to `Direct`.

### Upgrading from < 1.9.0
If you want to use the new configuration on existing environments, the [Update Topology Extension](update-topology.html) can be used to re-declare and re-bind queues with minimal downtime:

```csharp
var client = RawRabbitFactory.GetExtendableClient();
await client.UpdateTopologyAsync(c => c
	.ExchangeForMessage<BasicMessage>()
	.UseConfiguration(
		exchange => exchange.WithType(ExchangeType.Topic),
		bindingKey => $"{bindingKey}.#")
	);
```

By adding the `#` wildcard, the consumer matches zero or more words in the routing key, making it compatible with clients that use the old configuration.