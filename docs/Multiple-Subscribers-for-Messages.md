# Multiple Subscribers

By default, RawRabbit assumes that when a message is published (`PublishAsync<TMessage>()`), all _unique_, subscribers (with matching routing key on corresponding exchange) wants it. All subscribers are considered to be unique, except those who are hosted in applications that have multiple instances connected to the broker. This can happend if applications are deployed to multiple servers and connected to the same RabbitMq host (or clustered hosts).

The reason for this behaviour  is that in many cases it is unwanted to perform an operation multiple times.

## Default behaviour

### Example: Confirmation email
A service subscribes to a message `OrderSent`, the service sends an email to the customer. Even if this service has multiple insanse connected to the same broker, only one email should be sent.

The default behaviour is achieved by creating unique queue names that contains:
* queue name (extracted from naing convention)
* the application name (extracted from executing folder)
* a unique counter of subscriber to a message type (given the instance of the bus client). In order to make the queue names shorter, the counter is emitted for the first subscriber.

Note that the unique counter is per instance of `IBusClient`. It is therefore recommended to wire up the bus client as a singelton in the IoC container. If you use the `BusClientFactory` or register the IoC using the ServiceCollection extension `AddRawRabbit()`, this is done for you.

## Custom Behaviour
For some scenarios, the default behaviour is not desired. It can be modified on for each subscriber by setting a _subscription id_, or for the entire client by registering a custom `INamingConvention`. 

### Example: Clear Cache
A service subscribes to a `NewDataAvailable`, the service should clear its cache when recieving this message. If the service has multiple instance connected to the broker, each instanse should recieve the message and clear the cache.

### Specifying Subscriber Id
The solution is to specify a unique subscription id for the service.
```csharp
secondSubscriber.SubscribeAsync<BasicMessage>(async (message, context) =>
{
    //do stuff...
}, cfg => cfg.WithSubscriberId("unique_id"));
```

