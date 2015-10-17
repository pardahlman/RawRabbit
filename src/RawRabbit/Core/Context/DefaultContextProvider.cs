using System;
using System.Threading.Tasks;

namespace RawRabbit.Core.Context
{
	public class DefaultContextProvider : IContextProvider
	{
		private readonly Func<Task<string>> _asyncSessionFunk;
		private readonly Func<string> _sessionFunc;

		public DefaultContextProvider(Func<Task<string>> asyncSessionFunk, Func<string> sessionFunc)
		{
			_asyncSessionFunk = asyncSessionFunk;
			_sessionFunc = sessionFunc;
		}

		public string GetSessionId()
		{
			throw new System.NotImplementedException();
		}

		public Task<string> GetSessionIdAsync()
		{
			throw new System.NotImplementedException();
		}
	}
}
