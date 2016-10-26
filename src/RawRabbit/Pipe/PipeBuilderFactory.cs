using System;

namespace RawRabbit.Pipe
{
	public interface IPipeBuilderFactory
	{
		IExtendedPipeBuilder Create();
		Middleware.Middleware Create(Action<IPipeBuilder> pipe);
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

		public Middleware.Middleware Create(Action<IPipeBuilder> pipe)
		{
			var builder = _pipeBuilder();
			pipe(builder);
			return builder.Build();
		}
	}
}
