using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RawRabbit.Context
{
	public abstract class MessageContextProviderBase<TMessageContext> : IMessageContextProvider<TMessageContext> where TMessageContext : MessageContext
	{
		public virtual string ContextHeaderName => "message_context";

		public Task<TMessageContext> ExtractContextAsync(object o)
		{
			var bytes = (byte[])o;
			var jsonHeader = Encoding.UTF8.GetString(bytes);
			var context = JsonConvert.DeserializeObject<TMessageContext>(jsonHeader);
			return Task.FromResult(context);
		}

		public Task<object> GetMessageContextAsync()
		{
			return CreateMessageContextAsync()
				.ContinueWith(contextTask => JsonConvert.SerializeObject(contextTask.Result))
				.ContinueWith(jsonTask => (object)Encoding.UTF8.GetBytes(jsonTask.Result));
		}

		protected abstract Task<TMessageContext> CreateMessageContextAsync();
	}
}