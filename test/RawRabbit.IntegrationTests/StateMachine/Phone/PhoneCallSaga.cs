using System;
using System.Threading.Tasks;
using RawRabbit.Operations.Saga.Model;
using RawRabbit.Pipe;
using Stateless;

namespace RawRabbit.IntegrationTests.StateMachine.Phone
{
	public class PhoneCallSaga : Saga<State, Trigger, PhoneSagaDto>
	{
		private readonly IBusClient _busClient;

		public PhoneCallSaga(IBusClient busClient, PhoneSagaDto sagaDto = null) : base(sagaDto)
		{
			_busClient = busClient;
		}

		protected override void ConfigureState(StateMachine<State, Trigger> phoneCall)
		{
			var callConnectedTrigger = TriggerParameters.Get<IPipeContext>(Trigger.CallConnected);
			var callDialed =  TriggerParameters.Get<IPipeContext>(Trigger.CallDialed);

			phoneCall.Configure(State.OnHook)
				.Permit(Trigger.TakenOffHold, State.OffHook);

			phoneCall.Configure(State.OffHook)
				.OnEntryAsync(() => _busClient.PublishAsync(new PhonePickedUp()))
				.Permit(Trigger.DialSignalSent, State.DialTone);

			phoneCall.Configure(State.DialTone)
				.OnEntryAsync(() => _busClient.PublishAsync(new DialPhoneNumber { Number = "911"}))
				.Permit(Trigger.CallDialed, State.Ringing);
				
			phoneCall.Configure(State.Ringing)
				.OnEntryFromAsync(callDialed,OnRingingFromDialed)
				.Permit(Trigger.HungUp, State.OffHook)
				.Permit(Trigger.CallConnected, State.Connected);

			phoneCall.Configure(State.Connected)
				.OnEntryFromAsync(callConnectedTrigger, context => Task.FromResult(0))
				.OnExit(t => { })
				.InternalTransition(Trigger.MuteMicrophone, t => OnMute())
				.InternalTransition(Trigger.UnmuteMicrophone, t => OnUnmute())
				.Permit(Trigger.LeftMessage, State.OffHook)
				.Permit(Trigger.HungUp, State.OffHook)
				.Permit(Trigger.PlacedOnHold, State.OnHold);
		}

		private Task OnRingingFromDialed(IPipeContext context)
		{
			return Task.FromResult(0);
		}

		public override PhoneSagaDto Initialize()
		{
			return new PhoneSagaDto
			{
				State = State.OnHook
			};
		}

		private static void OnUnmute()
		{
			Console.WriteLine("Microphone muted!");
		}

		private void OnMute()
		{
			SagaDto.Id = Guid.NewGuid();
			Console.WriteLine("Microphone muted!");
		}
	}
}
