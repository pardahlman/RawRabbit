using System;
using System.Threading.Tasks;
using RawRabbit.IntegrationTests.StateMachine.Phone;
using RawRabbit.Operations.Saga;
using RawRabbit.vNext.Pipe;
using Xunit;

namespace RawRabbit.IntegrationTests.StateMachine
{
	public class StateMachineTests
	{
		[Fact]
		public async Task Should_Trigger()
		{
			var caller = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = p => p.UseStateMachine() });
			var phoneAntenna = RawRabbitFactory.CreateTestClient();
			var recipient = RawRabbitFactory.CreateTestClient();

			await phoneAntenna.SubscribeAsync<PhonePickedUp>(up => phoneAntenna.PublishAsync(new DialSignalSent { CallId = up.CallId}));
			await phoneAntenna.SubscribeAsync<DialPhoneNumber>(up => phoneAntenna.PublishAsync(new PhoneCallDialed()));
			await recipient.SubscribeAsync<PhoneCallDialed>(dialed => recipient.PublishAsync(new PhonePickedUp()), cfg => cfg.FromDeclaredQueue(q => q.WithName("recipient")));
			await caller.RegisterStateMachineAsync<PhoneCallSaga, PhoneCallTriggers>();
			await caller.TriggerStateMachineAsync<PhoneCallSaga>(saga => saga.TriggerAsync(Trigger.TakenOffHold));
			await Task.Delay(TimeSpan.FromMinutes(10));

		}
	}
}
