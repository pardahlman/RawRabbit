using System;
using System.Collections.Generic;
using RawRabbit.Common;

namespace RawRabbit.Configuration.Exchange
{
	public interface IExchangeDeclarationFactory
	{
		ExchangeDeclaration Create(string exchangeName);
		ExchangeDeclaration Create<TMessage>();
		ExchangeDeclaration Create(Type messageType);
	}

	public class ExchangeDeclarationFactory : IExchangeDeclarationFactory
	{
		private readonly RawRabbitConfiguration _config;
		private readonly INamingConventions _conventions;

		public ExchangeDeclarationFactory(RawRabbitConfiguration config, INamingConventions conventions)
		{
			_config = config;
			_conventions = conventions;
		}

		public ExchangeDeclaration Create(string exchangeName)
		{
			return new ExchangeDeclaration
			{
				Arguments = new Dictionary<string, object>(),
				ExchangeType = _config.Exchange.Type.ToString().ToLower(),
				Durable = _config.Exchange.Durable,
				AutoDelete = _config.Exchange.AutoDelete,
				Name = exchangeName
			};
		}

		public ExchangeDeclaration Create<TMessage>()
		{
			return Create(typeof(TMessage));
		}

		public ExchangeDeclaration Create(Type messageType)
		{
			var exchangeName = _conventions.ExchangeNamingConvention(messageType);
			return Create(exchangeName);
		}
	}
}
