using System.Collections.Generic;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using System.Threading.Tasks;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class BasicPublishMiddleware : Pipe.Middleware.BasicPublishMiddleware
	{
		public BasicPublishMiddleware(IExclusiveLock exclusive, BasicPublishOptions options = null)
			: base(exclusive, options) { }

		protected override async Task BasicPublish(
				IModel channel,
				string exchange,
				string routingKey,
				bool mandatory,
				IBasicProperties basicProps,
				byte[] body,
				IPipeContext context)
		{
			var policy = context.GetPolicy(PolicyKeys.BasicPublish);
			await policy.ExecuteAsync(action: () => { base.BasicPublish(channel, exchange, routingKey, mandatory, basicProps, body, context); return Task.FromResult(0); },
				contextData: new Dictionary<string, object>
				{
					[RetryKey.PipeContext] = context,
					[RetryKey.ExchangeName] = exchange,
					[RetryKey.RoutingKey] = routingKey,
					[RetryKey.PublishMandatory] = mandatory,
					[RetryKey.BasicProperties] = basicProps,
					[RetryKey.PublishBody] = body,
				});			
		}
	}
}
