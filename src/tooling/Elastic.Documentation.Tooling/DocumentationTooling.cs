// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Actions.Core.Extensions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Tooling.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Elastic.Documentation.Tooling;

public static class DocumentationTooling
{
	public static ServiceProvider CreateServiceProvider(ref string[] args, Action<IServiceCollection, ConfigurationFileProvider>? configure = null)
	{
		var defaultLogLevel = LogLevel.Information;
		ProcessCommandLineArguments(ref args, ref defaultLogLevel);

		var services = new ServiceCollection();
		CreateServiceCollection(services, defaultLogLevel, configure);
		return services.BuildServiceProvider();
	}

	public static void CreateServiceCollection(
		IServiceCollection services,
		LogLevel defaultLogLevel,
		Action<IServiceCollection, ConfigurationFileProvider>? configure = null
	)
	{
		_ = services
			.AddGitHubActionsCore()
			.AddSingleton<DiagnosticsChannel>()
			.AddSingleton<DiagnosticsCollector>()
			.AddSingleton<ConfigurationFileProvider>()
			.AddConfigurationFileProvider((s, p) =>
			{
				_ = s.AddSingleton(p.CreateVersionConfiguration());
				configure?.Invoke(s, p);
			});
		services.TryAddEnumerable(ServiceDescriptor.Singleton<ConsoleFormatter, CondensedConsoleFormatter>());
		_ = services.AddLogging(x => x
			.ClearProviders()
			.SetMinimumLevel(defaultLogLevel)
			.AddConsole(c => c.FormatterName = "condensed")
		);

	}

	private static void ProcessCommandLineArguments(ref string[] args, ref LogLevel defaultLogLevel)
	{
		var newArgs = new List<string>();
		for (var i = 0; i < args.Length; i++)
		{
			if (args[i] == "--log-level")
			{
				if (args.Length > i + 1)
					defaultLogLevel = GetLogLevel(args[i + 1]);

				i++;
			}
			else
				newArgs.Add(args[i]);
		}

		args = [.. newArgs];
	}

	private static LogLevel GetLogLevel(string? logLevel) => logLevel switch
	{
		"trace" => LogLevel.Trace,
		"debug" => LogLevel.Debug,
		"information" => LogLevel.Information,
		"info" => LogLevel.Information,
		"warning" => LogLevel.Warning,
		"error" => LogLevel.Error,
		"critical" => LogLevel.Critical,
		_ => LogLevel.Information
	};
}
