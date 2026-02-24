// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Serialization;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Search;
using Elastic.Transport;

namespace Elastic.Documentation.Search.Common;

/// <summary>
/// Shared singleton accessor for the Elasticsearch client.
/// Both Navigation Search (autocomplete) and FullSearch gateways share this client instance.
/// </summary>
public class ElasticsearchClientAccessor : IDisposable
{
	private readonly ElasticsearchClientSettings _clientSettings;
	private readonly SingleNodePool _nodePool;
	public ElasticsearchClient Client { get; }
	public ElasticsearchEndpoint Endpoint { get; }
	public SearchConfiguration SearchConfiguration { get; }
	public string SearchIndex { get; }
	public string? RulesetName { get; }
	public IReadOnlyDictionary<string, string[]> SynonymBiDirectional { get; }
	public IReadOnlyCollection<string> DiminishTerms { get; }

	public ElasticsearchClientAccessor(
		DocumentationEndpoints endpoints,
		SearchConfiguration searchConfiguration
	)
	{
		var endpoint = endpoints.Elasticsearch;
		Endpoint = endpoint;
		SearchConfiguration = searchConfiguration;
		SynonymBiDirectional = searchConfiguration.SynonymBiDirectional;
		DiminishTerms = searchConfiguration.DiminishTerms;

		SearchIndex = DocumentationMappingContext.DocumentationDocumentSemantic
			.CreateContext(type: "assembler")
			.ResolveReadTarget();

		RulesetName = searchConfiguration.Rules.Count > 0
			? "docs-ruleset-assembler"
			: null;

		_nodePool = new SingleNodePool(endpoint.Uri);
		var auth = endpoint.ApiKey is { } apiKey
			? (AuthorizationHeader)new ApiKey(apiKey)
			: endpoint is { Username: { } username, Password: { } password }
				? new BasicAuthentication(username, password)
				: null!;

		_clientSettings = new ElasticsearchClientSettings(
				_nodePool,
				sourceSerializer: (_, settings) => new DefaultSourceSerializer(settings, EsJsonContext.Default)
			)
			.DefaultIndex(SearchIndex)
			.Authentication(auth);

		Client = new ElasticsearchClient(_clientSettings);
	}

	/// <summary>
	/// Tests connectivity to the Elasticsearch cluster.
	/// </summary>
	public async Task<bool> CanConnect(Cancel ctx) => (await Client.PingAsync(ctx)).IsValidResponse;

	/// <inheritdoc />
	public void Dispose()
	{
		GC.SuppressFinalize(this);
		((IDisposable)_clientSettings).Dispose();
		_nodePool.Dispose();
	}
}
