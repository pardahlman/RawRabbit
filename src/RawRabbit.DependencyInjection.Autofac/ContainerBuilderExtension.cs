using Autofac;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Context;

namespace RawRabbit.DependencyInjection.Autofac
{
    public static class ContainerBuilderExtension
    {
        public static ContainerBuilder RegisterRawRabbit<TMessageContext>(this ContainerBuilder builder) where TMessageContext : IMessageContext
        {
            builder.RegisterModule<RawRabbitModule<TMessageContext>>();
            return builder;
        }

        public static ContainerBuilder RegisterRawRabbit(this ContainerBuilder builder)
        {
            builder.RegisterModule<RawRabbitModule>();
            return builder;
        }

        public static ContainerBuilder RegisterRawRabbit(this ContainerBuilder builder, string connectionString)
        {
            return RegisterRawRabbit<MessageContext>(builder, connectionString);
        }

        public static ContainerBuilder RegisterRawRabbit<TMessageContext>(this ContainerBuilder builder, string connectionString)
            where TMessageContext : IMessageContext
        {
            var config = ConnectionStringParser.Parse(connectionString);
            return RegisterRawRabbit<TMessageContext>(builder, config);
        }

        public static ContainerBuilder RegisterRawRabbit(this ContainerBuilder builder, RawRabbitConfiguration configuration)
        {
            return RegisterRawRabbit<MessageContext>(builder, configuration);
        }

        public static ContainerBuilder RegisterRawRabbit<TMessageContext>(this ContainerBuilder builder, RawRabbitConfiguration configuration)
            where TMessageContext : IMessageContext
        {
            builder
                .Register(c => configuration)
                .SingleInstance();

            builder.RegisterModule<RawRabbitModule<TMessageContext>>();
            return builder;
        }
    }

}
