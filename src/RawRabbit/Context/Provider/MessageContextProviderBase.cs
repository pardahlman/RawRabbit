using System;
using System.Threading.Tasks;
using RawRabbit.Serialization;

namespace RawRabbit.Context.Provider
{
	public abstract class MessageContextProviderBase<TMessageContext> : IMessageContextProvider<TMessageContext> where TMessageContext : IMessageContext
	{
		private readonly IHeaderSerializer _headerSerializer;

		protected MessageContextProviderBase(IHeaderSerializer headerSerializer)
		{
			_headerSerializer = headerSerializer;
		}

		public TMessageContext ExtractContext(object o)
		{
			var context = _headerSerializer.Deserialize<TMessageContext>(o);
			return context;
		}

		public Task<TMessageContext> ExtractContextAsync(object o)
		{
			var context = ExtractContext(o);
			return Task.FromResult(context);
		}

		public object GetMessageContext(ref Guid globalMessageId)
		{
			var context = CreateMessageContext(globalMessageId);
			return _headerSerializer.Serialize(context);
		}

		public Task<object> GetMessageContextAsync(Guid globalMessageId)
		{
			return CreateMessageContextAsync()
				.ContinueWith(contextTask => _headerSerializer.Serialize(contextTask.Result));
		}

		protected virtual Task<TMessageContext> CreateMessageContextAsync()
		{
			return Task.FromResult(CreateMessageContext());
		}

		public abstract TMessageContext CreateMessageContext(Guid globalRequestId = default(Guid));
	}
}