using RawRabbit.Common;
using RawRabbit.Context;
using RawRabbit.Operations;
using RawRabbit.Operations.Contracts;

namespace RawRabbit
{
	public class BusClient : BusClientBase<MessageContext>
	{
		public BusClient(IConfigurationEvaluator configEval, ISubscriber<MessageContext> subscriber, IPublisher publisher, IResponder<MessageContext> responder, IRequester requester)
			: base(configEval, subscriber, publisher, responder, requester)
		{ }
	}
}
