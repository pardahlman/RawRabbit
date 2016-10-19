using System;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RawRabbit.Common;

namespace RawRabbit.Pipe.Middleware
{
	public class PublishAcknowledgeMiddleware : Middleware
	{
		public override Task InvokeAsync(IPipeContext context)
		{
			var channel = context.GetChannel();
			if (channel.NextPublishSeqNo == 0UL)
			{
				channel.ConfirmSelect();
			}

			var thisSequence = channel.NextPublishSeqNo;
			var ackTcs = new TaskCompletionSource<ulong>();

			EventHandler<BasicAckEventArgs> channelBasicAck = null;
			channelBasicAck = (sender, args) =>
			{
				if (args.DeliveryTag < thisSequence)
				{
					return;
				}
				if (args.DeliveryTag != thisSequence && !args.Multiple)
				{
					return;
				}
				channel.BasicAcks -= channelBasicAck;
				ackTcs.TrySetResult(thisSequence);
			};
			channel.BasicAcks += channelBasicAck;
			context.Properties.Add(PipeKey.PublishAcknowledger, ackTcs.Task);
			return Next.InvokeAsync(context);
		}
	}
}
