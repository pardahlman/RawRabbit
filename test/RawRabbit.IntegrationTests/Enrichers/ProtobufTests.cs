using System;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ProtoBuf;
using RawRabbit.Exceptions;
using RawRabbit.Instantiation;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;

namespace RawRabbit.IntegrationTests.Enrichers
{
	public class ProtobufTests
	{
		[Fact]
		public async Task Should_Deliver_And_Recieve_Messages_Serialized_With_Protobuf()
		{
			using (var client = RawRabbitFactory.CreateTestClient(new RawRabbitOptions{ Plugins = p => p.UseProtobuf() }))
			{
				/** Setup **/
				var tcs = new TaskCompletionSource<ProtoMessage>();
				var message = new ProtoMessage
				{
					Member = "Straight into bytes",
					Id = Guid.NewGuid()
				};
				await client.SubscribeAsync<ProtoMessage>(msg =>
				{
					tcs.TrySetResult(msg);
					return Task.CompletedTask;
				});

				/** Test **/
				await client.PublishAsync(message);
				await tcs.Task;

				/** Assert **/
				Assert.Equal(tcs.Task.Result.Id, message.Id);
				Assert.Equal(tcs.Task.Result.Member, message.Member);
			}
		}

		[Fact]
		public async Task Should_Perform_Rpc_With_Messages_Serialized_With_Protobuf()
		{
			using (var client = RawRabbitFactory.CreateTestClient(new RawRabbitOptions {Plugins = p => p.UseProtobuf()}))
			{
				/* Setup */
				var response = new ProtoResponse {Id = Guid.NewGuid()};
				await client.RespondAsync<ProtoRequest, ProtoResponse>(request => Task.FromResult(response));

				/* Test */
				var recieved = await client.RequestAsync<ProtoRequest, ProtoResponse>(new ProtoRequest());

				/* Assert */
				Assert.Equal(recieved.Id, response.Id);
			}
		}

		[Fact]
		public async Task Should_Publish_Message_To_Error_Exchange_If_Serializer_Mismatch()
		{
			using (var protobufClient = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = p => p.UseProtobuf() }))
			using (var jsonClient = RawRabbitFactory.CreateTestClient())
			{
				/** Setup **/
				var handlerInvoked = false;
				var tcs = new TaskCompletionSource<ProtoMessage>();
				var message = new ProtoMessage
				{
					Member = "Straight into bytes",
					Id = Guid.NewGuid()
				};
				await jsonClient.SubscribeAsync<ProtoMessage>(msg =>
				{
					handlerInvoked = true; // Should never get here
					return Task.CompletedTask;
				});
				await protobufClient.SubscribeAsync<ProtoMessage>(msg =>
				{
					tcs.TrySetResult(msg);
					return Task.CompletedTask;
				}, ctx => ctx.UseSubscribeConfiguration(cfg => cfg
					.FromDeclaredQueue(q => q.WithName("error_queue"))
					.OnDeclaredExchange(e => e.WithName("default_error_exchange"))
				));

				/** Test **/
				await protobufClient.PublishAsync(message);
				await tcs.Task;

				/** Assert **/
				Assert.False(handlerInvoked);
				Assert.Equal(tcs.Task.Result.Id, message.Id);
				Assert.Equal(tcs.Task.Result.Member, message.Member);
			}
		}

		[Fact]
		public async Task Should_Throw_Exception_If_Responder_Can_Not_Deserialize_Request_And_Content_Type_Check_Is_Activated()
		{
			using (var protobufClient = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = p => p.UseProtobuf() }))
			using (var jsonClient = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				await jsonClient.RespondAsync<ProtoRequest, ProtoResponse>(request =>
					Task.FromResult(new ProtoResponse())
				);

				/* Test */
				/* Assert */
				var e = await Assert.ThrowsAsync<MessageHandlerException>(() =>
					protobufClient.RequestAsync<ProtoRequest, ProtoResponse>(new ProtoRequest {  Id = Guid.NewGuid()}
				, ctx => ctx.UseContentTypeCheck()));
			}
		}
	}

	[ProtoContract]
	public class ProtoMessage
	{
		[ProtoMember(1)]
		public Guid Id { get; set; }

		[ProtoMember(2)]
		public string Member { get; set; }
	}

	[ProtoContract]
	public class ProtoRequest
	{
		[ProtoMember(1)]
		public Guid Id { get; set; }
	}

	[ProtoContract]
	public class ProtoResponse
	{
		[ProtoMember(1)]
		public Guid Id { get; set; }
	}
}
