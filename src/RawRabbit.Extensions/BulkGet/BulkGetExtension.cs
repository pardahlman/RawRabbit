using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Context;
using RawRabbit.Context.Provider;
using RawRabbit.Extensions.BulkGet.Configuration;
using RawRabbit.Extensions.BulkGet.Model;
using RawRabbit.Extensions.Client;
using RawRabbit.Serialization;

namespace RawRabbit.Extensions.BulkGet
{
	public static class BulkGetExtension
	{
		public static BulkResult<TContextType> GetMessages<TContextType>(this IBusClient<TContextType> client, Action<IBulkGetConfigurationBuilder> cfg)
			where TContextType : IMessageContext
		{
			var extended = (client as ExtendableBusClient<TContextType>);
			if (extended == null)
			{
				throw new InvalidOperationException("Bus client does not support extensions. Make sure that the client is of type ExtendableBusClient.");
			}
			var channel = extended.GetService<IChannelFactory>().CreateChannel();
			var contextProvider = extended.GetService<IMessageContextProvider<TContextType>>();
			var serialier = extended.GetService<IMessageSerializer>();

			var result = new Dictionary<Type, List<IBulkMessage>>();
			var msgConfigs = GetMessageConfigurations(cfg);
			
			foreach (var msgConfig in msgConfigs)
			{
				var rawMsgs = new List<IBulkMessage>();
				var batchSize = msgConfig.GetAll ? int.MaxValue : msgConfig.BatchSize;

				foreach (var queueName in msgConfig.QueueNames)
				{
					while (rawMsgs.Count < batchSize)
					{
						var getResult = channel.BasicGet(queueName, msgConfig.NoAck);
						if (getResult == null)
						{
							break;
						}

						object message;
						try
						{
							message = serialier.Deserialize(getResult.Body, msgConfig.MessageType);
						}
						catch (Exception)
						{
							// msg is not of right type.
							continue;
						}
						var context = contextProvider.ExtractContext(getResult.BasicProperties.Headers[PropertyHeaders.Context]);
						var bulkMessage = CreateBulkMessage(msgConfig.MessageType, message, context, channel, getResult.DeliveryTag);
						rawMsgs.Add(bulkMessage);
					}
				}
				result.Add(msgConfig.MessageType, rawMsgs);
			}

			return new BulkResult<TContextType>(result);
		}

		private static IEnumerable<MessageConfiguration> GetMessageConfigurations(Action<IBulkGetConfigurationBuilder> cfg)
		{
			var builder = new BulkGetConfigurationBuilder();
			cfg(builder);
			return builder.Configurations;
		}

		private static IBulkMessage CreateBulkMessage<TMessageContext>(Type messageType, object message, TMessageContext context, IModel channel, ulong deliveryTag)
		{
			var bulkMsgType = typeof(BulkMessage<,>).MakeGenericType(messageType, typeof(TMessageContext));
			var msg = Activator.CreateInstance(bulkMsgType, channel, deliveryTag, context, message) as IBulkMessage;
			return msg;
		}
	}
}
