using System;
using System.IO;
using RawRabbit.Common;
using Xunit;

namespace RawRabbit.Tests.Common
{
	public class NamingConventionsTests
	{
		[Fact]
		public void Should_Be_Able_To_Get_Application_Name_From_Console_App()
		{
			/* Setup */
			var consoleAppUrl = "C:\\Projects\\Temp\\My.Console.App\\bin\\Debug\\RawException.vshost.exe";

			/* Test */
			var consoleAppRes = NamingConventions.GetApplicationNameFromUrl(consoleAppUrl);

			/* Assert */
			Assert.Equal("My.Console.App", consoleAppRes);
		}
		[Fact]
		public void Should_Be_Able_To_Get_Application_Name_From_Console_App_With_Version_Number()
		{
			/* Setup */
			var consoleAppUrl = "C:\\Projects\\Temp\\My.Console.App\\1.2.3.456\\bin\\Debug\\RawException.vshost.exe";

			/* Test */
			var consoleAppRes = NamingConventions.GetApplicationNameFromUrl(consoleAppUrl);

			/* Assert */
			Assert.Equal("My.Console.App", consoleAppRes);
		}

		[Fact]
		public void Should_Be_Able_To_Get_Application_Name_From_IIS_Hosted_App()
		{
			/* Setup */
			var iisHostedApp = "C:\\Projects\\Temp\\My.Application\\My.Application.Web\\bin";

			/* Test */
			var iisHostedAppRes = NamingConventions.GetApplicationNameFromUrl(iisHostedApp);

			/* Assert */
			Assert.Equal("My.Application.Web", iisHostedAppRes);
		}

		[Fact]
		public void Should_Be_Able_To_Get_Application_Name_From_IIS_Hosted_App_With_Version_Number()
		{
			/* Setup */
			var iisHostedApp = "C:\\Projects\\Temp\\My.Application\\My.Application.Web\\1.2.3.456\\bin";

			/* Test */
			var iisHostedAppRes = NamingConventions.GetApplicationNameFromUrl(iisHostedApp);

			/* Assert */
			Assert.Equal("My.Application.Web", iisHostedAppRes);
		}

		[Fact]
		public void Should_Be_Able_To_Get_Application_Name_From_Service()
		{
			/* Setup */
			var windowsServiceDeployedWithVersionNumber = "C:\\Application\\PRODUCTION\\My.Windows.Service\\";

			/* Test */
			var windowsServiceDeployedWithVersionNumberRes = NamingConventions.GetApplicationNameFromUrl(windowsServiceDeployedWithVersionNumber);

			/* Assert */
			Assert.Equal("My.Windows.Service", windowsServiceDeployedWithVersionNumberRes);
		}

		[Fact]
		public void Should_Be_Able_To_Get_Application_Name_From_Service_With_Version_Number()
		{
			/* Setup */
			var windowsServiceDeployedWithVersionNumber = "C:\\Application\\PRODUCTION\\My.Windows.Service\\1.2.3.456\\";

			/* Test */
			var windowsServiceDeployedWithVersionNumberRes = NamingConventions.GetApplicationNameFromUrl(windowsServiceDeployedWithVersionNumber);

			/* Assert */
			Assert.Equal("My.Windows.Service", windowsServiceDeployedWithVersionNumberRes);
		}
	}
}