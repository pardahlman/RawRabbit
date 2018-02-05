using System;
using System.Linq;
using System.Reflection;
using MessagePack;
using RawRabbit.Serialization;

namespace RawRabbit.Enrichers.MessagePack
{
    public class MessagePackSerializerWorker : ISerializer
    {
        public string ContentType => "application/x-messagepack";
        private readonly MethodInfo _deserializeType;
        private readonly MethodInfo _serializeType;

        public MessagePackSerializerWorker()
        {
			_deserializeType = typeof(MessagePackSerializer)
				.GetMethod(nameof(MessagePackSerializer.Deserialize), new[] { typeof(byte[]) });
			_serializeType = typeof(MessagePackSerializer)
				.GetMethods()
				.FirstOrDefault(s => s.Name == nameof(MessagePackSerializer.Serialize) && s.ReturnType == typeof(byte[]));
		}

        public byte[] Serialize(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException();

            return (byte[])_serializeType
                .MakeGenericMethod(obj.GetType())
                .Invoke(null, new[] { obj });
        }

        public object Deserialize(Type type, byte[] bytes)
        {
            return _deserializeType.MakeGenericMethod(type)
                .Invoke(null, new object[] { bytes });
        }

        public TType Deserialize<TType>(byte[] bytes)
        {
            return MessagePackSerializer.Deserialize<TType>(bytes);
        }
    }
}