using System;
using RawRabbit.Configuration.Consume;

namespace RawRabbit.Operations.Respond.Configuration
{
	public interface IRespondConfigurationFactory
	{
		RespondConfiguration Create<TRequest, TResponse>();
		RespondConfiguration Create(Type requestType, Type respondType);
	}

	public class RespondConfigurationFactory : IRespondConfigurationFactory
	{
		private readonly IConsumeConfigurationFactory _consumerFactory;

		public RespondConfigurationFactory(IConsumeConfigurationFactory consumerFactory)
		{
			_consumerFactory = consumerFactory;
		}

		public RespondConfiguration Create<TRequest, TResponse>()
		{
			return Create(typeof(TRequest), typeof(TResponse));
		}

		public RespondConfiguration Create(Type requestType, Type respondType)
		{
			var consumeCfg = _consumerFactory.Create(requestType);
			return new RespondConfiguration
			{
				Queue = consumeCfg.Queue,
				RoutingKey = consumeCfg.RoutingKey,
				NoAck = consumeCfg.NoAck,
				Exchange = consumeCfg.Exchange,
				Arguments = consumeCfg.Arguments,
				ConsumerTag = consumeCfg.ConsumerTag,
				Exclusive = consumeCfg.Exclusive,
				PrefetchCount = consumeCfg.PrefetchCount,
				NoLocal = consumeCfg.NoLocal
			};
		}
	}
}
