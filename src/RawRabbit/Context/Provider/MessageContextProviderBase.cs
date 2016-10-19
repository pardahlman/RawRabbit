using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
#if NET451
using System.Runtime.Remoting.Messaging;
#endif

namespace RawRabbit.Context.Provider
{
	public abstract class MessageContextProviderBase<TMessageContext> : IMessageContextProvider<TMessageContext> where TMessageContext : IMessageContext
	{
		private readonly JsonSerializer _serializer;
		protected ConcurrentDictionary<Guid, TMessageContext> ContextDictionary;
#if NETSTANDARD1_5
		private readonly AsyncLocal<Guid> _globalMsgId;
#elif NET451
		private const string GlobalMsgId = "RawRabbit:GlobalMessageId";
#endif
		protected MessageContextProviderBase(JsonSerializer serializer)
		{
			_serializer = serializer;
			ContextDictionary = new ConcurrentDictionary<Guid, TMessageContext>();
#if NETSTANDARD1_5
			_globalMsgId = new AsyncLocal<Guid>();
#endif
		}

		public TMessageContext ExtractContext(object o)
		{
			if (o == null)
			{
				return default(TMessageContext);
			}
			var bytes = (byte[])o;
			var jsonHeader = Encoding.UTF8.GetString(bytes);
			TMessageContext context;
			using (var jsonReader = new JsonTextReader(new StringReader(jsonHeader)))
			{
				context = _serializer.Deserialize<TMessageContext>(jsonReader);
			}

#if NETSTANDARD1_5
			_globalMsgId.Value = context.GlobalRequestId;
#elif NET451
			CallContext.LogicalSetData(GlobalMsgId, context.GlobalRequestId);
#endif
			ContextDictionary.TryAdd(context.GlobalRequestId, context);
			return context;
		}

		public Task<TMessageContext> ExtractContextAsync(object o)
		{
			var context = ExtractContext(o);
			return Task.FromResult(context);
		}

		public object GetMessageContext(ref Guid globalMessageId)
		{

#if NETSTANDARD1_5
			if (globalMessageId == Guid.Empty)
			{
				globalMessageId = _globalMsgId?.Value ?? globalMessageId;
			}
#elif NET451
			if (globalMessageId == Guid.Empty)
			{
				globalMessageId = (Guid?)CallContext.LogicalGetData(GlobalMsgId) ?? globalMessageId;
			}	
#endif
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

		public abstract TMessageContext CreateMessageContext(Guid globalRequestId = default(Guid));
	}
}