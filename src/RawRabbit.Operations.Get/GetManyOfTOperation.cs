using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Get;
using RawRabbit.Operations.Get.Model;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Extensions;

namespace RawRabbit
{
	public static class GetManyOfTOperation
	{
		public static async Task<Ackable<List<Ackable<TMessage>>>> GetManyAsync<TMessage>(this IBusClient busClient, int batchSize, Action<IGetConfigurationBuilder> config = null, CancellationToken token = default(CancellationToken))
		{
			var channel = await busClient.CreateChannelAsync();
			var result = new List<Ackable<TMessage>>();

			while (result.Count < batchSize)
			{
				var ackableMessage = await busClient.GetAsync<TMessage>(config, c => c.Properties.TryAdd(PipeKey.Channel, channel), token);
				if (ackableMessage.Content == null)
				{
					break;
				}
				result.Add(ackableMessage);
			}

			return new Ackable<List<Ackable<TMessage>>>(
				result,
				result.FirstOrDefault()?.Channel,
				list => list.Where(a => !a.Acknowledged).SelectMany(a => a.DeliveryTags).ToArray()
			);
		}
	}
}
