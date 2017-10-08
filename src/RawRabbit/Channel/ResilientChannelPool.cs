using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel.Abstraction;

namespace RawRabbit.Channel
{
	public class ResilientChannelPool : DynamicChannelPool
	{
		protected readonly IChannelFactory ChannelFactory;
		private readonly int _desiredChannelCount;

		public ResilientChannelPool(IChannelFactory factory, int channelCount)
			: this(factory, CreateSeed(factory, channelCount)) { }

		public ResilientChannelPool(IChannelFactory factory)
			: this(factory, Enumerable.Empty<IModel>()) { }

		public ResilientChannelPool(IChannelFactory factory, IEnumerable<IModel> seed) : base(seed)
		{
			ChannelFactory = factory;
			_desiredChannelCount = seed.Count();
		}

		private static IEnumerable<IModel> CreateSeed(IChannelFactory factory, int channelCount)
		{
			for (var i = 0; i < channelCount; i++)
			{
				yield return factory.CreateChannelAsync().GetAwaiter().GetResult();
			}
		}

		public override async Task<IModel> GetAsync(CancellationToken ct = default(CancellationToken))
		{
			var currentCount = GetActiveChannelCount();
			if (currentCount < _desiredChannelCount)
			{
				var createCount = _desiredChannelCount - currentCount;
				for (var i = 0; i < createCount; i++)
				{
					var channel = await ChannelFactory.CreateChannelAsync(ct);
					Add(channel);
				}
			}
			return await base.GetAsync(ct);
		}
	}
}
