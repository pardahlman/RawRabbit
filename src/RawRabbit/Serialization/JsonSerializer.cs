using System;
using System.IO;
using Newtonsoft.Json;

namespace RawRabbit.Serialization
{
	public interface ISerializer
	{
		string Serialize(object obj);
		object Deserialize(Type type, string str);
		TType Deserialize<TType>(string str);
	}

	public class JsonSerializer : ISerializer
	{
		private readonly Newtonsoft.Json.JsonSerializer _json;

		public JsonSerializer(Newtonsoft.Json.JsonSerializer json)
		{
			_json = json;
		}

		public string Serialize(object obj)
		{
			string serialized;
			using (var sw = new StringWriter())
			{
				_json.Serialize(sw, obj);
				serialized = sw.GetStringBuilder().ToString();
			}
			return serialized;
		}

		public object Deserialize(Type type, string str)
		{
			object obj;
			using (var jsonReader = new JsonTextReader(new StringReader(str)))
			{
				obj = _json.Deserialize(jsonReader, type);
			}
			return obj;
		}

		public TType Deserialize<TType>(string str)
		{
			return (TType)Deserialize(typeof(TType), str);
		}
	}
}
