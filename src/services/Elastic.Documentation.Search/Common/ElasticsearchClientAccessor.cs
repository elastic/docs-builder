// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Serialization;
using Elastic.Documentation.Configuration.Search;
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
	public ElasticsearchOptions Options { get; }
	public SearchConfiguration SearchConfiguration { get; }
	public string? RulesetName { get; }
	public IReadOnlyDictionary<string, string[]> SynonymBiDirectional { get; }
	public IReadOnlyCollection<string> DiminishTerms { get; }

	public ElasticsearchClientAccessor(
		ElasticsearchOptions elasticsearchOptions,
		SearchConfiguration searchConfiguration)
	{
		Options = elasticsearchOptions;
		SearchConfiguration = searchConfiguration;
		SynonymBiDirectional = searchConfiguration.SynonymBiDirectional;
		DiminishTerms = searchConfiguration.DiminishTerms;
		RulesetName = searchConfiguration.Rules.Count > 0
			? ExtractRulesetName(elasticsearchOptions.IndexName)
			: null;

		_nodePool = new SingleNodePool(new Uri(elasticsearchOptions.Url.Trim()));
		_clientSettings = new ElasticsearchClientSettings(
				_nodePool,
				sourceSerializer: (_, settings) => new DefaultSourceSerializer(settings, EsJsonContext.Default)
			)
			.DefaultIndex(elasticsearchOptions.IndexName)
			.Authentication(new ApiKey(elasticsearchOptions.ApiKey));

		Client = new ElasticsearchClient(_clientSettings);
	}

	/// <summary>
	/// Extracts the ruleset name from the index name.
	/// Index name format: "semantic-docs-{namespace}-latest" -> ruleset: "docs-ruleset-{namespace}"
	/// The namespace may contain hyphens (e.g., "codex-internal"), so we extract everything
	/// between the "semantic-docs-" prefix and the "-latest" suffix.
	/// </summary>
	private static string? ExtractRulesetName(string indexName)
	{
		const string prefix = "semantic-docs-";
		const string suffix = "-latest";
		if (!indexName.StartsWith(prefix, StringComparison.Ordinal) || !indexName.EndsWith(suffix, StringComparison.Ordinal))
			return null;

		var ns = indexName[prefix.Length..^suffix.Length];
		return string.IsNullOrEmpty(ns) ? null : $"docs-ruleset-{ns}";
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
