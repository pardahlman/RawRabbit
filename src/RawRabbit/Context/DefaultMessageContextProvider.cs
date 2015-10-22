using System;
using System.Threading.Tasks;

namespace RawRabbit.Context
{
	public class DefaultMessageContextProvider : MessageContextProviderBase<MessageContext>
	{
		private readonly Func<Task<Guid>> _asyncGlobalId;

		public DefaultMessageContextProvider(Func<Task<Guid>> asyncGlobalId)
		{
			_asyncGlobalId = asyncGlobalId;
		}

		protected async override Task<MessageContext> CreateMessageContextAsync()
		{
			return new MessageContext
			{
				GlobalRequestId = await _asyncGlobalId()
			};
		}
	}
}
