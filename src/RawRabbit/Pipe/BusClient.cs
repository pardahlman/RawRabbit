using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Publish;
using RawRabbit.Configuration.Request;
using RawRabbit.Configuration.Respond;
using RawRabbit.Configuration.Subscribe;
using RawRabbit.Context;

namespace RawRabbit.Pipe
{
	public class BusClient<TContext> : IBusClient<TContext> where TContext : IMessageContext
	{
		private readonly IPipeContextFactory _pipeContext;
		private readonly IResourceDisposer _resourceDisposer;
		private readonly Middleware.Middleware _subscribe;
		private readonly Middleware.Middleware _publish;

		public BusClient(IPipeContextFactory pipeContext, IPipeBuilderFactory pipeBuilderFactory, IStartup startup, IResourceDisposer resourceDisposer)
		{
			var subscribe = pipeBuilderFactory.Create();
			startup.ConfigureSubscribe(subscribe);
			_subscribe = subscribe.Build();

			var publish = pipeBuilderFactory.Create();
			startup.ConfigurePublish(publish);
			_publish = publish.Build();
			_pipeContext = pipeContext;
			_resourceDisposer = resourceDisposer;
		}

		public ISubscription SubscribeAsync<T>(Func<T, TContext, Task> subscribeMethod, Action<ISubscriptionConfigurationBuilder> configuration = null)
		{
			Func<object, IMessageContext, Task> genericHandler = (o, c) => subscribeMethod((T)o, (TContext)c);

			var context = _pipeContext.CreateContext(
				new KeyValuePair<string, object>(PipeKey.Operation, Operation.Subscribe),
				new KeyValuePair<string, object>(PipeKey.MessageType, typeof(T)),
				new KeyValuePair<string, object>(PipeKey.MessageHandler, genericHandler),
				new KeyValuePair<string, object>(PipeKey.ConfigurationAction, configuration)
			);
			_subscribe
				.InvokeAsync(context)
				.GetAwaiter()
				.GetResult();
			return null;
		}

		public Task PublishAsync<T>(T message = default(T), Guid globalMessageId = new Guid(), Action<IPublishConfigurationBuilder> configuration = null)
		{
			var context = _pipeContext.CreateContext(
				new KeyValuePair<string, object>(PipeKey.Operation, Operation.Publish),
				new KeyValuePair<string, object>(PipeKey.MessageType, typeof(T)),
				new KeyValuePair<string, object>(PipeKey.Message, message),
				new KeyValuePair<string, object>(PipeKey.ConfigurationAction, configuration)
			);
			return _publish
				.InvokeAsync(context)
				.ContinueWith(t => context.Get(PipeKey.PublishAcknowledger, Task.FromResult(0) as Task))
				.Unwrap();
		}

		public ISubscription RespondAsync<TRequest, TResponse>(Func<TRequest, TContext, Task<TResponse>> onMessage, Action<IResponderConfigurationBuilder> configuration = null)
		{
			throw new NotImplementedException();
		}

		public Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest message = default(TRequest), Guid globalMessageId = new Guid(),
			Action<IRequestConfigurationBuilder> configuration = null)
		{
			throw new NotImplementedException();
		}

		public Task ShutdownAsync(TimeSpan? graceful = null)
		{
			_resourceDisposer.Dispose();
			return Task.FromResult(0);
		}
	}

	public class BusClient : BusClient<MessageContext>, IBusClient
	{
		public BusClient(IPipeContextFactory pipeContext, IPipeBuilderFactory pipeBuilderFactory, IStartup startup, IResourceDisposer down)
			: base(pipeContext, pipeBuilderFactory, startup, down)
		{
		}
	}
}
