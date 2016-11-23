using RawRabbit.Common;
using Xunit;

namespace RawRabbit.Tests.Common
{
    public class NamingConventionsTests
    {
        [Fact]
        public void Should_Be_Able_To_Get_Application_Name_From_Console_App_Or_Service()
        {
            /* Setup */
            var consoleAppUrl = @"\""Services\\Micro.Services.MagicMaker\\bin\\Micro.Services.MagicMaker.exe\"" ";

            /* Test */
            var consoleAppRes = NamingConventions.GetApplicationName(consoleAppUrl);

            /* Assert */
            Assert.Equal(expected: "micro_services_magicmaker", actual: consoleAppRes);
        }

        [Fact]
        public void Should_Be_Able_To_Get_Application_Name_From_Console_App_Or_Service_With_Vshost()
        {
            /* Setup */
            var consoleAppUrl = @"\""Services\\Micro.Services.MagicMaker\\bin\\Micro.Services.MagicMaker.vshost.exe\"" ";

            /* Test */
            var consoleAppRes = NamingConventions.GetApplicationName(consoleAppUrl);

            /* Assert */
            Assert.Equal(expected: "micro_services_magicmaker", actual: consoleAppRes);
        }

        [Fact]
        public void Should_Be_Able_To_Get_Application_Name_From_IIS_Hosted_App_With_ApplicationPool_Flag()
        {
            /* Setup */
            var iisHostedApp = @"""c:\\windows\\system32\\inetsrv\\w3wp.exe -ap \""Application.Name\"" -v \""v4.0\"" -l \""webengine4.dll\"" -a \\\\.\\pipe\\iisipm6866bb0f-a36a-49b2-9ea8-d83ca69e873d -w \""\"" -m 0 -t 20 -ta 0""";

            /* Test */
            var iisHostedAppRes = NamingConventions.GetApplicationName(iisHostedApp);

            /* Assert */
            Assert.Equal(expected: "application_name", actual: iisHostedAppRes);
        }

        [Fact]
        public void Should_Be_Able_To_Get_Application_Name_From_IIS_Hosted_App_With_Host_Flag()
        {
            /* Setup */
            var iisHostedApp = @"""c:\\windows\\system32\\inetsrv\\w3wp.exe -ap \""Application.Name\"" -v \""v4.0\"" -l \""webengine4.dll\"" -a \\\\.\\pipe\\iisipm6866bb0f-a36a-49b2-9ea8-d83ca69e873d -h \""C:\\inetpub\\temp\\apppools\\voyager_dk\\voyager_dk.config\"" -w \""\"" -m 0 -t 20 -ta 0""";

            /* Test */
            var iisHostedAppRes = NamingConventions.GetApplicationName(iisHostedApp);

            /* Assert */
            Assert.Equal(expected: "application_name", actual: iisHostedAppRes);
        }

        [Fact]
        public void Should_Be_Able_To_Get_Appllication_Name_From_Dot_Net_Core_Hosted_Apps()
        {
            /* Setup */
            var commandLime = new[]
            {
                "C:\\PathToNuget\\packages\\dotnet-test-xunit\\2.2.0-preview2-build1029\\lib\\netcoreapp1.0\\dotnet-test-xunit.dll",
                "C:\\ProjectPath\\RawRabbit\\test\\RawRabbit.IntegrationTests\\bin\\Debug\\netcoreapp1.0\\RawRabbit.IntegrationTests.dll",
                "--designtime",
                "--port",
                "55821",
                "--wait-command"
            };

            /* Test */
            var appName = NamingConventions.GetApplicationName(commandLime);

            /* Assert */
            Assert.Equal("rawrabbit_integrationtests", appName);
        }
    }
}