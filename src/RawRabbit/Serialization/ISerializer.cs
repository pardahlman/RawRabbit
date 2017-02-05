using System;

namespace RawRabbit.Serialization
{
	public interface ISerializer
	{
		string ContentType { get; }
		string Serialize(object obj);
		object Deserialize(Type type, string str);
		TType Deserialize<TType>(string str);
	}
}