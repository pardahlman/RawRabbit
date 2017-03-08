using System;
using System.IO;
using Newtonsoft.Json;

namespace RawRabbit.Serialization
{
	public class JsonSerializer : ISerializer
	{
		private readonly Newtonsoft.Json.JsonSerializer _json;
		public string ContentType => "application/json";

		public JsonSerializer(Newtonsoft.Json.JsonSerializer json)
		{
			_json = json;
		}

		public string Serialize(object obj)
		{
			if (obj == null)
			{
				return string.Empty;
			}
			if (obj is string)
			{
				return obj as string;
			}
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
			if (type == typeof(string))
			{
				return str;
			}
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
