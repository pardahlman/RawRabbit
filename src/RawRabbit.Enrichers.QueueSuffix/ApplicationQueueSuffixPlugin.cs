using System;
using System.Linq;
using System.Text.RegularExpressions;
using RawRabbit.Instantiation;

namespace RawRabbit.Enrichers.QueueSuffix
{
	public static  class ApplicationQueueSuffixPlugin
	{
		private const string IisWorkerProcessName = "w3wp";
		private static readonly Regex DllRegex = new Regex(@"(?<ApplicationName>[^\\]*).dll", RegexOptions.Compiled);
		private static readonly Regex ConsoleOrServiceRegex = new Regex(@"(?<ApplicationName>[^\\]*).exe", RegexOptions.Compiled);
		private static readonly Regex IisHostedAppRegexVer1 = new Regex(@"-ap\s\\""(?<ApplicationName>[^\\]+)");
		private static readonly Regex IisHostedAppRegexVer2 = new Regex(@"\\\\apppools\\\\(?<ApplicationName>[^\\]+)");

		public static IClientBuilder UseApplicationQueueSuffix(this IClientBuilder builder)
		{
			var commandLine =  Environment.GetCommandLineArgs();
			var match = ConsoleOrServiceRegex.Match(commandLine.FirstOrDefault() ?? string.Empty);
			var applicationName = string.Empty;

			if (commandLine == null)
			{
				return builder;
			}
			if (match.Success && match.Groups["ApplicationName"].Value != IisWorkerProcessName)
			{
				applicationName = match.Groups["ApplicationName"].Value;
				if (applicationName.EndsWith(".vshost"))
					applicationName = applicationName.Remove(applicationName.Length - ".vshost".Length);
			}
			else
			{
				match = IisHostedAppRegexVer1.Match(commandLine.FirstOrDefault() ?? string.Empty);
				if (match.Success)
				{
					applicationName = match.Groups["ApplicationName"].Value;
				}
				else
				{
					match = IisHostedAppRegexVer2.Match(commandLine.FirstOrDefault() ?? string.Empty);
					if (match.Success)
					{
						applicationName = match.Groups["ApplicationName"].Value;
					}
					else
					{
						var index = commandLine.Length > 1 ? 1 : 0;
						if (DllRegex.IsMatch(commandLine[index]))
						{
							applicationName = DllRegex.Match(commandLine[index]).Groups["ApplicationName"].Value;
						}
					}
				}
			}

			var name =  applicationName.Replace(".", "_").ToLower();
			builder.UseCustomQueueSuffix(new QueueSuffixOptions
			{
				CustomSuffixFunc = context => name,
				ActiveFunc = context => context.GetApplicationSuffixFlag()
			});
			return builder;
		}
	}
}
