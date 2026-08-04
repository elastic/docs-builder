// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Elastic.Transport;
using Microsoft.Extensions.Logging;

namespace Elastic.SiteSearch.Cli.Elasticsearch;

/// <summary>
/// Copies the docs-builder-published Elasticsearch resources required by essc
/// (<c>docs-assembler-{env}</c> synonym set and <c>docs-ruleset-assembler-{env}</c>
/// query ruleset) from a source cluster to a destination cluster.
/// </summary>
/// <remarks>
/// <para>
/// Both resources are cluster-level objects (<c>_synonyms</c>, <c>_query_rules</c>) that
/// remote reindex does not carry across. Calling <see cref="CopyAsync"/> before starting a
/// cross-cluster reindex ensures the destination passes <see cref="SearchResourceValidator"/>
/// validation and that any analyzer referencing the synonym set resolves correctly.
/// </para>
/// <para>
/// Each resource is copied under the <em>same name</em> on the destination — the synced
/// index's settings reference the source-environment synonym-set name verbatim (because
/// <c>BootstrapSemanticIndexAsync</c> copies source settings as-is), so no name translation
/// is needed.
/// </para>
/// <para>
/// A no-op check is performed first: if the destination already holds an identical resource
/// the PUT is skipped. Synonym-set entries are normalised by <c>id</c> before comparing;
/// query-rule entries are compared in-order.
/// </para>
/// </remarks>
internal sealed partial class SearchResourceSynchronizer(
	DistributedTransport source,
	DistributedTransport destination,
	ILogger logger)
{
	/// <summary>
	/// Tries to extract the environment token from a known alias name.
	/// </summary>
	/// <remarks>
	/// Handles:
	/// <list type="bullet">
	///   <item><c>&lt;source&gt;.(lexical|semantic)-{env}-latest</c> — standard write-alias form</item>
	///   <item><c>ws-content-{env}</c> — search-facing page alias</item>
	/// </list>
	/// </remarks>
	/// <param name="alias">An alias name such as <c>website-search.semantic-prod-latest</c> or <c>ws-content-staging</c>.</param>
	/// <param name="environment">The extracted environment token, e.g. <c>prod</c>.</param>
	/// <returns><c>true</c> when the environment was successfully derived; <c>false</c> otherwise.</returns>
	public static bool TryDeriveEnvironment(string alias, out string environment)
	{
		// Matches: <anything>.(lexical|semantic)-<env>-latest
		var m = AliasEnvRegex().Match(alias);
		if (m.Success)
		{
			environment = m.Groups["env"].Value;
			return true;
		}

		// Matches: ws-content-<env>
		m = PageAliasEnvRegex().Match(alias);
		if (m.Success)
		{
			environment = m.Groups["env"].Value;
			return true;
		}

		environment = string.Empty;
		return false;
	}

	/// <summary>
	/// Copies the synonym set and query ruleset for the given environment from the source
	/// cluster to the destination cluster.
	/// </summary>
	/// <exception cref="InvalidOperationException">
	/// Thrown when a resource is missing on the source, the source GET fails for a non-404
	/// reason, or the destination PUT fails.
	/// </exception>
	public async Task CopyAsync(string environment, CancellationToken ct = default)
	{
		await CopySynonymSetAsync(SearchResourceNames.SynonymSet(environment), ct);
		await CopyQueryRulesetAsync(SearchResourceNames.QueryRuleset(environment), ct);
	}

	private async Task CopySynonymSetAsync(string name, CancellationToken ct)
	{
		// ── Fetch from source ──────────────────────────────────────────────────────
		var srcResp = await source.GetAsync<StringResponse>($"_synonyms/{name}", cancellationToken: ct);
		if (!srcResp.ApiCallDetails.HasSuccessfulStatusCode)
		{
			throw new InvalidOperationException(
				$"Synonym set '{name}' not found on source cluster. " +
				"Ensure docs-builder indexing has run for this environment before syncing.");
		}

		// Extract the synonyms_set array
		var srcRoot = JsonNode.Parse(srcResp.Body ?? "{}");
		var srcSet = srcRoot?["synonyms_set"]?.AsArray()
			?? throw new InvalidOperationException($"Synonym set '{name}': unexpected response shape — 'synonyms_set' missing.");

		// ── No-op check ────────────────────────────────────────────────────────────
		var dstResp = await destination.GetAsync<StringResponse>($"_synonyms/{name}", cancellationToken: ct);
		if (dstResp.ApiCallDetails.HasSuccessfulStatusCode)
		{
			var dstRoot = JsonNode.Parse(dstResp.Body ?? "{}");
			var dstSet = dstRoot?["synonyms_set"]?.AsArray();
			if (dstSet is not null && SynonymSetsAreEqual(srcSet, dstSet))
			{
				logger.LogInformation("Synonym set '{Name}' is already up-to-date on destination — skipping PUT", name);
				return;
			}
		}

		// ── Copy to destination ────────────────────────────────────────────────────
		// Rebuild the body with only the array (strip result_count etc.)
		var putBody = $"{{\"synonyms_set\":{srcSet.ToJsonString()}}}";
		var putResp = await destination.PutAsync<StringResponse>(
			$"_synonyms/{name}", PostData.String(putBody), ct);
		if (!putResp.ApiCallDetails.HasSuccessfulStatusCode)
		{
			throw new InvalidOperationException(
				$"Failed to copy synonym set '{name}' to destination: " +
				$"{putResp.ApiCallDetails.OriginalException?.Message ?? putResp.ToString()}");
		}

		logger.LogInformation("Synonym set '{Name}' copied to destination", name);
	}

	private async Task CopyQueryRulesetAsync(string name, CancellationToken ct)
	{
		// ── Fetch from source ──────────────────────────────────────────────────────
		var srcResp = await source.GetAsync<StringResponse>($"_query_rules/{name}", cancellationToken: ct);
		if (!srcResp.ApiCallDetails.HasSuccessfulStatusCode)
		{
			throw new InvalidOperationException(
				$"Query ruleset '{name}' not found on source cluster. " +
				"Ensure docs-builder indexing has run for this environment before syncing.");
		}

		// Extract the rules array (ordering is significant — no sort)
		var srcRoot = JsonNode.Parse(srcResp.Body ?? "{}");
		var srcRules = srcRoot?["rules"]?.AsArray()
			?? throw new InvalidOperationException($"Query ruleset '{name}': unexpected response shape — 'rules' missing.");

		// ── No-op check ────────────────────────────────────────────────────────────
		var dstResp = await destination.GetAsync<StringResponse>($"_query_rules/{name}", cancellationToken: ct);
		if (dstResp.ApiCallDetails.HasSuccessfulStatusCode)
		{
			var dstRoot = JsonNode.Parse(dstResp.Body ?? "{}");
			var dstRules = dstRoot?["rules"]?.AsArray();
			if (dstRules is not null &&
				srcRules.ToJsonString() == dstRules.ToJsonString())
			{
				logger.LogInformation("Query ruleset '{Name}' is already up-to-date on destination — skipping PUT", name);
				return;
			}
		}

		// ── Copy to destination ────────────────────────────────────────────────────
		var putBody = $"{{\"rules\":{srcRules.ToJsonString()}}}";
		var putResp = await destination.PutAsync<StringResponse>(
			$"_query_rules/{name}", PostData.String(putBody), ct);
		if (!putResp.ApiCallDetails.HasSuccessfulStatusCode)
		{
			throw new InvalidOperationException(
				$"Failed to copy query ruleset '{name}' to destination: " +
				$"{putResp.ApiCallDetails.OriginalException?.Message ?? putResp.ToString()}");
		}

		logger.LogInformation("Query ruleset '{Name}' copied to destination", name);
	}

	/// <summary>
	/// Compares two synonym-set arrays after normalising entry order by <c>id</c>.
	/// </summary>
	private static bool SynonymSetsAreEqual(JsonArray a, JsonArray b)
	{
		if (a.Count != b.Count)
			return false;

		static string? Id(JsonNode? n) => n?["id"]?.GetValue<string>();

		var sortedA = a.OrderBy(Id).Select(n => n?.ToJsonString()).ToArray();
		var sortedB = b.OrderBy(Id).Select(n => n?.ToJsonString()).ToArray();

		return sortedA.SequenceEqual(sortedB);
	}

	// Matches aliases with a lexical/semantic segment that embeds the env, e.g.:
	//   website-search.semantic-prod-latest
	//   docs-assembler.lexical-staging-latest
	//   site-docset.semantic-dev-latest
	[GeneratedRegex(@"\.(?:lexical|semantic)-(?<env>[^-]+(?:-[^-]+)*?)-latest$", RegexOptions.IgnoreCase)]
	private static partial Regex AliasEnvRegex();

	// Matches the search-facing page alias: ws-content-<env>
	[GeneratedRegex(@"^ws-content-(?<env>.+)$", RegexOptions.IgnoreCase)]
	private static partial Regex PageAliasEnvRegex();
}
