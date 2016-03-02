using System;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RawRabbit.Configuration.Respond;
using RawRabbit.Consumer.Abstraction;

namespace RawRabbit.ErrorHandling
{
	public interface IErrorHandlingStrategy
	{
		Task OnRequestHandlerExceptionAsync(IRawConsumer rawConsumer, IConsumerConfiguration cfg, BasicDeliverEventArgs args, Exception exception);
		Task OnResponseRecievedAsync(BasicDeliverEventArgs args, TaskCompletionSource<object> responseTcs);
		void OnResponseRecieved(BasicDeliverEventArgs args, TaskCompletionSource<object> responseTcs);
	}
}