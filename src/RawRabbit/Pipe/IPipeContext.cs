using System.Collections.Generic;

namespace RawRabbit.Pipe
{
	public interface IPipeContext
	{
		IDictionary<string, object> Properties { get; }
	}

	public class PipeContext : IPipeContext
	{
		public IDictionary<string, object> Properties { get; set; }
	}

	public static class DictionaryExtensions
	{
		public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
		{
			if (dictionary.ContainsKey(key))
			{
				return false;
			}
			dictionary.Add(key, value);
			return true;
		}

		public static bool AddOrReplace<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
		{
			if (dictionary.ContainsKey(key))
			{
				dictionary.Remove(key);
			}
			return dictionary.TryAdd(key, value);
		}
	}
}