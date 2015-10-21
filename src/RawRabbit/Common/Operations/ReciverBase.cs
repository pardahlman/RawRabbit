using System.Threading.Tasks;
using RawRabbit.Common.Serialization;

namespace RawRabbit.Common.Operations
{
	public abstract class ReciverBase : OperatorBase
	{
		protected IMessageSerializer Serializer;

		protected ReciverBase(IChannelFactory channelFactory, IMessageSerializer serializer) : base(channelFactory)
		{
			Serializer = serializer;
		}

		private Task<byte[]> CreateMessageAsync<T>(T message)
		{
			if (message == null)
			{
				return Task.FromResult(new byte[0]);
			}
			return Task.Factory.StartNew(() => Serializer.Serialize(message));
		}
	}
}
