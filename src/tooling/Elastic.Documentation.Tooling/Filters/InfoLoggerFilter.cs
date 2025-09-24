// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using ConsoleAppFramework;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.ServiceDefaults;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Tooling.Filters;

public class InfoLoggerFilter(
	ConsoleAppFilter next,
	ILogger<InfoLoggerFilter> logger,
	ConfigurationFileProvider fileProvider,
	CliInvocation cliInvocation
)
	: ConsoleAppFilter(next)
{
	public override async Task InvokeAsync(ConsoleAppContext context, Cancel cancellationToken)
	{
		var assemblyVersion = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyInformationalVersionAttribute>()
			.FirstOrDefault()?.InformationalVersion;
		if (cliInvocation.IsHelpOrVersion)
		{
			await Next.InvokeAsync(context, cancellationToken);
			return;
		}
		logger.LogInformation("Configuration source: {ConfigurationSource}", fileProvider.ConfigurationSource.ToStringFast(true));
		if (fileProvider.ConfigurationSource == ConfigurationSource.Checkout)
			logger.LogInformation("Configuration source git reference: {ConfigurationSourceGitReference}", fileProvider.GitReference);
		logger.LogInformation("Version: {Version}", assemblyVersion);
		await Next.InvokeAsync(context, cancellationToken);
	}
}
