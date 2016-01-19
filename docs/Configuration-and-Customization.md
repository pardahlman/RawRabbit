# Configuration and customization
As with most frameworks, the configuration of `RawRabbit` can be done in either code or configuration. The easiest way to configure a vNext application is by using the optional parameter in the `IServiceCollection` extension:

```csharp
private static void ConfigureApplication(IServiceCollection serviceCollection)
{
	serviceCollection
		.AddRawRabbit(
			cfg => cfg.AddJsonFile("rawrabbit.json"),
			ioc => ioc.AddTransient<ILogger, SerilogLogger>()
		);
}
```
If the application follows the pre vNext standards you can still leverage this syntax by using the `BusClientFactory` in [`RawRabbit.vNext`](https://www.nuget.org/packages/RawRabbit.vNext/) package

```csharp
BusClientFactory.CreateDefault(
	cfg => cfg.AddJsonFile("rawrabbit.json"),
	ioc => ioc.AddTransient<ILogger, SerilogLogger>()
)
```
Here's a sample of how the `rawrabbit.json` configuration file could look like
```js
{
	"RequestTimeout": "00:00:10",
	"PublishConfirmTimeout": "00:00:01",
	"RetryReconnectTimespan": "00:01:00",
	"PersistentDeliveryMode": true,
	"Brokers": [
		{
			"Hostname": "localhost",
			"Username": "guest",
			"Password": "guest",
			"VirtualHost": "/",
			"Port": "5672"
		}
	],
	"Exchange": {
		"Autodelete": false,
		"Durable": true,
		"Type": "Direct"
	},
	"Queue": {
		"Exclusive": false,
		"AutoDelete": false,
		"Durable": true
	}
}
```

### Using connection strings
If using the new configuration framework isn't suitable in your project, you could create a `connectionString` that `RawRabbit` can interpret.  The expected format for the connection string is `brokers={comma-seperated list of brokers};requestTimeout=3600`
where:

* `broker` contains a comma seperated lists of brokers (defined below)
* `requestTimeout` is the number of seconds before `RPC` request times out.

The format for a broker is `username:password@host:port/virtualHost`, for example `guest:guest@localhost:4562/`. The connection string can be parsed using the `ConnectionStringParser`