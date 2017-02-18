using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Operations.StateMachine.Core;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Operations.StateMachine.Middleware
{
	public class GlobalLockMiddleware : StagedMiddleware
	{
		private readonly IGlobalLock _globalLock;
		public override string StageMarker => Pipe.StageMarker.MessageDeserialized;

		public GlobalLockMiddleware(IGlobalLock globalLock)
		{
			_globalLock = globalLock;
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			return _globalLock.ExecuteAsync(context.Get<Guid>(StateMachineKey.ModelId), () => Next.InvokeAsync(context, token), token);
		}
	}
}
