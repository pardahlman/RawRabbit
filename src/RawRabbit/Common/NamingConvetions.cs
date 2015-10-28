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
			QueueNamingConvention = type => CreateShortAfqn(type);
			RpcReturnQueueNamingConvention = () => "amq.rabbitmq.reply-to";
			ErrorQueueNamingConvention = () => "default_error_queue";
			ErrorExchangeNamingConvention = () => "default_rpc_exchange";
		}

		private static string CreateShortAfqn(Type type, string path = "", string delimeter = ".")
		{
			var t = $"{path}{(string.IsNullOrEmpty(path) ? string.Empty : delimeter)}{GetNonGenericTypeName(type)}";

			if (type.IsGenericType)
			{
				t += "[";
				foreach (var argument in type.GenericTypeArguments)
				{
					t = CreateShortAfqn(argument, t, t.EndsWith("[") ? string.Empty : ",");
				}
				t += "]";
			}

			return (t.Length > 254
				? string.Concat("...", t.Substring(t.Length - 250))
				: t).ToLowerInvariant();
		}

		public static string GetNonGenericTypeName(Type type)
		{
			var name = !type.IsGenericType
				? new[] { type.Name }
				: type.Name.Split('`');

			return name[0];
		}
	}
}