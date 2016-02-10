using RawRabbit.Common;
using RawRabbit.Context;
using RawRabbit.Operations.Abstraction;

namespace RawRabbit
{
	public interface IBusClient : IBusClient<MessageContext> { }

	public class BusClient : BaseBusClient<MessageContext>, IBusClient
	{
		public BusClient(IConfigurationEvaluator configEval, ISubscriber<MessageContext> subscriber, IPublisher publisher, IResponder<MessageContext> responder, IRequester requester)
			: base(configEval, subscriber, publisher, responder, requester)
		{ }
	}
}
