using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Consumer;
using RawRabbit.Operations.Saga.Model;
using RawRabbit.Operations.Saga.Repository;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Saga.Middleware
{
	public class ConsumerTriggerMiddleware : Pipe.Middleware.Middleware
	{
		private readonly IConsumerFactory _consumerFactory;
		private readonly IPipeContextFactory _contextFactory;
		private readonly IConsumerConfigurationFactory _consumerConfFactory;
		private readonly ISagaRepository _sagaRepo;

		public ConsumerTriggerMiddleware(IConsumerFactory consumerFactory, IPipeContextFactory contextFactory, IConsumerConfigurationFactory consumerConfFactory, ISagaRepository sagaRepo)
		{
			_consumerFactory = consumerFactory;
			_contextFactory = contextFactory;
			_consumerConfFactory = consumerConfFactory;
			_sagaRepo = sagaRepo;
		}
		public override Task InvokeAsync(IPipeContext context)
		{
			var sagaType = context.Get<Type>(SagaKey.SagaType);
			var triggers = context.Get<Dictionary<object, List<ExternalTrigger>>>(SagaKey.ExternalTriggers);
			var consumerTasks = new List<Task>();
			var subscriptions = new List<Subscription>();
			foreach (var t in triggers)
			{
				var trigger = t.Key;
				var msgTypeTriggers = t.Value.OfType<MessageTypeTrigger>();
				foreach (var msgTrigger in msgTypeTriggers)
				{
					var cfg = _consumerConfFactory.Create(msgTrigger.MessageType);
					var consumerTask = _consumerFactory
						.GetConsumerAsync(cfg)
						.ContinueWith(tConsumer =>
						{
							tConsumer.Result.OnMessage((sender, args) =>
							{
								Task.Run(async () =>
								{
									var childContext = _contextFactory.CreateContext(context.Properties.ToArray());
									childContext.Properties.Add(PipeKey.DeliveryEventArgs, args);
									//var sagaId = (Guid)args.BasicProperties.Headers[PropertyHeaders.SagaId];
									var sagaId = new Guid("6B3B099D-35BF-436D-A051-0D5671DA6D25");
									var saga = await _sagaRepo.GetAsync(sagaId, sagaType);
									return saga.TriggerAsync(trigger, childContext);
								});
							});
							subscriptions.Add(new Subscription(tConsumer.Result, cfg.Queue.QueueName));
						});
					consumerTasks.Add(consumerTask);
				}
			}
			return Task
				.WhenAll(consumerTasks)
				.ContinueWith(t =>
				{
					context.Properties.Add("Subscriptions", subscriptions);
					return Next.InvokeAsync(context);
				})
				.Unwrap();
		}
	}
}
