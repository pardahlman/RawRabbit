using RawRabbit.Enrichers.Protobuf;
using RawRabbit.Instantiation;
using RawRabbit.Serialization;

namespace RawRabbit
{
	public static class ProtobufPlugin
	{
		/// <summary>
		/// Replaces the default serializer with Protobuf.
		/// </summary>
		/// <param name="builder"></param>
		/// <returns></returns>
		public static IClientBuilder UseProtobuf(this IClientBuilder builder)
		{
			builder.Register(
				pipe: p => {},
				ioc: di => di.AddSingleton<ISerializer, ProtobufSerializer>());
			return builder;
		}
	}
}
