using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using RawRabbit.Enrichers.MessageContext;
using RawRabbit.Instantiation;

namespace RawRabbit.PerformanceTest
{
	public class MessageContextBenchmarks
	{
		private IBusClient _withoutContext;
		private Task _completedTask;
		private MessageA _messageA;
		private IBusClient _withContext;
		private MessageB _messageB;
		public event EventHandler MessageReceived;
		public delegate void MessageReceivedEventHandler(EventHandler e);

		[Setup]
		public void Setup()
		{
			_withoutContext = RawRabbitFactory.CreateSingleton();
			_withContext = RawRabbitFactory.CreateSingleton(new RawRabbitOptions
			{
				Plugins = p => p.UseMessageContext<MessageContext>()
			});
			_completedTask = Task.FromResult(0);
			_messageA = new MessageA();
			_messageB = new MessageB();
			_withoutContext.SubscribeAsync<MessageA>(message =>
			{
				MessageReceived(message, EventArgs.Empty);
				return _completedTask;
			});
			_withContext.SubscribeAsync<MessageB, MessageContext>((message, context) =>
			{
				MessageReceived(message, EventArgs.Empty);
				return _completedTask;
			});
		}

		[Cleanup]
		public void Cleanup()
		{
			_withoutContext.DeleteQueueAsync<MessageA>();
			_withoutContext.DeleteQueueAsync<MessageB>();
			(_withoutContext as IDisposable).Dispose();
			(_withContext as IDisposable).Dispose();
		}

		[Benchmark]
		public async Task MessageContext_FromFactory()
		{
			var msgTsc = new TaskCompletionSource<Message>();

			EventHandler onMessageReceived = (sender, args) => { msgTsc.TrySetResult(sender as Message); };
			MessageReceived += onMessageReceived;

			_withContext.PublishAsync(_messageB);
			await msgTsc.Task;
			MessageReceived -= onMessageReceived;
		}

		[Benchmark]
		public async Task MessageContext_None()
		{
			var msgTsc = new TaskCompletionSource<Message>();

			EventHandler onMessageReceived = (sender, args) => { msgTsc.TrySetResult(sender as Message); };
			MessageReceived += onMessageReceived;

			_withoutContext.PublishAsync(_messageA);
			await msgTsc.Task;
			MessageReceived -= onMessageReceived;
		}


		public class MessageA { }
		public class MessageB { }
		public class MessageContext { }
	}
}
