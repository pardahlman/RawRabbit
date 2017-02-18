using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Operations.StateMachine.Trigger;

namespace RawRabbit.Operations.StateMachine
{
	public static class StateMachineExtension
	{
		public static async Task RegisterStateMachineAsync<TTriggerConfiguration>(
			this IBusClient busClient,
			CancellationToken ct = default(CancellationToken)) where TTriggerConfiguration : TriggerConfigurationCollection, new()
		{
			var triggerCfgs = new TTriggerConfiguration().GetTriggerConfiguration();
			foreach (var triggerCfg in triggerCfgs)
			{
				await busClient.InvokeAsync(triggerCfg.Pipe, triggerCfg.Context, ct);
			}
		}
	}
}
