# Registering the Client

Depending on the scenario, there are a few different ways to register the client.

### Register from factory

The easiest way to register a new client is by calling `RawRabbitFactory.CreateSingleton`. 
If no arguments are provided, the local configuration as defined in `RawRabbitConfiguration.Local` will be used (`guest` user on `localhost:5672` with virtual host `/`).

```csharp
var raw = RawRabbitFactory.CreateSingleton();
```

### vNext Application Registration
If the application is bootstrapped from a `vNext` application, the dependencies and client can be registered by using the `AddRawRabbit` extension for `IServiceCollection`
The package [`RawRabbit.vNext`](https://www.nuget.org/packages/RawRabbit.vNext) contains modules and extension methods for registering `RawRabbit`.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddRawRabbit(); //optional overrides here, too.
}
```

### Autofac Registration
The package [`RawRabbit.DependencyInjection.Autofac`](https://www.nuget.org/packages/RawRabbit.DependencyInjection.Autofac) contains modules and extension methods for registering `RawRabbit`.

```csharp
var builder = new ContainerBuilder();
builder.RegisterRawRabbit();
var container = builder.Build();
``` 

### Ninject Registration
The package [`RawRabbit.DependencyInjection.Ninject`](https://www.nuget.org/packages/RawRabbit.DependencyInjection.Ninject) contains modules and extension methods for registering `RawRabbit`.

```csharp
var kernel = new StandardKernel();
kernel.RegisterRawRabbit();