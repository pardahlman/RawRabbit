using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Consumer;
using RawRabbit.Logging;
using RawRabbit.Operations.Request.Context;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Request.Middleware
{
	public class ResponseConsumerOptions
	{
		public Action<IPipeBuilder> ResponseRecieved { get; set; }
		public Func<IPipeContext, ConsumerConfiguration> ResponseConfigFunc { get; set; }
		public Func<IPipeContext, string> CorrelationIdFunc { get; set; }
		public Func<IPipeContext, bool> UseDedicatedConsumer { get; set; }
	}

	public class ResponseConsumeMiddleware : Pipe.Middleware.Middleware
	{
		protected static readonly ConcurrentDictionary<IBasicConsumer, ConcurrentDictionary<string, TaskCompletionSource<BasicDeliverEventArgs>>> AllResponses =
			new ConcurrentDictionary<IBasicConsumer, ConcurrentDictionary<string, TaskCompletionSource<BasicDeliverEventArgs>>>();

		protected readonly IConsumerFactory ConsumerFactory;
		protected readonly Pipe.Middleware.Middleware ResponsePipe;
		private readonly ILog _logger = LogProvider.For<ResponseConsumeMiddleware>();
		protected Func<IPipeContext, ConsumerConfiguration> ResponseConfigFunc;
		protected Func<IPipeContext, string> CorrelationidFunc;
		protected Func<IPipeContext, bool> DedicatedConsumerFunc;

		public ResponseConsumeMiddleware(IConsumerFactory consumerFactory, IPipeBuilderFactory factory, ResponseConsumerOptions options)
		{
			ResponseConfigFunc = options?.ResponseConfigFunc ?? (context => context.GetResponseConfiguration());
			CorrelationidFunc = options?.CorrelationIdFunc ?? (context => context.GetBasicProperties()?.CorrelationId);
			DedicatedConsumerFunc = options?.UseDedicatedConsumer ?? (context => context.GetDedicatedResponseConsumer());
			ConsumerFactory = consumerFactory;
			ResponsePipe = factory.Create(options.ResponseRecieved);
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var respondCfg = GetResponseConfig(context);
			var correlationId = GetCorrelationid(context);
			var dedicatedConsumer = GetDedicatedConsumer(context);
			var responseTsc = new TaskCompletionSource<BasicDeliverEventArgs>();

			IBasicConsumer consumer;
			if (dedicatedConsumer)
			{
				consumer = await ConsumerFactory.CreateConsumerAsync(token: token);
				ConsumerFactory.ConfigureConsume(consumer, respondCfg.Consume);
			}
			else
			{
				consumer = await ConsumerFactory.GetConfiguredConsumerAsync(respondCfg.Consume, token: token);
			}
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
			if (dedicatedConsumer)
			{
				_logger.Info("Disposing dedicated consumer on queue {queueName}", respondCfg.Consume.QueueName);
				consumer.Model.Dispose();
				AllResponses.TryRemove(consumer, out _);
			}
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

		protected virtual bool GetDedicatedConsumer(IPipeContext context)
		{
			return DedicatedConsumerFunc?.Invoke(context) ?? false;
		}
	}

	public static class ResposeConsumerMiddlewareExtensions
	{
		private const string DedicatedResponseConsumer = "Request:DedicatedResponseConsumer";

		/// <summary>
		/// Use with caution!
		/// 
		/// Instruct the Request operation to create a unique consumer for
		/// the response queue. The consumer will be cancelled once the
		/// response message is recieved.
		/// </summary>
		public static IRequestContext UseDedicatedResponseConsumer(this IRequestContext context, bool useDedicated = true)
		{
			context.Properties.AddOrReplace(DedicatedResponseConsumer, useDedicated);
			return context;
		}

		public static bool GetDedicatedResponseConsumer(this IPipeContext context)
		{
			return context.Get(DedicatedResponseConsumer, false);
		}
	}
}
