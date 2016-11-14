using System;
using System.Text;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Operations.Respond.Acknowledgement;
using RawRabbit.Operations.Respond.Configuration;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Operations.Respond.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class RespondExtension
	{
		public static readonly Action<IPipeBuilder> ConsumePipe = pipe => pipe
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.MessageRecieved))
			.Use<MessageDeserializationMiddleware>(new MessageDeserializationOptions
			{
				MessageTypeFunc = context => context.GetRequestMessageType()
			})
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.MessageDeserialized))
			.Use<RespondInvokationMiddleware>()
			.Use<AutoAckMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.HandlerInvoked))
			.Use<BasicPropertiesMiddleware>(new BasicPropertiesOptions
			{
				PropertyModier = (context, properties) =>
				{
					properties.CorrelationId = context.GetDeliveryEventArgs()?.BasicProperties.CorrelationId;
					properties.Type = context.GetResponseMessageType()?.GetUserFriendlyName();
				}
			})
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.BasicPropertiesCreated))
			.Use<MessageSerializationMiddleware>(new MessageSerializationOptions
			{
				MessageFunc = context => context.Get<object>(RespondKey.ResponseMessage),
				SerializedMessageKey = RespondKey.SerializedResponse
			})
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.ResponseSerialized))
			.Use<ReplyToExtractionMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.ReplyToExtracted))
			.Use<TransientChannelMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.RespondChannelCreated))
			.Use<BasicPublishMiddleware>(new BasicPublishOptions
			{
				ExchangeNameFunc = context => context.GetPublicationAddress()?.ExchangeName,
				RoutingKeyFunc = context => context.GetPublicationAddress()?.RoutingKey,
				MandatoryFunc = context => true,
				BodyFunc = context => Encoding.UTF8.GetBytes(context.Get<string>(RespondKey.SerializedResponse))
			})
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.ResponsePublished));

		public static readonly Action<IPipeBuilder> AutoAckPipe = pipe => pipe
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.Initiated))
			.Use<RespondConfigurationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.ConsumeConfigured))
			.Use<QueueDeclareMiddleware>(new QueueDeclareOptions { QueueFunc = context => context.GetRespondConfiguration()?.Queue })
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.QueueDeclared))
			.Use<ExchangeDeclareMiddleware>(new ExchangeDeclareOptions { ExchangeFunc = context => context.GetRespondConfiguration()?.Exchange })
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.ExchangeDeclared))
			.Use<QueueBindMiddleware>(new QueueBindOptions { ConsumeFunc = context => context.GetRespondConfiguration() })
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.QueueBound))
			.Use<ConsumerCreationMiddleware>(new ConsumerCreationOptions { ConsumeFunc = context => context.GetRespondConfiguration() })
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.ConsumerCreated))
			.Use<MessageConsumeMiddleware>(new ConsumeOptions { Pipe = ConsumePipe })
			.Use<SubscriptionMiddleware>(new SubscriptionOptions { QueueFunc = context => context.GetRespondConfiguration()?.Queue });

		public static readonly Action<IPipeBuilder> ExplicitAckPipe = AutoAckPipe + (pipe => pipe
			.Replace<MessageConsumeMiddleware, MessageConsumeMiddleware>(args: new ConsumeOptions
			{
				Pipe = ConsumePipe + (consume => consume.Replace<AutoAckMiddleware, ExplicitAckMiddleware>())
			}));

		public static Task RespondAsync<TRequest, TResponse>(this IBusClient client, Func<TRequest, Task<TResponse>> handler, Action<IRespondConfigurationBuilder> configuration = null)
		{
			return client
				.InvokeAsync(
					AutoAckPipe,
					ctx =>
					{
						Func<object[], Task> genericHandler = args => handler((TRequest)args[0]).ContinueWith(tResponse => tResponse.Result as object);

						ctx.Properties.Add(RespondKey.RequestMessageType, typeof(TRequest));
						ctx.Properties.Add(RespondKey.ResponseMessageType, typeof(TResponse));
						ctx.Properties.Add(PipeKey.ConfigurationAction, configuration);
						ctx.Properties.Add(PipeKey.MessageHandler, genericHandler);
					}
				);
		}

		public static Task RespondAsync<TRequest, TResponse>(this IBusClient client, Func<TRequest, Task<TypedAcknowlegement<TResponse>>> handler, Action<IRespondConfigurationBuilder> configuration = null)
		{
			return client
				.InvokeAsync(
					ExplicitAckPipe,
					ctx =>
					{
						Func<object[], Task> genericHandler = args => handler((TRequest)args[0]).ContinueWith(tResponse => tResponse.Result.AsUntyped());

						ctx.Properties.Add(RespondKey.RequestMessageType, typeof(TRequest));
						ctx.Properties.Add(RespondKey.ResponseMessageType, typeof(TResponse));
						ctx.Properties.Add(PipeKey.ConfigurationAction, configuration);
						ctx.Properties.Add(PipeKey.MessageHandler, genericHandler);
					}
				);
		}
	}
}
