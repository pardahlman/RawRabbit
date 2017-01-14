namespace RawRabbit.Compatibility.Legacy.Configuration.Exchange
{
	public enum ExchangeType
	{
		Unknown,

		/// <summary>
		/// Direct exchanges delivers messages to queues based on the message routing key. A direct exchange is ideal for the<br/>
		/// unicast routing of messages (although they can be used for multicast routing as well).
		/// </summary>
		Direct,

		/// <summary>
		/// Fanout exchanges routes messages to all of the queues that are bound to it and the routing key is ignored. <br/>
		/// If N queues   are bound to a fanout exchange, when a new message is published to that exchange a copy of <br/>
		/// the message is delivered to all N queues. Fanout exchanges are ideal for the broadcast routing of messages.
		/// </summary>
		Fanout,

		/// <summary>
		/// Headers exchanges is designed for routing on multiple attributes that are more easily expressed as message headers than<br/>
		/// a routing key. Headers exchanges ignore the routing key attribute
		/// </summary>
		Headers,

		/// <summary>
		/// Topic exchanges route messages to one or many queues based on matching between a message routing key and the pattern<br/>
		/// that was used to bind a queue to an exchange.
		/// </summary>
		Topic
	}
}
