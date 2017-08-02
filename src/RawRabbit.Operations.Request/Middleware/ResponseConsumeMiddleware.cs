using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Consumer;
using RawRabbit.Logging;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Request.Middleware
{
	public class ResponseConsumerOptions
	{
		public Action<IPipeBuilder> ResponseRecieved { get; set; }
		public Func<IPipeContext, ConsumerConfiguration> ResponseConfigFunc { get; set; }
		public Func<IPipeContext, string> CorrelationIdFunc { get; set; }
	}

	public class ResponseConsumeMiddleware : Pipe.Middleware.Middleware
	{
		protected readonly IConsumerFactory ConsumerFactory;
		protected readonly Pipe.Middleware.Middleware ResponsePipe;
		private readonly ILog _logger = LogProvider.For<ResponseConsumeMiddleware>();
		protected readonly ConcurrentDictionary<IBasicConsumer, ConcurrentDictionary<string, TaskCompletionSource<BasicDeliverEventArgs>>> AllResponses;
		protected Func<IPipeContext, ConsumerConfiguration> ResponseConfigFunc;
		protected Func<IPipeContext, string> CorrelationidFunc;

		public ResponseConsumeMiddleware(IConsumerFactory consumerFactory, IPipeBuilderFactory factory, ResponseConsumerOptions options)
		{
			ResponseConfigFunc = options?.ResponseConfigFunc ?? (context => context.GetResponseConfiguration());
			CorrelationidFunc = options?.CorrelationIdFunc ?? (context => context.GetBasicProperties()?.CorrelationId);
			ConsumerFactory = consumerFactory;
			ResponsePipe = factory.Create(options.ResponseRecieved);
			AllResponses = new ConcurrentDictionary<IBasicConsumer, ConcurrentDictionary<string, TaskCompletionSource<BasicDeliverEventArgs>>>();
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var respondCfg = GetResponseConfig(context);
			var correlationId = GetCorrelationid(context);
			var responseTsc = new TaskCompletionSource<BasicDeliverEventArgs>();

			var consumer = await ConsumerFactory.GetConfiguredConsumerAsync(respondCfg.Consume, token: token);
			var responses = AllResponses.GetOrAdd(consumer, c =>
				{
					var pendings = new ConcurrentDictionary<string, TaskCompletionSource<BasicDeliverEventArgs>>();
					c.OnMessage((sender, args) =>
					{
						TaskCompletionSource<BasicDeliverEventArgs> tsc;
						if (!pendings.TryGetValue(args.BasicProperties.CorrelationId, out tsc))
							return;
						tsc.TrySetResult(args);
					});
					return pendings;
				}
			);

			context.Properties.Add(PipeKey.Consumer, consumer);
			responses.TryAdd(correlationId, responseTsc);
			await Next.InvokeAsync(context, token);
			token.Register(() => responseTsc.TrySetCanceled());
			await responseTsc.Task;
			_logger.Info("Message '{messageId}' for correlatrion '{correlationId}' recieved.", responseTsc.Task.Result.BasicProperties.MessageId, correlationId);
			context.Properties.Add(PipeKey.DeliveryEventArgs, responseTsc.Task.Result);
			try
			{
				await ResponsePipe.InvokeAsync(context, token);
			}
			catch (Exception e)
			{
				_logger.Error(e, "Response pipe for message '{messageId}' executed unsuccessfully.", responseTsc.Task.Result.BasicProperties.MessageId);
				throw;
			}
		}

		protected virtual ConsumerConfiguration GetResponseConfig(IPipeContext context)
		{
			return ResponseConfigFunc?.Invoke(context);
		}

		protected virtual string GetCorrelationid(IPipeContext context)
		{
			return CorrelationidFunc?.Invoke(context);
		}
	}
}
