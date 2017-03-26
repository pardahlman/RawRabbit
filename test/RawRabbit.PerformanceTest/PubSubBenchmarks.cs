using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using RawRabbit.Instantiation;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.PerformanceTest
{
	public class PubSubBenchmarks
	{
		private IBusClient _busClient;
		private Task _completedTask;
		private Message _message;
		public event EventHandler MessageRecieved;
		public delegate void MessageRecievedEventHandler(EventHandler e);

		[Setup]
		public void Setup()
		{
			_busClient = RawRabbitFactory.CreateSingleton();
			_completedTask = Task.FromResult(0);
			_message = new Message();
			_busClient.SubscribeAsync<Message>(message =>
			{
				MessageRecieved(message, EventArgs.Empty);
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

			EventHandler onMessageRecieved = (sender, args) => { msgTsc.TrySetResult(sender as Message); };
			MessageRecieved += onMessageRecieved;

			_busClient.PublishAsync(_message, ctx => ctx.UsePublishAcknowledge(false));
			await msgTsc.Task;
 			MessageRecieved -= onMessageRecieved;
		}

		[Benchmark]
		public async Task ConsumerAcknowledgements_On()
		{
			var msgTsc = new TaskCompletionSource<Message>();

			EventHandler onMessageRecieved = (sender, args) => { msgTsc.TrySetResult(sender as Message); };
			MessageRecieved += onMessageRecieved;

			_busClient.PublishAsync(_message);
			await msgTsc.Task;
			MessageRecieved -= onMessageRecieved;
		}
	}

	public class Message { }
}
