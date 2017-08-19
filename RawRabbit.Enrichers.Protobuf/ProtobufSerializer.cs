using System;
using System.IO;
using ProtoBuf;
using RawRabbit.Serialization;

namespace RawRabbit.Enrichers.Protobuf
{
	public class ProtobufSerializer : ISerializer
	{
		public string ContentType => "application/x-protobuf";

		public byte[] Serialize(object obj)
		{
			using (var memoryStream = new MemoryStream())
			{
				Serializer.Serialize(memoryStream, obj);
				return memoryStream.ToArray();
			}
		}

		public object Deserialize(Type type, byte[] bytes)
		{
			using (var memoryStream = new MemoryStream(bytes))
			{
				return Serializer.Deserialize(type, memoryStream);
			}
		}

		public TType Deserialize<TType>(byte[] bytes)
		{
			using (var memoryStream = new MemoryStream(bytes))
			{
				return Serializer.Deserialize<TType>(memoryStream);
			}
		}
	}
}
