using System;
using System.IO;
using System.Runtime.Serialization.Formatters;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace RawRabbit.Serialization
{
	public class JsonMessageSerializer : IMessageSerializer
	{
		private readonly JsonSerializer _converter;

		public JsonMessageSerializer(Action<JsonSerializer> config = null)
		{
			_converter = new JsonSerializer
			{
				ContractResolver = new CamelCasePropertyNamesContractResolver(),
				ObjectCreationHandling = ObjectCreationHandling.Auto,
				TypeNameHandling = TypeNameHandling.Objects,
				TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple
			};
			config?.Invoke(_converter);
		}

		public byte[] Serialize<T>(T obj)
		{
			if (obj == null)
			{
				return Encoding.UTF8.GetBytes(string.Empty);
			}
			string msgStr;
			using (var sw = new StringWriter())
			{
				_converter.Serialize(sw, obj);
				msgStr = sw.GetStringBuilder().ToString();
			}
			var msgBytes = Encoding.UTF8.GetBytes(msgStr);
			return msgBytes;
		}

		public T Deserialize<T>(byte[] bytes)
		{
			var obj = (T)Deserialize(bytes, typeof(T));
			return obj;
		}

		public object Deserialize(byte[] bytes, Type messageType)
		{
			object obj;
			var msgStr = Encoding.UTF8.GetString(bytes);
			using (var jsonReader = new JsonTextReader(new StringReader(msgStr)))
			{
				obj = _converter.Deserialize(jsonReader, messageType);
			}
			return obj;
		}
	}
}
