using System;
using System.Linq;
using System.Reflection;
using RawRabbit.Serialization;
using ZeroFormatter;

namespace RawRabbit.Enrichers.ZeroFormatter
{
    internal class ZeroFormatterSerializerWorker : ISerializer
    {
        public string ContentType => "application/x-zeroformatter";
        private readonly MethodInfo _deserializeType;
        private readonly MethodInfo _serializeType;

        public ZeroFormatterSerializerWorker()
        {
            _deserializeType = typeof(ZeroFormatterSerializer)
                .GetMethod(nameof(ZeroFormatterSerializer.Deserialize), new[] { typeof(byte[]) });
            _serializeType = typeof(ZeroFormatterSerializer)
                .GetMethods()
                .FirstOrDefault(s => s.Name == nameof(ZeroFormatterSerializer.Serialize) && s.ReturnType == typeof(byte[]));
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
            return ZeroFormatterSerializer.Deserialize<TType>(bytes);
        }
    }
}
