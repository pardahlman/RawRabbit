# Queue Name Suffix

By default, RawRabbit declares [topic exchanges](https://www.rabbitmq.com/tutorials/tutorial-five-dotnet.html), that allows messages to be routed to all queues with a matching routing key. Only be one copy of the message will be delivered to each queue. This means that if different applications subscribe to the same message _on the same queue_, the message will only be delivered to one of the applications in a round robin fashion.

The _Queue Name Suffix Enricher_ can be used to add suffixes to queue names, an thereby distinguishable from other queues. There are different suffixes that can be used to achieve desired delivery patterns.

## Register Plugin

```csharp
var options = new RawRabbitOptions
{
    Plugins = ioc => ioc
        .UseApplicationQueueSuffix()
        .UseCustomQueueSuffix("custom")
        .UseHostQueueSuffix()
};
```
## Types of queue suffixes

The _Application Queue Suffix Plugin_ append the application name, derived from executable or command line arguments.

The _Host Queue Suffix Plugin_ appends the machine name from `System.Environment.MachineName`.

The _Custom Queue Suffix Plugin_ appends the provided string to the queue name. It can be overriden when declaring a subscribe by using the optional `Action<IPipeContext>`

```csharp
await secondSubscriber.SubscribeAsync<BasicMessage>(async msg =>
{
  // code here
}, ctx => ctx.UseCustomQueueSuffix("special"));
```

## Scenarios

### Act and monitor e-commerce

A customer has added an item to the cart, and the message `ItemAdded` is published. There are two services that subscribe to the message, one service that is responsible to calculate the new total price that will be updated in the user interface and another serivce that monitors the data and push it to a BI integration.

It is important that the message is delivered to both services. This can be achieved by appending the _Application Queue Suffix_.

### Send email

The customer has placed an order and the message `OrderCaptured` is published. A service that is responsible for sending an order acknowledgement to the customer subscribes to the message. There are multiple instances of the service for redundancy reasons. It makes sense that the different instances of the service uses the same queue, so that the customer only gets one email.

## Clearing caches

New product data is available, and the system is being informed of this by the message `ProductDataAvailable`. Services are distributed one instance per server in a clustered environment. It is important that all instances of all services gets this message, so that they clear their caches. This can done by using the _Host Queue Suffix_, adding the server name as a suffix, and hence creating a unique queue.