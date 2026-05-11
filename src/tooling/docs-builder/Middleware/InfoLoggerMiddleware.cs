// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using Elastic.Documentation.Configuration;
using Microsoft.Extensions.Logging;
using Nullean.Argh.Middleware;

namespace Documentation.Builder.Middleware;

internal sealed class InfoLoggerMiddleware(ILogger<InfoLoggerMiddleware> logger, ConfigurationFileProvider fileProvider)
	: ICommandMiddleware
{
	public async ValueTask InvokeAsync(CommandContext context, CommandMiddlewareDelegate next)
	{
		var assemblyVersion = Assembly.GetExecutingAssembly()
			.GetCustomAttributes<AssemblyInformationalVersionAttribute>()
			.FirstOrDefault()?.InformationalVersion;

		logger.LogInformation("Configuration source: {ConfigurationSource}", fileProvider.ConfigurationSource);
		if (fileProvider.ConfigurationSource == Elastic.Documentation.ConfigurationSource.Remote)
			logger.LogInformation("Configuration source git reference: {ConfigurationSourceGitReference}", fileProvider.GitReference);
		logger.LogInformation("Version: {Version}", assemblyVersion);

		await next(context);
	}
}
