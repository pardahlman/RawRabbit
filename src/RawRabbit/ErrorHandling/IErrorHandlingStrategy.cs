using System;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RawRabbit.Configuration.Respond;
using RawRabbit.Configuration.Subscribe;
using RawRabbit.Consumer.Abstraction;

namespace RawRabbit.ErrorHandling
{
    public interface IErrorHandlingStrategy
    {
        /// <summary>
        /// Executes the message handler with exception handling for sync and async calls.
        /// </summary>
        /// <param name="messageHandler"> The message handler</param>
        /// <param name="exceptionHandler">The exception handler to be called if message handler throws exception</param>
        /// <returns></returns>
        Task ExecuteAsync(Func<Task> messageHandler, Func<Exception, Task> exceptionHandler);

        /// <summary>
        /// Error strategy for unhandled exceptions thrown within RespondAsync message handler
        /// </summary>
        /// <param name="consumer">The consumer that was used for the message handler</param>
        /// <param name="config"> The configuration for the consumer</param>
        /// <param name="args">The recieved args</param>
        /// <param name="exception">The thrown exception</param>
        /// <returns></returns>
        Task OnResponseHandlerExceptionAsync(IRawConsumer consumer, IConsumerConfiguration config, BasicDeliverEventArgs args,  Exception exception);

        /// <summary>
        /// Error strategy for unhandled exceptions thrown within SubscribeAsync message handler
        /// </summary>
        /// <param name="consumer">The consumer that was used for the message handler</param>
        /// <param name="config"> The configuration for the consumer</param>
        /// <param name="args">The recieved args</param>
        /// <param name="exception">The thrown exception</param>
        /// <returns></returns>
        Task OnSubscriberExceptionAsync(IRawConsumer consumer, SubscriptionConfiguration config, BasicDeliverEventArgs args, Exception exception);

        /// <summary>
        /// Method called when response is recieved. This method can be used to re-throw exceptions from the responder.
        /// </summary>
        /// <param name="args">The recieved args</param>
        /// <param name="responseTcs">The TaskCompletionSource to the return task from RequestAsync</param>
        /// <returns></returns>
        Task OnResponseRecievedAsync(BasicDeliverEventArgs args, TaskCompletionSource<object> responseTcs);

        /// <summary>
        /// Method called when an unhandled exception is thrown when handling the recieved response
        /// </summary>
        /// <param name="consumer">The consumer that was used for the message handler</param>
        /// <param name="config"> The configuration for the consumer</param>
        /// <param name="args">The recieved args</param>
        /// <param name="responseTcs">The TaskCompletionSource to the return task from RequestAsync</param>
        /// <param name="exception">The thrown exception</param>
        /// <returns></returns>
        Task OnResponseRecievedException(IRawConsumer consumer, IConsumerConfiguration config, BasicDeliverEventArgs args, TaskCompletionSource<object> responseTcs, Exception exception);
    }
}