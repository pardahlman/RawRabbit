using System;
using System.Threading.Tasks;

namespace RawRabbit.Context.Provider
{
	public class MessageContextProvider<TContext> : MessageContextProviderBase<TContext> where TContext : IMessageContext
	{
		private readonly Func<Task<TContext>> _createContext;

		public MessageContextProvider(Func<Task<TContext>> createContext = null)
		{
			_createContext = createContext;
		}

		protected override Task<TContext> CreateMessageContextAsync()
		{
			if (_createContext != null)
			{
				return _createContext();
			}

			TContext context;
			try
			{
				context =  (TContext)Activator.CreateInstance(typeof(TContext), new object[] {});
				context.GlobalRequestId = Guid.NewGuid();
			}
			catch (Exception)
			{
				context = default(TContext);
			}
			return Task.FromResult(context);
		}
	}
}
