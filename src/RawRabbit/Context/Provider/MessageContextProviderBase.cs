using System;
using System.Text;
using System.Threading.Tasks;
using RawRabbit.Serialization;

namespace RawRabbit.Context.Provider
{
	public abstract class MessageContextProviderBase<TMessageContext> : IMessageContextProvider<TMessageContext> where TMessageContext : IMessageContext
	{
		private readonly ISerializer _serializer;

		protected MessageContextProviderBase(ISerializer serializer)
		{
			_serializer = serializer;
		}

		public TMessageContext ExtractContext(object o)
		{
			if (o == null)
			{
				return default(TMessageContext);
			}
			var bytes = (byte[])o;
			var jsonHeader = Encoding.UTF8.GetString(bytes);
			return _serializer.Deserialize<TMessageContext>(jsonHeader);
		}

		public Task<TMessageContext> ExtractContextAsync(object o)
		{
			var context = ExtractContext(o);
			return Task.FromResult(context);
		}

		public object GetMessageContext(ref Guid globalMessageId)
		{
			var context = CreateMessageContext(globalMessageId);
			var objAsJson = _serializer.Serialize(context);
			var objAsBytes = (object)Encoding.UTF8.GetBytes(objAsJson);
			return objAsBytes;
		}

		public Task<object> GetMessageContextAsync(Guid globalMessageId)
		{
			return CreateMessageContextAsync()
				.ContinueWith(contextTask =>
				{
					var objAsJson = _serializer.Serialize(contextTask.Result);
					var objAsBytes = (object)Encoding.UTF8.GetBytes(objAsJson);
					return objAsBytes;
				});
		}

		protected virtual Task<TMessageContext> CreateMessageContextAsync()
		{
			return Task.FromResult(CreateMessageContext());
		}

		public abstract TMessageContext CreateMessageContext(Guid globalRequestId = default(Guid));
	}
}