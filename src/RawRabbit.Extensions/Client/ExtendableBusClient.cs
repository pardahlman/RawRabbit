using System;
using RawRabbit.Context;

namespace RawRabbit.Extensions.Client
{
	public interface ILegacyBusClient : ILegacyBusClient<MessageContext>
	{
	}

	public class ExtendableBusClient : ExtendableBusClient<MessageContext>, ILegacyBusClient
	{
		public ExtendableBusClient(IServiceProvider serviceProvider) : base(serviceProvider)
		{ }
	}
}
