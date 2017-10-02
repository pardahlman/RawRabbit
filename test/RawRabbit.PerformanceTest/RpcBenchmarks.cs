using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using RawRabbit.Instantiation;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Operations.Respond.Core;

namespace RawRabbit.PerformanceTest
{
	public class RpcBenchmarks
	{
		private IBusClient _busClient;
		private Request _request;
		private Respond _respond;
		public event EventHandler MessageRecieved;
		public delegate void MessageRecievedEventHandler(EventHandler e);

		[Setup]
		public void Setup()
		{
			_busClient = RawRabbitFactory.CreateSingleton();
			_request = new Request();
			_respond = new Respond();
			_busClient.RespondAsync<Request,Respond>(message =>
				Task.FromResult(_respond)
			);
			_busClient.RespondAsync<Request, Respond>(message =>
				Task.FromResult(_respond),
				ctx => ctx.UseRespondConfiguration(cfg => cfg
					.Consume(c => c
						.WithRoutingKey("custom_key"))
					.FromDeclaredQueue(q => q
						.WithName("custom_queue")
						.WithAutoDelete())
					.OnDeclaredExchange(e => e
						.WithName("custom_exchange")
						.WithAutoDelete()))
			);
		}

		[Cleanup]
		public void Cleanup()
		{
			_busClient.DeleteQueueAsync<Request>();
			(_busClient as IDisposable).Dispose();
		}

		[Benchmark]
		public async Task DirectRpc()
		{
			await _busClient.RequestAsync<Request, Respond>(_request, ctx => ctx
				.UseRequestConfiguration(cfg => cfg
					.PublishRequest(p => p
						.WithProperties(prop => prop.DeliveryMode = 1)))
			);
		}

		[Benchmark]
		public async Task NormalRpc()
		{
			await _busClient.RequestAsync<Request, Respond>(_request, ctx => ctx
				.UseRequestConfiguration(cfg => cfg
					.PublishRequest(p => p
						.OnDeclaredExchange(e => e
							.WithName("custom_exchange")
							.WithAutoDelete())
						.WithRoutingKey("custom_key")
						.WithProperties(prop => prop.DeliveryMode = 1))
					.ConsumeResponse(r => r
						.Consume(c => c
							.WithRoutingKey("response_key"))
						.FromDeclaredQueue(q => q
							.WithName("response_queue")
							.WithAutoDelete())
						.OnDeclaredExchange(e => e
							.WithName("response_exchange")
							.WithAutoDelete()
						)
					)
				)
			);
		}

		public class Request { }
		public class Respond { }
	}
}
