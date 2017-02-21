# Configuration

The default `RawRabbitConfiguration.Local` options can be easily overridden through the `RawRabbitOptions` via the `RawRabbitConfiguration` object when registering your instance. 
Here is an example showing various configuration options:

```csharp
RawRabbitFactory.CreateSingleton(new RawRabbitOptions()
{
	ClientConfiguration = new RawRabbitConfiguration
	{
		Username = "raw",
		Password = "rabbit",
		VirtualHost = "/myvhost",
		Port = 5672,
		Hostnames = { "127.0.0.1" },
		RequestTimeout = TimeSpan.FromSeconds(10),
		PublishConfirmTimeout = TimeSpan.FromSeconds(10),
		PersistentDeliveryMode = true,
		TopologyRecovery = true,
		AutoCloseConnection = false,
		AutomaticRecovery = true,
		Exchange = new GeneralExchangeConfiguration
		{
			AutoDelete = false,
			Durable = true,
			Type = ExchangeType.Topic
		},
		Queue = new GeneralQueueConfiguration
		{
			AutoDelete = false,
			Durable = true,
			Exclusive = false
		},
		RecoveryInterval = TimeSpan.FromMinutes(1),
		GracefulShutdown = TimeSpan.FromMinutes(1),
		RouteWithGlobalId = true,
		Ssl = new SslOption()
	}
});
```