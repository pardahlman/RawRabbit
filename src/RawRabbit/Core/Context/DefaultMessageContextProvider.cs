using System;
using System.Threading.Tasks;
using RawRabbit.Core.Message;

namespace RawRabbit.Core.Context
{
	public class DefaultMessageContextProvider : MessageContextProviderBase<MessageContext>
	{
		private readonly Func<Task<Guid>> _asyncGlobalId;

		public DefaultMessageContextProvider(Func<Task<Guid>> asyncGlobalId)
		{
			_asyncGlobalId = asyncGlobalId;
		}

		protected async override Task<MessageContext> CreateMessageContext()
		{
			return new MessageContext
			{
				GlobalRequestId = await _asyncGlobalId()
			};
		}
	}
}
