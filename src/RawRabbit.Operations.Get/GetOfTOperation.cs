using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Get;
using RawRabbit.Operations.Get.Middleware;
using RawRabbit.Operations.Get.Model;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class GetOfTOperation
	{

		public static readonly Action<IPipeBuilder> DeserializedBodyGetPipe = pipe => pipe
			.Use<GetConfigurationMiddleware>()
			.Use<ConventionNamingMiddleware>()
			.Use<ChannelCreationMiddleware>()
			.Use<BasicGetMiddleware>()
			.Use<BodyDeserializationMiddleware>(new MessageDeserializationOptions
			{
				BodyTypeFunc = context => context.GetMessageType(),
				BodyFunc = context => context.GetBasicGetResult()?.Body
			})
			.Use<AckableResultMiddleware>(new AckableResultOptions
			{
				DeliveryTagFunc = context => context.GetBasicGetResult()?.DeliveryTag ?? 0,
				ContentFunc = context => context.GetMessage()
			});

		public static Task<Ackable<TMessage>> GetAsync<TMessage>(this IBusClient busClient, Action<IGetConfigurationBuilder> config = null, CancellationToken token = default(CancellationToken))
		{
			return GetAsync<TMessage>(busClient, config, null, token);
		}

		internal static Task<Ackable<TMessage>> GetAsync<TMessage>(this IBusClient busClient, Action<IGetConfigurationBuilder> config = null, Action<IPipeContext> pipeAction = null, CancellationToken token = default(CancellationToken))
		{
			return busClient
				.InvokeAsync(DeserializedBodyGetPipe, context =>
				{
					context.Properties.Add(PipeKey.ConfigurationAction, config);
					context.Properties.Add(PipeKey.MessageType, typeof(TMessage));
					pipeAction?.Invoke(context);
				}, token)
				.ContinueWith(tContext => tContext.Result.Get<Ackable<object>>(GetKey.AckableResult).AsAckable<TMessage>(), token);
		}


	}
}
