using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Operations.Saga.Model;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Saga.Middleware
{
	public class TopologyMiddleware : Pipe.Middleware.Middleware
	{
		private readonly ITopologyProvider _provider;
		private readonly IConsumerConfigurationFactory _consumerFactory;

		public TopologyMiddleware(ITopologyProvider provider, IConsumerConfigurationFactory consumerFactory)
		{
			_provider = provider;
			_consumerFactory = consumerFactory;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var triggers = context.Get<Dictionary<object, List<ExternalTrigger>>>(SagaKey.ExternalTriggers);
			var topologyTasks = new List<Task>();
			foreach (var trigger in triggers.Values.SelectMany(v => v.OfType<MessageTypeTrigger>()))
			{
				var cfg = _consumerFactory.Create(trigger.MessageType);
				var topoloyTask = _provider.BindQueueAsync(cfg.Queue, cfg.Exchange, cfg.RoutingKey);
				topologyTasks.Add(topoloyTask);
			}
			return Task
				.WhenAll(topologyTasks)
				.ContinueWith(t => Next.InvokeAsync(context))
				.Unwrap();
		}
	}
}
