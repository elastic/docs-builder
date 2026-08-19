// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Links.CrossLinks;

public static class CrossLinkFetchDiagnostics
{
	private const string CodexLinkIndexUrl = "https://github.com/elastic/codex-link-index";

	public static void EmitFetchFailures(IDiagnosticsCollector collector, string configurationPath, FetchedCrossLinks crossLinks)
	{
		if (crossLinks.FetchFailures.Count == 0)
			return;

		collector.EmitError(configurationPath, FormatSummary(crossLinks));
	}

	internal static string FormatSummary(FetchedCrossLinks crossLinks)
	{
		var fetchFailures = crossLinks.FetchFailures;
		var repositories = fetchFailures.Keys.Order(StringComparer.Ordinal).ToArray();
		var isCodexFailure = repositories.All(repository =>
			crossLinks.RegistryByRepository?.GetValueOrDefault(repository) is { } registry
			&& registry != DocSetRegistry.Public);
		var distinctReasons = fetchFailures.Values.Distinct(StringComparer.Ordinal).ToArray();
		var heading = isCodexFailure && distinctReasons.Length == 1
			? $"Could not fetch the Elastic Internal Docs link index from {CodexLinkIndexUrl}."
			: $"Could not fetch cross-link index data for: {string.Join(", ", repositories)}.";
		var details = distinctReasons.Length == 1
			? distinctReasons[0]
			: string.Join(Environment.NewLine, repositories.Select(repository => $"{repository}: {fetchFailures[repository]}"));
		var validation = repositories.Length == 1
			? $"Cross-links to {repositories[0]} were not validated."
			: $"Cross-links to these repositories were not validated: {string.Join(", ", repositories)}.";

		return $"{heading}{Environment.NewLine}{Environment.NewLine}{details}{Environment.NewLine}{Environment.NewLine}{validation}";
	}
}
