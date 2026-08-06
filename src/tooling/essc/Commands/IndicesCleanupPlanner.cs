// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search.Contract;

namespace Elastic.SiteSearch.Cli.Commands;

/// <summary>Describes one alias-family to monitor and clean up.</summary>
internal sealed record AliasEntry(
	string Source,
	string Variant,
	string Environment,
	string LatestAlias,
	string IndexPattern
);

/// <summary>One concrete backing index and its cleanup disposition.</summary>
internal sealed record BackingIndex(
	string Name,
	DateTime Date,
	bool IsActive,
	AliasEntry Group
);

/// <summary>The computed cleanup plan for a single run.</summary>
internal sealed record CleanupPlan(
	IReadOnlyList<BackingIndex> ToKeep,
	IReadOnlyList<BackingIndex> ToDelete,
	IReadOnlyList<string> Warnings
);

/// <summary>Pure, side-effect-free planner for <c>indices cleanup</c>.</summary>
internal static class IndicesCleanupPlanner
{
	private const string DateSuffix = "yyyy.MM.dd.HHmmss";

	/// <summary>
	/// Returns the <c>ws-content-{env}</c> alias name — the single search-facing alias for the
	/// unified ws-catalog index. Centralised here so callers never construct the string directly.
	/// </summary>
	public static string PageAliasName(string env) => $"ws-content-{env}";

	/// <summary>
	/// Builds the complete set of alias entries for the given build type and environment.
	/// Derives site, labs, guide, and ws-catalog entries from their mapping contexts; adds
	/// <c>docs-assembler</c> as a literal external prefix.
	/// </summary>
	public static IReadOnlyList<AliasEntry> BuildAliasEntries(string buildType, string environment)
	{
		var entries = new List<AliasEntry>();

		void Add(string latestAlias)
		{
			// latestAlias ends with "-latest"; IndexPattern replaces that with "-*"
			var indexPattern = latestAlias[..^"-latest".Length] + "-*";
			// Source = everything before the first dot; Variant = segment between dot and env
			var dot = latestAlias.IndexOf('.', StringComparison.Ordinal);
			var source = dot > 0 ? latestAlias[..dot] : latestAlias;
			var afterDot = dot > 0 ? latestAlias[(dot + 1)..] : latestAlias;
			// afterDot: e.g. "lexical-prod-latest" → variant = "lexical"
			var dash = afterDot.IndexOf('-', StringComparison.Ordinal);
			var variant = dash > 0 ? afterDot[..dash] : afterDot;
			entries.Add(new AliasEntry(source, variant, environment, latestAlias, indexPattern));
		}

		// Derived from mapping contexts — propagates NameTemplate changes automatically
		Add(SiteMappingContext.SiteDocument.CreateContext(type: buildType, env: environment).ResolveReadTarget());
		Add(SiteMappingContext.SiteDocumentSemantic.CreateContext(type: buildType, env: environment).ResolveReadTarget());
		Add(LabsMappingContext.LabsDocument.CreateContext(type: buildType, env: environment).ResolveReadTarget());
		Add(LabsMappingContext.LabsDocumentSemantic.CreateContext(type: buildType, env: environment).ResolveReadTarget());
		Add(GuideMappingContext.GuideDocument.CreateContext(type: buildType, env: environment).ResolveReadTarget());
		Add(GuideMappingContext.GuideDocumentSemantic.CreateContext(type: buildType, env: environment).ResolveReadTarget());
		Add(WebsiteSearchMappingContext.WebsiteSearchDocument.CreateContext(env: environment).ResolveReadTarget());
		Add(WebsiteSearchMappingContext.WebsiteSearchDocumentSemantic.CreateContext(env: environment).ResolveReadTarget());

		// External prefix — not owned by any mapping context in this repo
		Add($"docs-assembler.lexical-{environment}-latest");
		Add($"docs-assembler.semantic-{environment}-latest");

		return entries.AsReadOnly();
	}

