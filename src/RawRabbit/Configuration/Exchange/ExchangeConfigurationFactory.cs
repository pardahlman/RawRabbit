using System.Collections.Generic;
using RawRabbit.Common;

namespace RawRabbit.Configuration.Exchange
{
	public interface IExchangeConfigurationFactory
	{
		ExchangeConfiguration Create(string exchangeName);
		ExchangeConfiguration Create<TMessage>();
	}

	public class ExchangeConfigurationFactory : IExchangeConfigurationFactory
	{
		private readonly RawRabbitConfiguration _config;
		private readonly INamingConventions _conventions;

		public ExchangeConfigurationFactory(RawRabbitConfiguration config, INamingConventions conventions)
		{
			_config = config;
			_conventions = conventions;
		}

		public ExchangeConfiguration Create(string exchangeName)
		{
			return new ExchangeConfiguration
			{
				Arguments = new Dictionary<string, object>(),
				ExchangeType = _config.Exchange.Type.ToString().ToLower(),
				Durable = _config.Exchange.Durable,
				AutoDelete = _config.Exchange.AutoDelete,
				ExchangeName = exchangeName
			};
		}

		public ExchangeConfiguration Create<TMessage>()
		{
			var exchangeName = _conventions.ExchangeNamingConvention(typeof(TMessage));
			return Create(exchangeName);
		}
	}
}
