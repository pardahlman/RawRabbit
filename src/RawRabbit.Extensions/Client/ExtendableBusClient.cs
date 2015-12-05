using System;
using RawRabbit.Context;

namespace RawRabbit.Extensions.Client
{
	public class ExtendableBusClient : ExtendableBusClient<MessageContext>
	{
		public ExtendableBusClient(IServiceProvider serviceProvider) : base(serviceProvider)
		{ }
	}
}
