using System;
using System.Threading.Tasks;

namespace RawRabbit.Context.Provider
{
	public class MessageContextProvider<TContext> : MessageContextProviderBase<TContext> where TContext : IMessageContext
	{
		private readonly Func<Task<TContext>> _createContextAsync;
		private readonly Func<TContext> _createContext;

		public MessageContextProvider(Func<Task<TContext>> createContextAsync = null)
		{
			_createContextAsync = createContextAsync;
		}

		public MessageContextProvider(Func<TContext> createContext)
		{
			_createContext = createContext;
		}

		protected override Task<TContext> CreateMessageContextAsync()
		{
			return _createContextAsync != null
				? _createContextAsync()
				: Task.FromResult(ActivateOrDefault());
		}

		protected override TContext CreateMessageContext(Guid globalRequestId = default(Guid))
		{
			var context = _createContext != null
						? _createContext()
						: ActivateOrDefault();
			if (globalRequestId != Guid.Empty)
			{
				context.GlobalRequestId = globalRequestId;
			}
			return context;
		}

		private static TContext ActivateOrDefault()
		{
			TContext context;
			try
			{
				context = (TContext)Activator.CreateInstance(typeof(TContext), new object[] { });
				context.GlobalRequestId = Guid.NewGuid();
			}
			catch (Exception)
			{
				context = default(TContext);
			}
			return context;
		}
	}
}
