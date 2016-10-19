using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace RawRabbit.Serialization
{
	public interface IHeaderSerializer
	{
		object Serialize(object obj);
		TType Deserialize<TType>(object obj);
	}

	public class HeaderSerializer : IHeaderSerializer
	{
		private readonly JsonSerializer _json;

		public HeaderSerializer(JsonSerializer json)
		{
			_json = json;
		}

		public object Serialize(object obj)
		{
			string objAsJson;
			using (var sw = new StringWriter())
			{
				_json.Serialize(sw, obj);
				objAsJson = sw.GetStringBuilder().ToString();
			}
			var objAsBytes = (object)Encoding.UTF8.GetBytes(objAsJson);
			return objAsBytes;
		}

		public TType Deserialize<TType>(object obj)
		{
			if (obj == null)
			{
				return default(TType);
			}
			var bytes = (byte[])obj;
			var jsonHeader = Encoding.UTF8.GetString(bytes);
			TType target;
			using (var jsonReader = new JsonTextReader(new StringReader(jsonHeader)))
			{
				target = _json.Deserialize<TType>(jsonReader);
			}
			return target;
		}
	}
}
