// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Api.Core.Search;
using Elastic.Documentation.Search.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Search;

/// <summary>
/// Extension methods for registering search services in the DI container.
/// </summary>
public static class ServicesExtension
{
	/// <summary>
	/// Adds search services to the service collection.
	/// Includes Elasticsearch options, client accessor, and search gateways.
	/// </summary>
	/// <param name="services">The service collection to add services to.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddSearchServices(this IServiceCollection services)
	{
		var logger = GetLogger(services);
		logger?.LogInformation("Configuring Search services");

		_ = services.AddSingleton<ElasticsearchClientAccessor>();

		// Navigation Search (autocomplete/navigation search)
		_ = services.AddScoped<INavigationSearchGateway, NavigationSearchGateway>();
		_ = services.AddScoped<NavigationSearchUsecase>();

		// FullSearch (full-page search with hybrid RRF)
		_ = services.AddScoped<IFullSearchGateway, FullSearchGateway>();
		_ = services.AddScoped<FullSearchUsecase>();
		logger?.LogInformation("Full search use case registered with hybrid RRF support");

		return services;
	}

	private static ILogger? GetLogger(IServiceCollection services)
	{
		using var serviceProvider = services.BuildServiceProvider();
		var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
		return loggerFactory?.CreateLogger(typeof(ServicesExtension));
	}
}
