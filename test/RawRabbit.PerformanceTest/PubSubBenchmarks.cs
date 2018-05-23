using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using RawRabbit.Instantiation;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.PerformanceTest
{
	public class PubSubBenchmarks
	{
		private IBusClient _busClient;
		private Task _completedTask;
		private Message _message;
		public event EventHandler MessageReceived;
		public delegate void MessageReceivedEventHandler(EventHandler e);

		[Setup]
		public void Setup()
		{
			_busClient = RawRabbitFactory.CreateSingleton();
			_completedTask = Task.FromResult(0);
			_message = new Message();
			_busClient.SubscribeAsync<Message>(message =>
			{
				MessageReceived(message, EventArgs.Empty);
				return _completedTask;
			});
		}

		[Cleanup]
		public void Cleanup()
		{
			_busClient.DeleteQueueAsync<Message>();
			(_busClient as IDisposable).Dispose();
		}

		[Benchmark]
		public async Task ConsumerAcknowledgements_Off()
		{
			var msgTsc = new TaskCompletionSource<Message>();

			EventHandler onMessageReceived = (sender, args) => { msgTsc.TrySetResult(sender as Message); };
			MessageReceived += onMessageReceived;

			_busClient.PublishAsync(_message, ctx => ctx.UsePublishAcknowledge(false));
			await msgTsc.Task;
 			MessageReceived -= onMessageReceived;
		}

		[Benchmark]
		public async Task ConsumerAcknowledgements_On()
		{
			var msgTsc = new TaskCompletionSource<Message>();

			EventHandler onMessageReceived = (sender, args) => { msgTsc.TrySetResult(sender as Message); };
			MessageReceived += onMessageReceived;

			_busClient.PublishAsync(_message);
			await msgTsc.Task;
			MessageReceived -= onMessageReceived;
		}

		[Benchmark]
		public async Task DeliveryMode_NonPersistant()
		{
			var msgTsc = new TaskCompletionSource<Message>();

			EventHandler onMessageReceived = (sender, args) => { msgTsc.TrySetResult(sender as Message); };
			MessageReceived += onMessageReceived;

			_busClient.PublishAsync(_message, ctx => ctx
				.UsePublishConfiguration(cfg => cfg
					.WithProperties(p => p.DeliveryMode = 1))
			);
			await msgTsc.Task;
			MessageReceived -= onMessageReceived;
		}

		[Benchmark]
		public async Task DeliveryMode_Persistant()
		{
			var msgTsc = new TaskCompletionSource<Message>();

			EventHandler onMessageReceived = (sender, args) => { msgTsc.TrySetResult(sender as Message); };
			MessageReceived += onMessageReceived;

			_busClient.PublishAsync(_message, ctx => ctx
				.UsePublishConfiguration(cfg => cfg
					.WithProperties(p => p.DeliveryMode = 2))
			);
			await msgTsc.Task;
			MessageReceived -= onMessageReceived;
		}
	}

	public class Message { }
}
