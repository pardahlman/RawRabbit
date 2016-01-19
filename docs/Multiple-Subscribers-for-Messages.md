# Multiple Subscribers
### Introduction
_Publishing_ and _Subscribing_ to messages involves configuration of _queues_ and _exchanges_, as well as deciding on the routing key a published message should use. If you're interested in learning more about the implications of different type of configuration, see the excellent [tutorials on RabbitMq's site](http://www.rabbitmq.com/tutorials/tutorial-one-dotnet.html).

### Default behaviour
By default, RawRabbit assumes that a when a message is published (`client.PublishAsync<BasicMessage>()`) , all _unique_ subscribers wants it delivered to them. A unique subscriber is just about any subscriber, with the exception of subscribers that have multiple instances on different servers (like a load balanced scenario where the application is installed on multiple servers).

_Why is that?_ Well, say that you have a subscriber to a message that sends a message to a email address retrieved from the message. If this application service exists on multiple servers and each of them would receive the message, then the user would get as many mails as instances of the service.

The default behaviour, which can be replaced by register a custom implementation of `INamingConvention`, is achieved by creating unique queue names that contains:
* queue name (extracted from naing convention)
* the application name (extracted from executing folder)
* a unique counter of subscriber to a message type (given the instance of the bus client). In order to make the queue names shorter, the counter is emitted for the first subscriber.

Note that the unique counter is per instance of `IBusClient`. It is therefore recommended to wire up the bus client as a singelton in the IoC container. If you use the `BusClientFactory` or register the IoC using the ServiceCollection extension `AddRawRabbit()`, this is done for you.

### Customizing
Sometimes the defaults aren't good enough for a subscriber. One applied example would be a service that hold a cache and at some point a `ClearCacheMessage` is published. When this happends, we want _all_ subscribers to clear the cache. This can be achieved by overriding the default queue prefix:

```csharp
secondSubscriber.SubscribeAsync<BasicMessage>(async (message, context) =>
{
    //do stuff...
}, cfg => cfg.WithSubscriberId("unique_id"));
```

