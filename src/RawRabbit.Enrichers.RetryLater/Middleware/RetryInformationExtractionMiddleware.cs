using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Middleware
{
	public class RetryInformationExtractionOptions
	{
		public Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgsFunc { get; set; }
	}

	public class RetryInformationExtractionMiddleware : StagedMiddleware
	{
		private readonly IRetryInformationProvider _retryProvider;
		protected Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgsFunc;
		public override string StageMarker => Pipe.StageMarker.MessageRecieved;

		public RetryInformationExtractionMiddleware(IRetryInformationProvider retryProvider, RetryInformationExtractionOptions options = null)
		{
			_retryProvider = retryProvider;
			DeliveryArgsFunc = options?.DeliveryArgsFunc ?? (context => context.GetDeliveryEventArgs());
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
		{
			var retryInfo = GetRetryInformation(context);
			AddToPipeContext(context, retryInfo);
			return Next.InvokeAsync(context, token);
		}

		protected virtual BasicDeliverEventArgs GetDeliveryEventArgs(IPipeContext context)
		{
			return DeliveryArgsFunc?.Invoke(context);
		}

		protected virtual RetryInformation GetRetryInformation(IPipeContext context)
		{
			var devlieryArgs = GetDeliveryEventArgs(context);
			return _retryProvider.Get(devlieryArgs);
		}

		protected virtual void AddToPipeContext(IPipeContext context, RetryInformation retryInfo)
		{
			context.AddRetryInformation(retryInfo);
		}
	}
}
