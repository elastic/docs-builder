// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.ServiceDefaults.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Elastic.Documentation.ServiceDefaults;

public static class AppDefaultsExtensions
{
	public static TBuilder AddDocumentationServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
	{
		var args = Array.Empty<string>();
		return builder.AddDocumentationServiceDefaults(ref args);
	}
	public static TBuilder AddDocumentationServiceDefaults<TBuilder>(this TBuilder builder, ref string[] args, Action<IServiceCollection, ConfigurationFileProvider> configure) where TBuilder : IHostApplicationBuilder =>
		builder.AddDocumentationServiceDefaults(ref args, null, configure);

	public static TBuilder AddDocumentationServiceDefaults<TBuilder>(this TBuilder builder, ref string[] args, LogLevel? defaultLogLevel = null, Action<IServiceCollection, ConfigurationFileProvider>? configure = null) where TBuilder : IHostApplicationBuilder
	{
		var logLevel = defaultLogLevel ?? LogLevel.Information;
		GlobalCommandLine.Process(ref args, ref logLevel, out var skipPrivateRepositories);

		var services = builder.Services;
		_ = services
			.AddConfigurationFileProvider(skipPrivateRepositories, (s, p) =>
			{
				_ = s.AddSingleton(p.CreateVersionConfiguration());
				configure?.Invoke(s, p);
			});
		_ = builder.Services.AddElasticDocumentationLogging(logLevel);

		return builder.AddServiceDefaults();
	}

	public static TServiceCollection AddElasticDocumentationLogging<TServiceCollection>(this TServiceCollection services, LogLevel logLevel)
		where TServiceCollection : IServiceCollection
	{
		services.TryAddEnumerable(ServiceDescriptor.Singleton<ConsoleFormatter, CondensedConsoleFormatter>());
		_ = services.AddLogging(x => x
			.ClearProviders()
			.SetMinimumLevel(logLevel)
			.AddConsole(c => c.FormatterName = "condensed")
		);
		return services;
	}

}
