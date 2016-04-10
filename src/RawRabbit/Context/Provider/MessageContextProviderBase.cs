using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RawRabbit.Context.Provider
{
	public abstract class MessageContextProviderBase<TMessageContext> : IMessageContextProvider<TMessageContext> where TMessageContext : IMessageContext
	{
		private readonly JsonSerializer _serializer;
		protected ConcurrentDictionary<Guid, TMessageContext> ContextDictionary;

		protected MessageContextProviderBase(JsonSerializer serializer)
		{
			_serializer = serializer;
			ContextDictionary = new ConcurrentDictionary<Guid, TMessageContext>();
		}

		public TMessageContext ExtractContext(object o)
		{
			var bytes = (byte[])o;
			var jsonHeader = Encoding.UTF8.GetString(bytes);
			TMessageContext context;
			using (var jsonReader = new JsonTextReader(new StringReader(jsonHeader)))
			{
				context = _serializer.Deserialize<TMessageContext>(jsonReader);
			}
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
			var contextAsJson = SerializeContext(context);
			var contextAsBytes = (object) Encoding.UTF8.GetBytes(contextAsJson);
			return contextAsBytes;
		}

		public Task<object> GetMessageContextAsync(Guid globalMessageId)
		{
			var ctxTask = globalMessageId != Guid.Empty && ContextDictionary.ContainsKey(globalMessageId)
				? Task.FromResult(ContextDictionary[globalMessageId])
				: CreateMessageContextAsync();

			return ctxTask
				.ContinueWith(contextTask => SerializeContext(contextTask.Result))
				.ContinueWith(jsonTask => (object)Encoding.UTF8.GetBytes(jsonTask.Result));
		}

		private string SerializeContext(TMessageContext messageContext)
		{
			using (var sw = new StringWriter())
			{
				_serializer.Serialize(sw, messageContext);
				return sw.GetStringBuilder().ToString();
			}
		}

		protected virtual Task<TMessageContext> CreateMessageContextAsync()
		{
			return Task.FromResult(CreateMessageContext());
		}

		protected abstract TMessageContext CreateMessageContext(Guid globalRequestId = default(Guid));
	}
}