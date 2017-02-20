# Attribute Routing

Attribute Routing is a convenient way to specify routing key and other topology features based on attributes on the message object
The enricher is be registered as a plugin

```csharp
new RawRabbitOptions
{
    Plugins = p => p.UseAttributeRouting()
}
```

There are three attributes that configure Exchange, Queue and Routing reprectivly. There is no requirement to use all three attributes for a message.

```csharp
[Exchange(Name = "todo", Type = ExchangeType.Topic)]
[Queue(Name = "attributed_create_todo", MessageTtl = 3000, AutoDelete = false)]
[Routing(RoutingKey = "create_the_todo", NoAck = true, PrefetchCount = 50)]
public class CreateTodo
{
    public Todo Todo { get; set; }
}
```