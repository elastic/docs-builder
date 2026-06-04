// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Search.Common;
using Elastic.Internal.Search;
using Elastic.Internal.Search.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Search;

/// <summary>
/// Extension methods for registering search services in the DI container.
/// </summary>
public static class ServicesExtension
{
	/// <summary>
	/// Adds search services to the service collection. Wires the shared
	/// <see cref="ISearchService{TDocument}"/> against the docs index (resolved via
	/// <see cref="ElasticsearchClientAccessor.SearchIndex"/>), then registers the
	/// docs-builder-specific <see cref="INavigationSearchService"/> and
	/// <see cref="IFullSearchService"/> adapters on top.
	/// </summary>
	public static IServiceCollection AddSearchServices(this IServiceCollection services)
	{
		var logger = GetLogger(services);
		logger?.LogInformation("Configuring Search services");

		_ = services.AddSingleton<ElasticsearchClientAccessor>();

		// ProductsConfiguration already implements IProductNameLookup; surface it via the
		// shared interface so both the inner DefaultSearchService (for aggregation buckets) and
		// the FullSearchService adapter (for per-hit display names) can resolve it from DI.
		_ = services.AddScoped<IProductNameLookup>(sp => sp.GetRequiredService<ProductsConfiguration>());

		// Inner search service: docs-builder pairs the typed contract with the docs index alias.
		// docs-builder's SearchConfiguration (in Elastic.Documentation.Configuration.Search) is
		// translated to the contract's shape — both carry synonyms+diminish-terms+ruleset, but
		// the docs-builder type has richer QueryRule POCOs the shared service doesn't need.
		_ = services.AddScoped<ISearchService<DocumentationDocument>>(sp =>
		{
			var acc = sp.GetRequiredService<ElasticsearchClientAccessor>();
			var lookup = sp.GetRequiredService<IProductNameLookup>();
			var innerLogger = sp.GetRequiredService<ILogger<DefaultSearchService<DocumentationDocument>>>();

			var sharedConfig = new Internal.Search.Configuration.SearchConfiguration
			{
				SynonymBiDirectional = acc.SynonymBiDirectional,
				DiminishTerms = acc.DiminishTerms.ToArray(),
				RulesetName = acc.RulesetName,
				SemanticEnabled = true
			};

			return new DefaultSearchService<DocumentationDocument>(
				acc.Client, acc.SearchIndex, sharedConfig, innerLogger, lookup);
		});

		// Docs-specific adapters preserve the existing API/MCP wire format.
		_ = services.AddScoped<INavigationSearchService, NavigationSearchService>();
		_ = services.AddScoped<IFullSearchService, FullSearchService>();
		logger?.LogInformation("Full search services registered with hybrid RRF support");

		// Changes feed (cursor-paginated changes since a given date)
		_ = services.AddSingleton<SharedPointInTimeManager>();
		_ = services.AddScoped<IChangesService, ChangesService>();

		return services;
	}

	private static ILogger? GetLogger(IServiceCollection services)
	{
		using var serviceProvider = services.BuildServiceProvider();
		var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
		return loggerFactory?.CreateLogger(typeof(ServicesExtension));
	}
}
