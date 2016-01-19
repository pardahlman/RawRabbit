# Extending RawRabbit
The core mission for `RawRabbit` is to provide a solid foundation enabling simple Request/Reply and Publish/Subscribe operations. However, sometimes this is not enough and you'd like a way to do more specialized tasks. Below is an introduction how you can extend the `RawRabbit` client with just about anything you'd like.

### The ExtendableBusClient
The `ExtendableBusClient` is found in [`RawRabbit.Extensions`](https://www.nuget.org/packages/RawRabbit.Extensions/) NuGet pacakge. It is an super class of the normal bus client, that exposes the method `GetService<TService>` (which is just a wrapper around [`Microsoft.Extensions.DependencyInjection`](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions/)). This method allows you to resolve the registered services that `RawRabbit` uses. This way, if you for example has a custom `IContextProvider` that you need to get a hold of, it's just a call away.

Below is a small boiler plait for an extension

```csharp
public static class RawRabbitExtensionExample
{
	public static void DoStuff<TContext>(this IBusClient<TContext> client)
		where TContext : IMessageContext
	{
		var extended = (client as ExtendableBusClient<TMessageContext>);
		if (extended == null)
		{
			//TODO: nice error handling
			throw new InvalidOperationException("");
		}
		var channel = extended.GetService<IChannelFactory>().CreateChannel();
		// resolve stuff, make calls...
	}
}
```

Wondering about the generic parameter `TContext`? This has to do with the message context that you're using. It can be handly when [chaining messages](https://github.com/pardahlman/RawRabbit/wiki/Chaining-messages) or using advanced contexts like  [delayed requeue of messages](https://github.com/pardahlman/RawRabbit/wiki/Delayed-requeue-of-messages).