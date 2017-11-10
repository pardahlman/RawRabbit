# Client lifetime

The `IBusClient` interface is the central part of RawRabbit. In order for the client to work properly it requires an open connection to the broker, as well as a few channels on that connection. The connection is an example of a expensive resource that should only be created once and re-used through-out the application lifetime. In this section, the relation between client and internal resources is explained.

## Dependency Injection registration

RawRabbit can be registered in the application's dependency injection framework. In fact, there are NuGet packages for some of the most populare DI frameworks, making it a turn-key solution. Below is an example on how RawRabbit can be registered using Autofac

```csharp
var builder = new ContainerBuilder();
builder.RegisterRawRabbit(new RawRabbitOptions());
var container = builder.Build();
```

The DI framework can be used, even though a particualr framework is not officially supported. The can be done by registering the `InstanceFactory` as singleton (it holds the resources to dispose) and `IBusClient` as transient

```csharp
var builder = new ContainerBuilder();
builder
    .Register<IInstanceFactory>(c => RawRabbitFactory.CreateInstanceFactory())
    .SingleInstance();
builder
    .Register<IBusClient>(c => c.Resolve<IInstanceFactory>().Create());
```

## Singelton client

For some applications, like tools, console apps etc, it doesn't make sense to include an dependency injection framework to create an instance of the `IBusClient`. It can easiest be done by creating a singleton instance from the `RawRabbitFactory`

```csharp
var client = RawRabbitFactory.CreateSingleton(new RawRabbitOptions());
```

This client implements `IDisposable` and will dispose all resources when disposed. In order to avoid opening and closing connections several times during the application lifetime, it should be registered as a singleton if used in a DI framework.