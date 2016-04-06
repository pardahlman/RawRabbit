# Logging

`RawRabbit` comes with a console logger, which makes sense when playing around in a console app. However, you probably want to use the same logger as you use in the rest of the project. This can be done by downloading `RawRabbit.Logger.Serilog`, `RawRabbit.Logger.NLog`, `RawRabbit.Logger.Log4Net` or implement your own custom logger. Create a logger is fairly easy, it is a matter of implementing `ILogger` and `ILoggerFactory`.

The logger is provided to RawRabbit though the registered `ILoggerFactory`, so it is enough to register the desired factory to use it in all internal classes

```csharp
RawRabbitFactory.GetDefaultBusClient(
			ioc => ioc.AddSingleton<ILoggerFactory, RawRabbit.Logging.Serilog.LoggerFactory>()
);
```

Similarly for vNext apps

```csharp
collection.AddRawRabbit(
	custom: ioc => ioc.AddSingleton<ILoggerFactory, RawRabbit.Logging.Serilog.LoggerFactory>()
)
```