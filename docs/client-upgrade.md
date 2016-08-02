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

## 1.9.5

With `1.9.5`, the life time management has been looked over thoroughly. Previously, the base client implemented the `IDisposable` interface, that in turn disposed all of its own resoruces, all the way down the `IChannelFactory`. This is wanted behaviour in applications where the busclient is registered as a single instance with the same life time as the applications. However, in web applications, we might want to build the bus client for each request, customizing dependencies based on the `HttpContext`. Disposing everything in that scenario will lead to a unneccesary performance hit.

To address this, the `IDisposable` interface was removed from the base client, and added to derived clientes in the `Disposable` namespace. This is the client that is returned from the `BusClientFactory` (and the `RawRabbitFactory` for extendable bus clients).

### Updraging to 1.9.5

There should be no major problems with this update. If you are using the factory classes for creating bus clients and somehow misses any references in your class, make sure to use

* `RawRabbit.vNext.Disposable.IBusClient` where your previously used `RawRabbit.IBusClient`
* `RawRabbit.Extensions.Disposable.IBusClient` where your previously used `RawRabbit.IBusClient` for extensions.