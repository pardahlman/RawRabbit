using System;
using RawRabbit.Context;

namespace RawRabbit.Extensions.Client
{
	public interface IBusClient : IBusClient<MessageContext>
	{
	}

	public class ExtendableBusClient : ExtendableBusClient<MessageContext>, IBusClient
	{
		public ExtendableBusClient(IServiceProvider serviceProvider) : base(serviceProvider)
		{ }
	}
}
