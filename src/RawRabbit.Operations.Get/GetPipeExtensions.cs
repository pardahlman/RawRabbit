using RabbitMQ.Client;
using RawRabbit.Configuration.Get;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Get
{

	public static class GetPipeExtensions
	{
		public const string GetConfiguration = "GetConfiguration";
		public const string BasicGetResult = "BasicGetResult";

		public static GetConfiguration GetGetConfiguration(this IPipeContext context)
		{
			return context.Get<GetConfiguration>(GetConfiguration);
		}

		public static BasicGetResult GetBasicGetResult(this IPipeContext context)
		{
			return context.Get<BasicGetResult>(BasicGetResult);
		}
	}
}
