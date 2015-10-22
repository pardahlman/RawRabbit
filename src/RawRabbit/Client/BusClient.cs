using RawRabbit.Common;
using RawRabbit.Common.Operations;
using RawRabbit.Core.Message;

namespace RawRabbit.Client
{
	public class BusClient : BusClientBase<MessageContext>
	{
		public BusClient(IConfigurationEvaluator configEval, ISubscriber<MessageContext> subscriber, IPublisher publisher, IResponder<MessageContext> responder, IRequester request)
			: base(configEval, subscriber, publisher, responder, request)
		{ }
	}
}
