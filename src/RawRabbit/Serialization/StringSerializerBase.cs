using System;
using System.Text;

namespace RawRabbit.Serialization
{
	public abstract class StringSerializerBase : ISerializer
	{
		public abstract string ContentType { get; }
		public abstract object Deserialize(Type type, string serialized);
		public abstract string SerializeToString(object obj);

		public byte[] Serialize(object obj)
		{
			var serialized = SerializeToString(obj);
			return ConvertToBytes(serialized);
		}

		public object Deserialize(Type type, byte[] bytes)
		{
			if (bytes == null)
			{
				return null;
			}
			var serialized = ConvertToString(bytes);
			return Deserialize(type, serialized);
		}

		public TType Deserialize<TType>(byte[] bytes)
		{
			var serialized = ConvertToString(bytes);
			return (TType)Deserialize(typeof(TType), serialized);
		}

		protected virtual byte[] ConvertToBytes(string serialzed)
		{
			return Encoding.UTF8.GetBytes(serialzed);
		}

		protected virtual string ConvertToString(byte[] bytes)
		{
			return Encoding.UTF8.GetString(bytes);
		}
	}
}
