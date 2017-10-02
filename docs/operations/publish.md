# Publish

It just isn’t feasible to have a multi-line, complex expression just to perform a simple publish (or any other operation for that matter). 
This is where extension methods come to the rescue. It turns out that it is dead simple to create a publish signature that very much resembles the 1.x way of doing things.

First get the `Publish` operation package to enrich the BusClient with Publishing capabilities

```nuget

  PM> Install-Package RawRabbit.Operations.Publish
```

then you can

```csharp

var message = new BasicMessage { Prop = "Hello, world!" };

await publisher.PublishAsync(message, ctx => ctx
	.UsePublisherConfiguration(cfg => cfg
		.OnExchange("custom_exchange")
		.WithRoutingKey("custom_key")
));
```