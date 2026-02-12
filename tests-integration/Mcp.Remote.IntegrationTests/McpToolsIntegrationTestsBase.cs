// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Api.Core.Search;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Mcp.Remote.Gateways;
using Elastic.Documentation.Mcp.Remote.Tools;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mcp.Remote.IntegrationTests;

/// <summary>
/// Base class for MCP Lambda integration tests providing shared configuration and factory methods.
/// </summary>
public abstract class McpToolsIntegrationTestsBase(ITestOutputHelper output)
{
	protected ITestOutputHelper Output { get; } = output;

	/// <summary>
	/// Creates SearchTools with all required dependencies.
	/// </summary>
	protected (SearchTools? Tools, ElasticsearchClientAccessor? ClientAccessor) CreateSearchTools()
	{
		var clientAccessor = CreateElasticsearchClientAccessor();
		if (clientAccessor == null)
			return (null, null);

		var productsConfig = CreateProductsConfiguration();
		var fullSearchGateway = new FullSearchGateway(
			clientAccessor,
			productsConfig,
			NullLogger<FullSearchGateway>.Instance);

		var searchTools = new SearchTools(fullSearchGateway);
		return (searchTools, clientAccessor);
	}

	/// <summary>
	/// Creates DocumentTools with all required dependencies.
	/// </summary>
	protected (DocumentTools? Tools, ElasticsearchClientAccessor? ClientAccessor) CreateDocumentTools()
	{
		var clientAccessor = CreateElasticsearchClientAccessor();
		if (clientAccessor == null)
			return (null, null);

		var documentGateway = new DocumentGateway(
			clientAccessor,
			NullLogger<DocumentGateway>.Instance);

		var documentTools = new DocumentTools(documentGateway);
		return (documentTools, clientAccessor);
	}

	/// <summary>
	/// Creates CoherenceTools with all required dependencies.
	/// </summary>
	protected (CoherenceTools? Tools, ElasticsearchClientAccessor? ClientAccessor) CreateCoherenceTools()
	{
		var clientAccessor = CreateElasticsearchClientAccessor();
		if (clientAccessor == null)
			return (null, null);

		var productsConfig = CreateProductsConfiguration();
		var fullSearchGateway = new FullSearchGateway(
			clientAccessor,
			productsConfig,
			NullLogger<FullSearchGateway>.Instance);

		var coherenceTools = new CoherenceTools(fullSearchGateway);
		return (coherenceTools, clientAccessor);
	}

	/// <summary>
	/// Creates an ElasticsearchClientAccessor using configuration from user secrets and environment variables.
	/// </summary>
	private static ElasticsearchClientAccessor? CreateElasticsearchClientAccessor()
	{
		var configBuilder = new ConfigurationBuilder();
		configBuilder.AddUserSecrets("72f50f33-6fb9-4d08-bff3-39568fe370b3");
		configBuilder.AddEnvironmentVariables();
		var config = configBuilder.Build();

		var elasticsearchUrl =
			config["Parameters:DocumentationElasticUrl"]
			?? config["DOCUMENTATION_ELASTIC_URL"];

		var elasticsearchApiKey =
			config["Parameters:DocumentationElasticApiKey"]
			?? config["DOCUMENTATION_ELASTIC_APIKEY"];

		if (string.IsNullOrEmpty(elasticsearchUrl) || string.IsNullOrEmpty(elasticsearchApiKey))
			return null;

		var testConfig = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["DOCUMENTATION_ELASTIC_URL"] = elasticsearchUrl,
				["DOCUMENTATION_ELASTIC_APIKEY"] = elasticsearchApiKey,
				["DOCUMENTATION_ELASTIC_INDEX"] = "semantic-docs-dev-latest"
			})
			.Build();

		var options = new ElasticsearchOptions(testConfig);
		var searchConfig = new SearchConfiguration
		{
			Synonyms = new Dictionary<string, string[]>(),
			Rules = [],
			DiminishTerms = ["plugin", "client", "integration", "glossary"]
		};

		return new ElasticsearchClientAccessor(options, searchConfig);
	}

	/// <summary>
	/// Creates a minimal ProductsConfiguration for testing.
	/// </summary>
	private static ProductsConfiguration CreateProductsConfiguration()
	{
		var products = new Dictionary<string, Product>
		{
			["elasticsearch"] = new() { Id = "elasticsearch", DisplayName = "Elasticsearch" },
			["kibana"] = new() { Id = "kibana", DisplayName = "Kibana" },
			["logstash"] = new() { Id = "logstash", DisplayName = "Logstash" },
			["beats"] = new() { Id = "beats", DisplayName = "Beats" },
			["cloud"] = new() { Id = "cloud", DisplayName = "Elastic Cloud" },
			["fleet"] = new() { Id = "fleet", DisplayName = "Fleet" },
			["apm"] = new() { Id = "apm", DisplayName = "APM" }
		};

		var productDisplayNames = products.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.DisplayName);

		return new ProductsConfiguration
		{
			Products = products.ToFrozenDictionary(),
			ProductDisplayNames = productDisplayNames.ToFrozenDictionary()
		};
	}
}
