// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
	public static TBuilder AddDocumentationServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
	{
		var args = Array.Empty<string>();
		return builder.AddDocumentationServiceDefaults(ref args);
	}
	public static TBuilder AddDocumentationServiceDefaults<TBuilder>(this TBuilder builder, ref string[] args, Action<IServiceCollection, ConfigurationFileProvider>? configure = null)
		where TBuilder : IHostApplicationBuilder
	{
		GlobalCli.Process(ref args, out var globalArgs);

		var services = builder.Services;
		_ = builder.Services.AddElasticDocumentationLogging(globalArgs.LogLevel, globalArgs.IsMcp);
		_ = services
			.AddConfigurationFileProvider(globalArgs.SkipPrivateRepositories, globalArgs.ConfigurationSource, (s, p) =>
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
		_ = builder.Services.AddElasticDocumentationLogging(globalArgs.LogLevel, globalArgs.IsMcp);
		_ = services.AddSingleton(globalArgs);

		return builder.AddServiceDefaults();
	}

	public static TServiceCollection AddElasticDocumentationLogging<TServiceCollection>(this TServiceCollection services, LogLevel logLevel, bool isMcp = false)
		where TServiceCollection : IServiceCollection
	{
		_ = services.AddLogging(x =>
		{
			_ = x.ClearProviders().SetMinimumLevel(logLevel);
			if (!isMcp)
			{
				services.TryAddEnumerable(ServiceDescriptor.Singleton<ConsoleFormatter, CondensedConsoleFormatter>());
				_ = x.AddConsole(c => c.FormatterName = "condensed");
			}
		});
		return services;
	}

}
