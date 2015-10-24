using System;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Configuration
{
	public class RawRabbitConfiguration
	{
		public string Hostname { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
		public string VirtualHost { get; set; }

		/// <summary>
		/// The amount of time to wait for response to request. Defaults to 10 seconds.
		/// </summary>
		public TimeSpan RequestTimeout { get; set; }
		public GeneralExchangeConfiguration Exchange { get; set; }
		public GeneralQueueConfiguration Queue { get; set; }

		public RawRabbitConfiguration()
		{
			Hostname = "localhost";
			Username = "guest";
			Password = "guest";
			RequestTimeout = TimeSpan.FromSeconds(10);
			Exchange = new GeneralExchangeConfiguration
			{
				AutoDelete = false,
				Durable = true,
				Type = ExchangeType.Direct
			};
			Queue = new GeneralQueueConfiguration
			{
				Exclusive = false,
				AutoDelete = false,
				Durable = false
			};
		}

		public static RawRabbitConfiguration Default => new RawRabbitConfiguration();

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
}
