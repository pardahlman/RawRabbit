using System;

namespace RawRabbit.Serialization
{
	public interface ISerializer
	{
		string ContentType { get; }
		byte[] Serialize(object obj);
		object Deserialize(Type type, byte[] bytes);
		TType Deserialize<TType>(byte[] bytes);
	}
}
