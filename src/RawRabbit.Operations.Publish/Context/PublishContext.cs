using RawRabbit.Pipe;

namespace RawRabbit.Operations.Publish.Context
{
	public interface IPublishContext : IPipeContext { }

	public class PublishContext : PipeContext, IPublishContext
	{
		public PublishContext(IPipeContext context)
		{
			Properties = context.Properties;
		}
	}
}
