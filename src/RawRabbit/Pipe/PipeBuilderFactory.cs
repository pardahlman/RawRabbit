using System;

namespace RawRabbit.Pipe
{
	public interface IPipeBuilderFactory
	{
		IExtendedPipeBuilder Create();
	}

	public class PipeBuilderFactory : IPipeBuilderFactory
	{
		private readonly Func<IExtendedPipeBuilder> _pipeBuilder;

		public PipeBuilderFactory(Func<IExtendedPipeBuilder> pipeBuilder)
		{
			_pipeBuilder = pipeBuilder;
		}

		public IExtendedPipeBuilder Create()
		{
			return _pipeBuilder();
		}
	}
}
