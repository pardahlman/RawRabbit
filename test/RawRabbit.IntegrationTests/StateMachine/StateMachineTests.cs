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

			await phoneAntenna.SubscribeAsync<PhonePickedUp>(up => phoneAntenna.PublishAsync(new DialSignalSent()));
			await phoneAntenna.SubscribeAsync<DialPhoneNumber>(up => phoneAntenna.PublishAsync(new PhoneCallDialed()));
			await recipient.SubscribeAsync<PhoneCallDialed>(dialed => recipient.PublishAsync(new PhonePickedUp()), cfg => cfg.FromQueue(q => q.WithName("recipient")));
			await caller.RegisterStateMachineAsync<PhoneCallSaga, PhoneCallTriggers>();
			await caller.TriggerStateMachineAsync<PhoneCallSaga>(saga => saga.TriggerAsync(Trigger.TakenOffHold), new Guid("6B3B099D-35BF-436D-A051-0D5671DA6D25"));
			await Task.Delay(TimeSpan.FromMinutes(10));

		}
	}
}
