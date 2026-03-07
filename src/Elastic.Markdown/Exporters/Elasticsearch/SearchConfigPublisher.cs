// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Diagnostics;
using Elastic.Transport;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Exporters.Elasticsearch;

/// <summary>
/// Publishes synonyms and query rules to Elasticsearch.
/// Shared by <see cref="ElasticsearchMarkdownExporter"/> and the crawl-indexer exporters.
/// </summary>
public class SearchConfigPublisher(
	DistributedTransport transport,
	ILogger logger,
	IDiagnosticsCollector collector
)
{
	private static readonly string[] FixedSynonymIds = ["esql", "data-stream", "data-streams", "machine-learning"];

	private readonly ElasticsearchOperations _operations = new(transport, logger, collector);

	/// <summary>Extracts index-time synonyms that are baked into the index mapping.</summary>
	public static string[] GetIndexTimeSynonyms(IReadOnlyDictionary<string, string[]> synonyms) =>
		synonyms.Aggregate(new List<SynonymRule>(), (acc, synonym) =>
		{
			acc.Add(new SynonymRule { Id = synonym.Key, Synonyms = string.Join(", ", synonym.Value) });
			return acc;
		}).Where(r => FixedSynonymIds.Contains(r.Id)).Select(r => r.Synonyms).ToArray();

	public async Task PublishSynonymsAsync(IReadOnlyDictionary<string, string[]> synonyms, string buildType, string environment, Cancel ctx)
	{
		var setName = $"docs-{buildType}-{environment}";
		logger.LogInformation("Publishing synonym set '{SetName}' to Elasticsearch", setName);

		var synonymRules = synonyms.Aggregate(new List<SynonymRule>(), (acc, synonym) =>
		{
			acc.Add(new SynonymRule { Id = synonym.Key, Synonyms = string.Join(", ", synonym.Value) });
			return acc;
		});

		var synonymsSet = new SynonymsSet { Synonyms = synonymRules };
		var json = JsonSerializer.Serialize(synonymsSet, SynonymSerializerContext.Default.SynonymsSet);

		var response = await _operations.WithRetryAsync(
			() => transport.PutAsync<StringResponse>($"_synonyms/{setName}", PostData.String(json), ctx),
			$"PUT _synonyms/{setName}",
			ctx
		);

		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
			collector.EmitGlobalError(
				$"Failed to publish synonym set '{setName}'. Reason: {response.ApiCallDetails.OriginalException?.Message ?? response.ToString()}");
		else
			logger.LogInformation("Successfully published synonym set '{SetName}'", setName);
	}

	public async Task PublishQueryRulesAsync(IReadOnlyCollection<QueryRule> rules, string buildType, string environment, Cancel ctx)
	{
		if (rules.Count == 0)
		{
			logger.LogInformation("No query rules to publish");
			return;
		}

		var rulesetName = $"docs-ruleset-{buildType}-{environment}";
		logger.LogInformation("Publishing query ruleset '{RulesetName}' with {Count} rules to Elasticsearch", rulesetName, rules.Count);

		var rulesetRules = rules.Select(r => new QueryRulesetRule
		{
			RuleId = r.RuleId,
			Type = r.Type.ToString().ToLowerInvariant(),
			Criteria = r.Criteria.Select(c => new QueryRulesetCriteria
			{
				Type = c.Type.ToString().ToLowerInvariant(),
				Metadata = c.Metadata,
				Values = c.Values.ToList()
			}).ToList(),
			Actions = new QueryRulesetActions { Ids = r.Actions.Ids.ToList() }
		}).ToList();

		var ruleset = new QueryRuleset { Rules = rulesetRules };
		var json = JsonSerializer.Serialize(ruleset, QueryRulesetSerializerContext.Default.QueryRuleset);

		var response = await _operations.WithRetryAsync(
			() => transport.PutAsync<StringResponse>($"_query_rules/{rulesetName}", PostData.String(json), ctx),
			$"PUT _query_rules/{rulesetName}",
			ctx
		);

		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
			collector.EmitGlobalError(
				$"Failed to publish query ruleset '{rulesetName}'. Reason: {response.ApiCallDetails.OriginalException?.Message ?? response.ToString()}");
		else
			logger.LogInformation("Successfully published query ruleset '{RulesetName}'", rulesetName);
	}
}

public sealed record SynonymsSet
{
	[JsonPropertyName("synonyms_set")]
	public required List<SynonymRule> Synonyms { get; init; } = [];
}

public sealed record SynonymRule
{
	public required string Id { get; init; }
	public required string Synonyms { get; init; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(SynonymsSet))]
[JsonSerializable(typeof(SynonymRule))]
public sealed partial class SynonymSerializerContext : JsonSerializerContext;

public sealed record QueryRuleset
{
	[JsonPropertyName("rules")]
	public required List<QueryRulesetRule> Rules { get; init; } = [];
}

public sealed record QueryRulesetRule
{
	[JsonPropertyName("rule_id")]
	public required string RuleId { get; init; }

	[JsonPropertyName("type")]
	public required string Type { get; init; }

	[JsonPropertyName("criteria")]
	public required List<QueryRulesetCriteria> Criteria { get; init; } = [];

	[JsonPropertyName("actions")]
	public required QueryRulesetActions Actions { get; init; }
}

public sealed record QueryRulesetCriteria
{
	[JsonPropertyName("type")]
	public required string Type { get; init; }

	[JsonPropertyName("metadata")]
	public required string Metadata { get; init; }

	[JsonPropertyName("values")]
	public required List<string> Values { get; init; } = [];
}

public sealed record QueryRulesetActions
{
	[JsonPropertyName("ids")]
	public required List<string> Ids { get; init; } = [];
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(QueryRuleset))]
[JsonSerializable(typeof(QueryRulesetRule))]
[JsonSerializable(typeof(QueryRulesetCriteria))]
[JsonSerializable(typeof(QueryRulesetActions))]
public sealed partial class QueryRulesetSerializerContext : JsonSerializerContext;
