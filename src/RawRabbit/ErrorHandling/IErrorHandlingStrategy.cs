using System;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RawRabbit.Consumer;
using RawRabbit.Consumer.Contract;

namespace RawRabbit.ErrorHandling
{
	public interface IErrorHandlingStrategy
	{
		Task OnRequestHandlerExceptionAsync(IRawConsumer rawConsumer, BasicDeliverEventArgs args, Exception exception);
		Task OnResponseRecievedAsync<TResponse>(BasicDeliverEventArgs args, TaskCompletionSource<TResponse> responseTcs);
		void OnResponseRecieved<TResponse>(BasicDeliverEventArgs args, TaskCompletionSource<TResponse> responseTcs);
	}
}