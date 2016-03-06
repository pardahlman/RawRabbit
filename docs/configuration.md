# Configuration
As with most frameworks, the configuration of `RawRabbit` can be specified in either code or configuration. The easiest way to configure a vNext application is by using the optional parameter in the `IServiceCollection` extension:

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
### Configuration options
#### Connecting to the broker
_Username_, _password_, _virtual host_, _port_ and _hosts_ are used for connecting to the host. Hosts is a list of strings that is passed to the registered `IConnectionFactory` when establishing a connection. It uses the default host selection strategy for `RabbitMQ.Client`, which is `RandomHostnameSelector` (as of `3.6.0`).
#### Recovery From Network Failures
`RawRabbit` supports automatic recovery of connection and topology. `AutomaticRecovery` (`bool`) indicates if recovery of connections, channels and QoS should be performed. If the recovery fails it, `RawRabbit` will wait for `RecoveryInterval` (`TimeSpan`) until retrying again.  `AutomaticRecovery` (`bool`) includes recovery of exchanges, queues, bindings and consumers. More information about automatic recovering, see [RabbitMq's .NET API guide](https://www.rabbitmq.com/dotnet-api-guide.html) (under section _Automatic Recovery From Network Failures_)
#### Operation timeouts
For request/reply, the `RequestTimeout` (`TimeSpan`) specifies the amout of time to wait for a response to arrive. `PublishConfirmTimeout` specifies the time to wait for a [publish confirm](https://www.rabbitmq.com/confirms.html) from the broker.

#### Default topology settings
The default configuration for topology features (such as queue name, exchange type, auto delete) are specified in the `Exchange` (`GeneralExchangeConfiguration`) and `Queue` (`GeneralQueueConfiguration`) properties. These values can be overriden by custom configuration when specifying an operation.

#### Other
When `AutoCloseConnection` (`bool`) is set to `true`, a connection will be closed when the last channel has disconnected. Read more about this at [RabbitMq's .NET API guide](https://www.rabbitmq.com/dotnet-api-guide.html) (under section _Disconnecting from RabbitMQ_).

`PersistentDeliveryMode` (`bool`) specifies if messages should be persisted to disk. While it affects performance, it makes the system more stabile for crashes/restart. Read more about it at [RabbitMq's AMQP concept](https://www.rabbitmq.com/tutorials/amqp-concepts.html) (under section _Message Attributes and Payload_)

## vNext configuration file
Here's a sample of how the `rawrabbit.json` configuration file could look like
```js
{
	"Username": "guest",
	"Password": "guest",
	"VirtualHost": "/",
	"Port": 5672,
	"Hostnames": [ "localhost" ],
	"RequestTimeout": "00:00:10",
	"PublishConfirmTimeout": "00:00:01",
	"RecoveryInterval": "00:00:10",
	"PersistentDeliveryMode": true,
	"AutoCloseConnection": true,
	"AutomaticRecovery": true,
	"TopologyRecovery": true,
	"Exchange": {
		"Durable": true,
		"AutoDelete": true,
		"Type": "Topic"
	},
	"Queue": {
		"AutoDelete": true,
		"Durable": true,
		"Exclusive": true
	}
}
```

## ConnectionString
`RawRabbit` also supports configuration from connection strings. The syntax is `username:password@host:port/vhost(?parameter=value)`. Where

* **username** is the username used for authentication to the broker (`string`)
* **password** is the password used for authentication to the broker (`string`)
* **host** is a comma seperated lists of brokers to connect to (`string`)
* **port** is the port used when connect to a broker (`int`)
* **vhost** is the virtual host to use on the broker (`string`)
* **parameters** is a query string like seperated list of parameters (`string`). Supported parameters are the properties in the `RawRabbitConfiguration` object, such as  `requestTimeout`, `persistentDeliveryMode` etc.

The `ConnectionStringParser` can be used to create a configuration object
```csharp
var connectionString = ConfigurationManager.ConnectionStrings["RabbitMq"];
var config = ConnectionStringParser.Parse(connectionString.ConnectionString);
var client = BusClientFactory.CreateDefault(config);
```

### Localhost
```xml
<connectionStrings>
	<add name="RawRabbit" connectionString="guest:guest@localhost:5672/?requestTimeout=10"/>
</connectionStrings>
```
### Multiple hosts
Multiple hosts can specified by using a comma-seperated list.
```xml
<connectionStrings>
	<add name="RawRabbit" connectionString="admin:admin@host1.production,host2.production:5672/"/>
</connectionStrings>
```