	/// <summary>
	/// Computes a cleanup plan.
	/// </summary>
	/// <param name="indexAliases">
	/// Dictionary of index name → set of alias names that point to that index.
	/// Typically parsed from an Elasticsearch <c>GET &lt;pattern&gt;/_alias</c> response.
	/// </param>
	/// <param name="knownAliases">Alias entries from <see cref="BuildAliasEntries"/>.</param>
	/// <param name="keep">Total backing indices to retain per (source, variant) pair — includes the active one.</param>
	/// <param name="pageAlias">
	/// The search-facing alias (e.g. <c>ws-content-prod</c>). Any index this alias points to is
	/// unconditionally protected. A warning is emitted when its target diverges from the
	/// <c>ws-catalog.semantic-{env}-latest</c> target.
	/// </param>
	public static CleanupPlan Plan(
		IReadOnlyDictionary<string, IReadOnlySet<string>> indexAliases,
		IReadOnlyList<AliasEntry> knownAliases,
		int keep,
		string? pageAlias = null)
	{
		keep = Math.Max(1, keep);
		var knownLatestAliasSet = new HashSet<string>(knownAliases.Select(a => a.LatestAlias), StringComparer.OrdinalIgnoreCase);
		if (pageAlias is not null)
			_ = knownLatestAliasSet.Add(pageAlias);

		var warnings = new List<string>();
		var parsed = new List<BackingIndex>();

		if (pageAlias is not null)
		{
			var semanticLatestAlias = knownAliases
				.FirstOrDefault(a => a.Source == "ws-catalog" && a.Variant == "semantic")
				?.LatestAlias;
			if (semanticLatestAlias is not null)
			{
				string? pageTarget = null, semanticTarget = null;
				foreach (var (idx, aliases) in indexAliases)
				{
					if (aliases.Contains(pageAlias, StringComparer.OrdinalIgnoreCase))
						pageTarget = idx;
					if (aliases.Contains(semanticLatestAlias, StringComparer.OrdinalIgnoreCase))
						semanticTarget = idx;
				}
				if (pageTarget is not null && semanticTarget is not null &&
					!string.Equals(pageTarget, semanticTarget, StringComparison.OrdinalIgnoreCase))
				{
					warnings.Add(
						$"'{pageAlias}' → '{pageTarget}' differs from '{semanticLatestAlias}' → '{semanticTarget}'; both indices are protected");
				}
			}
		}

		foreach (var (indexName, aliases) in indexAliases)
		{
			var entry = MatchGroup(indexName, knownAliases);
			if (entry is null)
				continue;

			var prefix = entry.LatestAlias[..^"-latest".Length] + "-";
			if (!indexName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
				continue;

			var suffix = indexName[prefix.Length..];

			// Well-known auxiliary indices (e.g. -ai-cache) are intentionally excluded from cleanup.
			if (suffix.EndsWith("-ai-cache", StringComparison.OrdinalIgnoreCase) ||
				suffix.Equals("ai-cache", StringComparison.OrdinalIgnoreCase))
				continue;

			if (!DateTime.TryParseExact(suffix, DateSuffix, System.Globalization.CultureInfo.InvariantCulture,
					System.Globalization.DateTimeStyles.AssumeUniversal, out var date))
			{
				warnings.Add($"Skipping '{indexName}': suffix '{suffix}' did not parse as {DateSuffix}");
				continue;
			}

			var isActive = aliases.Overlaps(knownLatestAliasSet);
			parsed.Add(new BackingIndex(indexName, date, isActive, entry));
		}

		var toKeep = new List<BackingIndex>();
		var toDelete = new List<BackingIndex>();

		// Group by (Source, Variant) then apply retention policy
		var groups = parsed.GroupBy(b => (b.Group.Source, b.Group.Variant));
		foreach (var group in groups)
		{
			var sorted = group.OrderByDescending(b => b.Date).ToList();
			var active = sorted.Where(b => b.IsActive).ToList();
			var nonActive = sorted.Where(b => !b.IsActive).ToList();

			// Active indices are always kept; they consume from the keep budget
			var keepNonActive = Math.Max(0, keep - active.Count);

			toKeep.AddRange(active);
			toKeep.AddRange(nonActive.Take(keepNonActive));
			toDelete.AddRange(nonActive.Skip(keepNonActive));

			if (active.Count > keep)
				warnings.Add($"Active index count ({active.Count}) for {group.Key.Source}.{group.Key.Variant} exceeds --keep ({keep}); all active indices are retained.");
		}

		return new CleanupPlan(toKeep.AsReadOnly(), toDelete.AsReadOnly(), warnings.AsReadOnly());
	}

	private static AliasEntry? MatchGroup(string indexName, IReadOnlyList<AliasEntry> entries)
	{
		foreach (var entry in entries)
		{
			var prefix = entry.LatestAlias[..^"-latest".Length] + "-";
			if (indexName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
				return entry;
		}
		return null;
	}
}
