using System;
using System.Threading.Tasks;
using RawRabbit.Consumer;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Request.Middleware
{
	public class ResponseConsumerOptions
	{
		public Action<IPipeBuilder> ResponseRecieved { get; set; }
	}

	public class ResponseConsumeMiddleware : Pipe.Middleware.Middleware
	{
		private readonly IConsumerFactory _consumerFactory;
		private readonly Pipe.Middleware.Middleware _responsePipe;

		public ResponseConsumeMiddleware(IConsumerFactory consumerFactory, IPipeBuilderFactory factory, ResponseConsumerOptions options)
		{
			_consumerFactory = consumerFactory;
			_responsePipe = factory.Create(options.ResponseRecieved);
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var respondCfg = context.GetResponseConfiguration();
			var correlationId = context.GetBasicProperties()?.CorrelationId;

			var executionTsc = new TaskCompletionSource<bool>();

			_consumerFactory
				.GetConsumerAsync(respondCfg.Consume)
				.ContinueWith(tConsumer =>
				{
					context.Properties.Add(PipeKey.Consumer, tConsumer.Result);
					tConsumer.Result.OnMessage((sender, args) =>
					{
						if (!string.Equals(args.BasicProperties.CorrelationId, correlationId))
						{
							return;
						}
						context.Properties.Add(PipeKey.DeliveryEventArgs, args);
						_responsePipe
							.InvokeAsync(context)
							.ContinueWith(t => executionTsc.TrySetResult(t.IsCompleted));
					}, abort: args => string.Equals(args.BasicProperties.CorrelationId, correlationId));
					Next.InvokeAsync(context);
				});

			return executionTsc.Task;
		}
	}
}
