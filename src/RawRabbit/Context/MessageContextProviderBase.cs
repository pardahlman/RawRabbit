using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RawRabbit.Context
{
	public abstract class MessageContextProviderBase<TMessageContext> : IMessageContextProvider<TMessageContext> where TMessageContext : MessageContext
	{
		public virtual string ContextHeaderName => "message_context";
		protected ConcurrentDictionary<Guid, TMessageContext> ContextDictionary;

		protected MessageContextProviderBase()
		{
			ContextDictionary = new ConcurrentDictionary<Guid, TMessageContext>();
		}

		public Task<TMessageContext> ExtractContextAsync(object o)
		{
			var bytes = (byte[])o;
			var jsonHeader = Encoding.UTF8.GetString(bytes);
			var context = JsonConvert.DeserializeObject<TMessageContext>(jsonHeader);
			ContextDictionary.TryAdd(context.GlobalRequestId, context);
			return Task.FromResult(context);
		}

		public Task<object> GetMessageContextAsync(Guid globalMessageId)
		{
			Task<TMessageContext> createOrGetContextTask;
			if (globalMessageId != Guid.Empty && ContextDictionary.ContainsKey(globalMessageId))
			{
				createOrGetContextTask = Task.FromResult(ContextDictionary[globalMessageId]);
			}
			else
			{
				createOrGetContextTask = CreateMessageContextAsync();
			}
			return createOrGetContextTask
				.ContinueWith(contextTask => JsonConvert.SerializeObject(contextTask.Result))
				.ContinueWith(jsonTask => (object)Encoding.UTF8.GetBytes(jsonTask.Result));
		}

		protected abstract Task<TMessageContext> CreateMessageContextAsync();
	}
}