// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Actions.Core.Extensions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ServiceDefaults.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Elastic.Documentation.ServiceDefaults;

public static class AppDefaultsExtensions
{
	public static TBuilder AddAppDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
	{
		var args = Array.Empty<string>();
		return builder.AddAppDefaults(ref args);
	}
	public static TBuilder AddAppDefaults<TBuilder>(this TBuilder builder, LogLevel defaultLogLevel) where TBuilder : IHostApplicationBuilder
	{
		var args = Array.Empty<string>();
		return builder.AddAppDefaults(ref args, defaultLogLevel);
	}
	public static TBuilder AddAppDefaults<TBuilder>(this TBuilder builder, ref string[] args, Action<IServiceCollection, ConfigurationFileProvider> configure) where TBuilder : IHostApplicationBuilder =>
		builder.AddAppDefaults(ref args, null, configure);

	public static TBuilder AddAppDefaults<TBuilder>(this TBuilder builder, ref string[] args, LogLevel? defaultLogLevel = null, Action<IServiceCollection, ConfigurationFileProvider>? configure = null) where TBuilder : IHostApplicationBuilder
	{
		var logLevel = defaultLogLevel ?? LogLevel.Information;
		ProcessCommandLineArguments(ref args, ref logLevel);

		var services = builder.Services;
		_ = services.AddLogging(x => x
			.ClearProviders()
			.SetMinimumLevel(logLevel).AddConsole(c => c.FormatterName = "condensed")
		);

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
			.SetMinimumLevel(logLevel)
			.AddConsole(c => c.FormatterName = "condensed")
		);

		return builder;
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
