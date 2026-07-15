// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Transport;
using Microsoft.Extensions.Logging;

namespace Elastic.SiteSearch.Cli.Elasticsearch;

/// <summary>
/// Validates that the prerequisite Elasticsearch resources published by docs-builder exist
/// on the cluster before essc bootstraps its indices. Fails fast with an actionable message
/// if they are absent — the caller should treat this as a hard stop.
/// </summary>
internal sealed class SearchResourceValidator(DistributedTransport transport, ILogger logger)
{
	public async Task ValidateAsync(string environment, CancellationToken ct = default)
	{
		await ValidateSynonymSetAsync(SearchResourceNames.SynonymSet(environment), ct);
		await ValidateQueryRulesetAsync(SearchResourceNames.QueryRuleset(environment), ct);
	}

	private async Task ValidateSynonymSetAsync(string setName, CancellationToken ct)
	{
		var response = await transport.GetAsync<StringResponse>($"_synonyms/{setName}", cancellationToken: ct);
		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
		{
			throw new InvalidOperationException(
				$"Synonym set '{setName}' not found on {transport}. " +
				$"Run docs-builder indexing for this environment first to publish the required synonym set.");
		}
		logger.LogInformation("Synonym set '{SetName}' validated", setName);
	}

	private async Task ValidateQueryRulesetAsync(string rulesetName, CancellationToken ct)
	{
		var response = await transport.GetAsync<StringResponse>($"_query_rules/{rulesetName}", cancellationToken: ct);
		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
		{
			if (response.ApiCallDetails.HttpStatusCode == 404)
			{
				// Query ruleset may be absent in environments that don't use query rules —
				// log a warning rather than hard-failing.
				logger.LogWarning(
					"Query ruleset '{RulesetName}' not found — query rules will not apply. " +
					"Run docs-builder indexing for this environment to publish query rules.",
					rulesetName);
				return;
			}

			throw new InvalidOperationException(
				$"Failed to check query ruleset '{rulesetName}': {response.ApiCallDetails.OriginalException?.Message ?? response.ToString()}");
		}
		logger.LogInformation("Query ruleset '{RulesetName}' validated", rulesetName);
	}
}
