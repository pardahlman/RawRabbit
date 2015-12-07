using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RawRabbit.Context.Provider
{
	public abstract class MessageContextProviderBase<TMessageContext> : IMessageContextProvider<TMessageContext> where TMessageContext : IMessageContext
	{
		protected ConcurrentDictionary<Guid, TMessageContext> ContextDictionary;

		protected MessageContextProviderBase()
		{
			ContextDictionary = new ConcurrentDictionary<Guid, TMessageContext>();
		}

		public TMessageContext ExtractContext(object o)
		{
			var bytes = (byte[])o;
			var jsonHeader = Encoding.UTF8.GetString(bytes);
			var context = JsonConvert.DeserializeObject<TMessageContext>(jsonHeader);
			ContextDictionary.TryAdd(context.GlobalRequestId, context);
			return context;
		}

		public Task<TMessageContext> ExtractContextAsync(object o)
		{
			var context = ExtractContext(o);
			return Task.FromResult(context);
		}

		public object GetMessageContext(Guid globalMessageId)
		{
			var context = globalMessageId != Guid.Empty && ContextDictionary.ContainsKey(globalMessageId)
				? ContextDictionary[globalMessageId]
				: CreateMessageContext(globalMessageId);
			var contextAsJson = JsonConvert.SerializeObject(context);
			var contextAsBytes = (object) Encoding.UTF8.GetBytes(contextAsJson);
			return contextAsBytes;
		}

		public Task<object> GetMessageContextAsync(Guid globalMessageId)
		{
			var ctxTask = globalMessageId != Guid.Empty && ContextDictionary.ContainsKey(globalMessageId)
				? Task.FromResult(ContextDictionary[globalMessageId])
				: CreateMessageContextAsync();

			return ctxTask
				.ContinueWith(contextTask => JsonConvert.SerializeObject(contextTask.Result))
				.ContinueWith(jsonTask => (object)Encoding.UTF8.GetBytes(jsonTask.Result));
		}

		protected virtual Task<TMessageContext> CreateMessageContextAsync()
		{
			return Task.FromResult(CreateMessageContext());
		}

		protected abstract TMessageContext CreateMessageContext(Guid globalRequestId = default(Guid));
	}
}