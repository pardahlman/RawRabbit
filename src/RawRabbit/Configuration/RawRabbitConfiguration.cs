using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using ExchangeType = RawRabbit.Configuration.Exchange.ExchangeType;

namespace RawRabbit.Configuration
{
	public class RawRabbitConfiguration
	{
		/// <summary>
		/// The amount of time to wait for response to request. Defaults to 10 seconds.
		/// </summary>
		public TimeSpan RequestTimeout { get; set; }

		/// <summary>
		/// The amount of time to wait for a publish to be confirmed. Default to 1 second.
		/// Read more at: https://www.rabbitmq.com/confirms.html
		/// </summary>
		public TimeSpan PublishConfirmTimeout { get; set; }

		/// <summary>
		/// The amount of time to wait for message handlers to process message before
		/// shutting down.
		/// </summary>
		public TimeSpan GracefulShutdown { get; set; }

		/// <summary>
		/// Appends the message's global id to the publish routing key and wild cards (#)
		/// to subscribers routing key.
		/// </summary>
		public bool RouteWithGlobalId { get; set; }

		/// <summary>
		/// Indicates if automatic recovery (reconnect, re-open channels, restore QoS) should be enabled
		/// Defaults to true.
		/// </summary>
		public bool AutomaticRecovery { get; set; }

		/// <summary>
		/// Indicates if topology recovery (re-declare queues/exchanges, recover bindings and consumers) should be enabled
		/// Defaults to true
		/// </summary>
		public bool TopologyRecovery { get; set; }

		/// <summary>
		/// The default values for exchnages. Can be overriden using the fluent configuration
		/// builder that is available as an optional argument for all operations
		/// </summary>
		public GeneralExchangeConfiguration Exchange { get; set; }

		/// <summary>
		/// The default values for queues. Can be overriden using the fluent configuration
		/// builder that is available as an optional argument for all operations
		/// </summary>
		public GeneralQueueConfiguration Queue { get; set; }

		/// <summary>
		/// Indicates if messages should be stored on disk or held in memory.
		/// Set this to false if performance is more important than delivery of messages.
		/// </summary>
		public bool PersistentDeliveryMode { get; set; }

		/// <summary>
		/// Indicates if a connection should be closed when the last channel disconnects
		/// from the connection. Read more: https://www.rabbitmq.com/dotnet-api-guide.html
		/// </summary>
		public bool AutoCloseConnection { get; set; }

		/// <summary>
		/// Used for configure Ssl connection to the broker(s).
		/// </summary>
		public SslOption Ssl { get; set; }

		public string VirtualHost { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
		public int Port { get; set; }
		public List<string> Hostnames { get; set; }
		// Backwards Compatible, Customize Connection Label
		public string ClientProvidedName { get; set; } = null;
		public TimeSpan RecoveryInterval { get; set; }

		public RawRabbitConfiguration()
		{
			RequestTimeout = TimeSpan.FromSeconds(10);
			PublishConfirmTimeout = TimeSpan.FromSeconds(1);
			PersistentDeliveryMode = true;
			AutoCloseConnection = true;
			AutomaticRecovery = true;
			TopologyRecovery = true;
			RouteWithGlobalId = true;
			RecoveryInterval = TimeSpan.FromSeconds(10);
			GracefulShutdown = TimeSpan.FromSeconds(10);
			Ssl = new SslOption { Enabled = false };
			Hostnames = new List<string>();
			Exchange = new GeneralExchangeConfiguration
			{
				AutoDelete = false,
				Durable = true,
				Type = ExchangeType.Topic
			};
			Queue = new GeneralQueueConfiguration
			{
				Exclusive = false,
				AutoDelete = false,
				Durable = true
			};
		}

		public static RawRabbitConfiguration Local => new RawRabbitConfiguration
		{
			VirtualHost = "/",
			Username = "guest",
			Password = "guest",
			Port = 5672,
			Hostnames = new List<string> { "localhost" }
		};
	}

	public class GeneralQueueConfiguration
	{
		/// <summary>
		/// <para>
		/// If set, the queue is deleted when all consumers have finished using it. The last consumer can be cancelled<br/>
		/// either explicitly or because its channel is closed. If there was no consumer ever on the queue, it won't be<br/>
		/// deleted. Applications can explicitly delete auto-delete queues using the Delete method as normal.
		/// </para>
		/// </summary>
		public bool AutoDelete { get; set; }

		/// <summary>
		/// <para>
		/// Durable queues are persisted to disk and thus survive broker restarts. Queues that are not durable are called transient.<br/>
		/// Not all scenarios and use cases mandate queues to be durable.
		/// </para>
		///<para>
		/// Durability of a queue does not make messages that are routed to that queue durable.If broker is taken down and then brought<br/>
		/// back up, durable queue will be re-declared during broker startup, however, only persistent messages will be recovered.
		/// </para>
		/// </summary>
		public bool Durable { get; set; }

		/// <summary>
		/// Exclusive queues are used by only one connection and the queue will be deleted when that connection closes.
		/// </summary>
		public bool Exclusive { get; set; }
	}

	public class GeneralExchangeConfiguration
	{
		/// <summary>
		/// Exchanges can be durable or transient. Durable exchanges survive broker restart whereas transient <br />
		/// exchanges do not (they have to be redeclared when broker comes back online). Not all scenarios <br />
		/// and use cases require exchanges to be durable.
		/// <a href="https://www.rabbitmq.com/tutorials/amqp-concepts.html">https://www.rabbitmq.com/tutorials/amqp-concepts.html</a>
		/// </summary>
		public bool Durable { get; set; }

		/// <summary>
		/// If set, the exchange is deleted when all queues have finished using it.
		/// <a href="https://www.rabbitmq.com/amqp-0-9-1-reference.html">https://www.rabbitmq.com/amqp-0-9-1-reference.html</a>
		/// </summary>
		public bool AutoDelete { get; set; }

		/// <summary>
		/// There are four different types of exchanges see <see cref="RawRabbit.Configuration.Exchange"/> for more info.
		/// </summary>
		public ExchangeType Type { get; set; }
	}

	public static class RawRabbitConfigurationExtensions
	{
		/// <summary>
		/// Changes the configuration so that it does not use PersistentDeliveryMode. Also,
		/// it sets the exchange type to Direct to increase performance, however that
		/// disables the ability to use the MessageSequence extension.
		/// </summary>
		/// <param name="config">The RawRabbit configuration object</param>
		/// <returns></returns>
		public static RawRabbitConfiguration AsHighPerformance(this RawRabbitConfiguration config)
		{
			config.PersistentDeliveryMode = false;
			config.RouteWithGlobalId = false;
			config.Exchange.Type = ExchangeType.Direct;
			return config;
		}

		/// <summary>
		/// Disables RouteWithGlobalId to keep routing keys intact with older versions of
		/// RawRabbit and sets exchange type to Direct.
		/// </summary>
		/// <param name="config"></param>
		/// <returns></returns>
		public static RawRabbitConfiguration AsLegacy(this RawRabbitConfiguration config)
		{
			config.Exchange.Type = ExchangeType.Direct;
			config.RouteWithGlobalId = false;
			return config;
		}
	}
}
