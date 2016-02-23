using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RawRabbit.Common
{
	public interface INamingConventions
	{
		Func<Type, string> ExchangeNamingConvention { get; set; }
		Func<Type, string> QueueNamingConvention { get; set; }
		Func<Type, Type, string> RpcExchangeNamingConvention { get; set; }
		Func<string> ErrorExchangeNamingConvention { get; set; }
		Func<string> ErrorQueueNamingConvention { get; set; }
		Func<string> DeadLetterExchangeNamingConvention { get; set; }
		Func<string> RetryQueueNamingConvention { get; set; }
		Func<Type, string> SubscriberQueueSuffix { get; set; }
	}

	public class NamingConventions : INamingConventions
	{
		private readonly IEnumerable<string> _disallowedDirectoryNames = new[] {"bin", "debug", "release"};
		private readonly Dictionary<Type, int> _subscriberCounter;
		private readonly string _applicationName = GetApplicationNameFromUrl(Directory.GetCurrentDirectory());

		public virtual Func<Type, string> ExchangeNamingConvention { get; set; }
		public virtual Func<Type, string> QueueNamingConvention { get; set; }
		public virtual Func<Type, Type, string> RpcExchangeNamingConvention { get; set; }
		public virtual Func<string> ErrorExchangeNamingConvention { get; set; }
		public virtual Func<string> ErrorQueueNamingConvention { get; set; }
		public virtual Func<string> DeadLetterExchangeNamingConvention { get; set; }
		public virtual Func<string> RetryQueueNamingConvention { get; set; }
		public virtual Func<Type, string> SubscriberQueueSuffix { get; set; }

		public NamingConventions()
		{
			_subscriberCounter = new Dictionary<Type,int>();
			
			ExchangeNamingConvention = type => type?.Namespace?.ToLower() ?? string.Empty;
			RpcExchangeNamingConvention = (request, response) => request?.Namespace?.ToLower() ?? "default_rpc_exchange";
			QueueNamingConvention = type => CreateShortAfqn(type);
			ErrorQueueNamingConvention = () => "default_error_queue";
			ErrorExchangeNamingConvention = () => "default_error_exchange";
			DeadLetterExchangeNamingConvention = () => "default_dead_letter_exchange";
			RetryQueueNamingConvention = () => $"retry_{Guid.NewGuid()}";
			SubscriberQueueSuffix = GetSubscriberQueueSuffix;
		}

		private string GetSubscriberQueueSuffix(Type messageType)
		{
			if (!_subscriberCounter.ContainsKey(messageType))
			{
				_subscriberCounter.Add(messageType,0);
			}
			var subscriberIndex = ++_subscriberCounter[messageType];
			_subscriberCounter[messageType] = subscriberIndex;

			var sb = new StringBuilder(_applicationName);
			if (subscriberIndex > 1)
			{
				sb.Append($"_{subscriberIndex}");
			}

			return sb.ToString();
		}

		public static string GetApplicationNameFromUrl(string url)
		{
			var consoleOrIisHostedAppRegex = new Regex(@".*\\(?<ApplicationName>.*[a-zA-Z][^\\]*)(\\[0-9.]*)?\\bin.*");
			var match = consoleOrIisHostedAppRegex.Match(url);
			var applicationName = string.Empty;

			if (match.Success)
			{
				applicationName = match.Groups["ApplicationName"].Value;
			}
			else
			{
				var windowsServiceRegex = new Regex(@".*\\(?<ApplicationName>.*[a-zA-Z][^\\]*)(\\[0-9.]*\\)?");
				match = windowsServiceRegex.Match(url);
				if (match.Success)
				{
					applicationName = match.Groups["ApplicationName"].Value;
				}
			}
			
			return applicationName;
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