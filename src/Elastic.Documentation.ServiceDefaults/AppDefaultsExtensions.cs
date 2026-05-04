// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Search;
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
	public static TBuilder AddDocumentationServiceDefaults<TBuilder>(this TBuilder builder)
		where TBuilder : IHostApplicationBuilder => builder.AddDocumentationServiceDefaults(new GlobalCliOptions(), null);

	public static TBuilder AddDocumentationServiceDefaults<TBuilder>(this TBuilder builder, Action<IServiceCollection, ConfigurationFileProvider>? configure)
		where TBuilder : IHostApplicationBuilder => builder.AddDocumentationServiceDefaults(new GlobalCliOptions(), configure);

	public static TBuilder AddDocumentationServiceDefaults<TBuilder>(this TBuilder builder, GlobalCliOptions cliOptions, Action<IServiceCollection, ConfigurationFileProvider>? configure = null)
		where TBuilder : IHostApplicationBuilder
	{
		var services = builder.Services;
		_ = services.AddElasticDocumentationLogging(cliOptions.LogLevel);
		_ = services
			.AddConfigurationFileProvider(cliOptions.SkipPrivateRepositories, cliOptions.ConfigSource, (s, p) =>
			{
				var versionConfiguration = p.CreateVersionConfiguration();
				var products = p.CreateProducts(versionConfiguration);
				var search = p.CreateSearchConfiguration();
				_ = s.AddSingleton(p.CreateLegacyUrlMappings(products));
				_ = s.AddSingleton(products);
				_ = s.AddSingleton(versionConfiguration);
				_ = s.AddSingleton(search);
				configure?.Invoke(s, p);
			});
		_ = services.AddSingleton(cliOptions);

		var endpoints = ElasticsearchEndpointFactory.Create(builder.Configuration);
		_ = services.AddSingleton(endpoints);

		return builder.AddServiceDefaults();
	}

	public static TServiceCollection AddElasticDocumentationLogging<TServiceCollection>(this TServiceCollection services, LogLevel logLevel)
		where TServiceCollection : IServiceCollection
	{
		_ = services.AddLogging(x =>
		{
			_ = x.ClearProviders().SetMinimumLevel(logLevel);
			services.TryAddEnumerable(ServiceDescriptor.Singleton<ConsoleFormatter, CondensedConsoleFormatter>());
			_ = x.AddConsole(c => c.FormatterName = "condensed");
		});
		return services;
	}
}
