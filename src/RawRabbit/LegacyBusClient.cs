using RawRabbit.Common;
using RawRabbit.Context;
using RawRabbit.Operations.Abstraction;

namespace RawRabbit
{
	public interface ILegacyBusClient : ILegacyBusClient<MessageContext> { }

	public class LegacyBusClient : BaseBusClient<MessageContext>, ILegacyBusClient
	{
		public LegacyBusClient(IConfigurationEvaluator configEval, ISubscriber<MessageContext> subscriber, IPublisher publisher, IResponder<MessageContext> responder, IRequester requester)
			: base(configEval, subscriber, publisher, responder, requester)
		{ }
	}
}
