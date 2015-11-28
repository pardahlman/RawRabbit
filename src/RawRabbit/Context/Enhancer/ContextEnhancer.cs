using RabbitMQ.Client.Events;
using RawRabbit.Consumer.Contract;

namespace RawRabbit.Context.Enhancer
{
	public class ContextEnhancer : IContextEnhancer
	{
		public void WireUpContextFeatures<TMessageContext1>(TMessageContext1 context, IRawConsumer consumer, BasicDeliverEventArgs args) where TMessageContext1 : IMessageContext
		{
			if (context == null)
			{
				return;
			}

			var advancedCtx = context as IAdvancedMessageContext;
			if (advancedCtx != null)
			{
				advancedCtx.Nack = () =>
				{
					consumer.NackedDeliveryTags.Add(args.DeliveryTag);
					consumer.Model.BasicNack(args.DeliveryTag, false, true);
				};
			}
		}
	}
}
