// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.ServiceDefaults.Logging;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Elastic.Documentation.ServiceDefaults;

public static class AppDefaultsExtensions
{
	public static TBuilder AddDocumentationServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder =>
		builder.AddDocumentationServiceDefaults(new GlobalCliOptions(), null);

	public static TBuilder AddDocumentationServiceDefaults<TBuilder>(
		this TBuilder builder,
		Action<IServiceCollection, ConfigurationFileProvider>? configure
	) where TBuilder : IHostApplicationBuilder => builder.AddDocumentationServiceDefaults(new GlobalCliOptions(), configure);

	public static TBuilder AddDocumentationServiceDefaults<TBuilder>(
		this TBuilder builder,
		GlobalCliOptions cliOptions,
		Action<IServiceCollection, ConfigurationFileProvider>? configure = null
	) where TBuilder : IHostApplicationBuilder
	{
		// Map ENVIRONMENT (dev/edge/staging/prod) to the .NET hosting environment so
		// IsDevelopment()/IsStaging()/IsProduction() reflect the real deployment environment.
		// Guarded on non-null so unset (local serve, test factory) leaves the host default untouched.
		var dotnetEnv = DeploymentEnvironment.ToDotnetEnvironment(builder.Configuration["ENVIRONMENT"]);
		if (dotnetEnv is not null)
			builder.Environment.EnvironmentName = dotnetEnv;

		// We do not use appsettings.json — all config comes from env vars / user secrets / code.
		var jsonSources = builder
			.Configuration
			.Sources
			.OfType<JsonConfigurationSource>()
			.Where(s => s.Path is not null && s.Path.StartsWith("appsettings", StringComparison.OrdinalIgnoreCase))
			.ToList();
		foreach (var s in jsonSources)
			_ = builder.Configuration.Sources.Remove(s);

		var services = builder
			.Services
			.AddElasticDocumentationLogging(cliOptions.LogLevel)
			.ConfigureHttpClientDefaults(http =>
			{
				_ = http.AddStandardResilienceHandler(options =>
				{
					options.Retry.DisableForUnsafeHttpMethods();
				});
			})
			.AddConfigurationFileProvider(cliOptions.SkipPrivateRepositories, cliOptions.ConfigSource, (s, p) =>
			{
				// The only place global configuration is loaded: one entry, every file, every error listed at once (ConfigurationException).
				// The warnings it carries are registered as StartupDiagnostics, drained by whoever owns a collector (once per process).
				var result = GlobalConfigurationLoader.LoadOnce(p);
				var global = result.OrThrow();
				var catalog = global.Catalog;
				_ = s.AddSingleton(global);
				_ = s.AddSingleton(GlobalConfigurationLoader.StartupDiagnosticsOf(result));
				_ = s.AddSingleton(global.Assembly);
				_ = s.AddSingleton(catalog);
				_ = s.AddSingleton(catalog.VersioningSystems);
				_ = s.AddSingleton(catalog.Products);
				_ = s.AddSingleton(catalog.LegacyUrlMappings);
				_ = s.AddSingleton(global.Search);
				configure?.Invoke(s, p);
			})
			.AddSingleton(cliOptions);

		var endpoints = ElasticsearchEndpointFactory.Create(builder.Configuration);
		_ = services.AddSingleton(endpoints);

		return builder;
	}

	public static TServiceCollection AddElasticDocumentationLogging<TServiceCollection>(
		this TServiceCollection services,
		LogLevel logLevel
	) where TServiceCollection : IServiceCollection
	{
		_ = services.AddLogging(x =>
		{
			_ = x.ClearProviders().SetMinimumLevel(logLevel);
			services.TryAddEnumerable(ServiceDescriptor.Singleton<ConsoleFormatter, CondensedConsoleFormatter>());
			_ = x.AddConsole(c => c.FormatterName = "condensed");
		});
		return services;
	}

	public static TBuilder HealthCheckBuilderExtensions<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
	{
		_ = builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

		return builder;
	}
}
