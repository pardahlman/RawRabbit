using System;

namespace RawRabbit.Common
{
	public interface INamingConvetions
	{
		Func<Type, string> ExchangeNamingConvention { get; set; }
		Func<Type, string> QueueNamingConvention { get; set; }
		Func<Type, Type, string> RpcExchangeNamingConvention { get; set; }
		Func<string> RpcReturnQueueNamingConvention { get; set; }
		Func<string> ErrorExchangeNamingConvention { get; set; }
		Func<string> ErrorQueueNamingConvention { get; set; }
	}

	public class NamingConvetions : INamingConvetions
	{
		public Func<Type, string> ExchangeNamingConvention { get; set; }
		public Func<Type, string> QueueNamingConvention { get; set; }
		public Func<Type, Type, string> RpcExchangeNamingConvention { get; set; }
		public Func<string> RpcReturnQueueNamingConvention { get; set; }
		public Func<string> ErrorExchangeNamingConvention { get; set; }
		public Func<string> ErrorQueueNamingConvention { get; set; }

		public NamingConvetions()
		{
			ExchangeNamingConvention = type => type?.Namespace?.ToLower();
			RpcExchangeNamingConvention = (request, response) => "default_rpc_exchange";
			QueueNamingConvention = type => type.Name.ToLower();
			RpcReturnQueueNamingConvention = () => $"default_rpc_response.{Guid.NewGuid()}";
			ErrorQueueNamingConvention = () => "default_error_queue";
			ErrorExchangeNamingConvention = () => "default_rpc_exchange";
		}
	}
}
