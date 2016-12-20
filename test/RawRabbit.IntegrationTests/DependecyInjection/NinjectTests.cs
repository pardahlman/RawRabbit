using System.Threading.Tasks;
using Ninject;
using RawRabbit.DependencyInjection.Ninject;
using RawRabbit.Instantiation;
using Xunit;

namespace RawRabbit.IntegrationTests.DependecyInjection
{
	public class NinjectTests
	{
		[Fact]
		public async Task Should_Be_Able_To_Resolve_Client_From_Ninject()
		{
			/* Setup */
			var kernel = new StandardKernel();
			kernel.RegisterRawRabbit();
			
			/* Test */
			var client = kernel.Get<IBusClient>();
			var instanceFactory = kernel.Get<IInstanceFactory>();

			/* Assert */
			(instanceFactory as InstanceFactory)?.Dispose();
			Assert.NotNull(client);
		}
	}
}
