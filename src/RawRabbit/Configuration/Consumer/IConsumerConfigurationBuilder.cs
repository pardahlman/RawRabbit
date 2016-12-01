using System;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Legacy.Respond;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Consumer
{
	public interface IConsumerConfigurationBuilder
	{
		/// <summary>
		/// Specify the topology features of the Exchange to consume from.
		/// Exchange will be declared.
		/// </summary>
		/// <param name="exchange">Builder for exchange features.</param>
		/// <returns></returns>
		IConsumerConfigurationBuilder OnDeclaredExchange(Action<IExchangeDeclarationBuilder> exchange);

		/// <summary>
		/// Specify the topology features of the Queue to consume from.
		/// Queue will be declared.
		/// /// </summary>
		/// <param name="queue"></param>
		/// <returns></returns>
		IConsumerConfigurationBuilder FromDeclaredQueue(Action<IQueueDeclarationBuilder> queue);

		IConsumerConfigurationBuilder Consume(Action<IConsumeConfigurationBuilder> consume);
	}
}