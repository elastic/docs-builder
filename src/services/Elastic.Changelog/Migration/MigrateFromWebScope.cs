// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Migration;

/// <summary>
/// TEMPORARY (elastic/docs-eng-team#736): one product's migration scope — where its published
/// release-notes Markdown lives (owner/repo/path at a pinned ref) and the inclusive version cutoff.
/// The checked-in table below replaces the former <c>config/migrate-from-web.yml</c> (dropped on
/// review: no standing config surface for a one-off tool). It grows per rollout wave
/// (elastic/docs-eng-team#683) and is deleted together with the command once the rollout completes.
/// </summary>
public sealed record MigrateFromWebScope
{
	public required string ProductId { get; init; }

	/// <summary>GitHub owner of the source repository (e.g. <c>elastic</c>).</summary>
	public required string Owner { get; init; }

	/// <summary>Source repository name (e.g. <c>elastic-otel-java</c>).</summary>
	public required string Repo { get; init; }

	/// <summary>Repository-relative path of the release-notes Markdown page.</summary>
	public required string Path { get; init; }

	/// <summary>Pinned git ref (commit SHA) at which the Markdown is fetched (reproducible runs).</summary>
	public required string Ref { get; init; }

	/// <summary>Inclusive upper version bound; releases above it belong to the live pipeline.</summary>
	public required string Cutoff { get; init; }

	/// <summary>
	/// Every product the migration knows how to source. The page→product mapping is deliberately
	/// checked in rather than derived: bundle product ids appear in no published metadata (page
	/// frontmatter carries the site taxonomy, not bundle ids), so each entry pins its source
	/// explicitly. A run covers the whole table unless narrowed with <c>--products</c>.
	/// </summary>
	public static ImmutableArray<MigrateFromWebScope> All { get; } =
	[
		new()
		{
			ProductId = "edot-java",
			Owner = "elastic",
			Repo = "elastic-otel-java",
			Path = "docs/release-notes/index.md",
			// Last commit before the repo switched to native docs-builder bundle YAMLs (#1023):
			// the final hand-authored state of the published release-notes Markdown.
			Ref = "9a61ce4faaf08e272c433a083bcc6f0e96d80e0a",
			Cutoff = "1.10.0"
		}
	];

	/// <summary>
	/// Resolves a <c>--products</c> selection against the table — the whole table when the selection
	/// is empty — or null (with an error emitted) when any requested id is unknown.
	/// </summary>
	public static IReadOnlyList<MigrateFromWebScope>? Select(IDiagnosticsCollector collector, IReadOnlyList<string> products)
	{
		if (products.Count == 0)
			return All;

		var byId = All.ToDictionary(s => s.ProductId, StringComparer.Ordinal);
		var selected = new List<MigrateFromWebScope>(products.Count);
		var unknown = new List<string>();
		foreach (var product in products.Distinct(StringComparer.Ordinal))
		{
			if (byId.TryGetValue(product, out var scope))
				selected.Add(scope);
			else
				unknown.Add(product);
		}

		if (unknown.Count > 0)
		{
			var known = string.Join(", ", All.Select(s => s.ProductId).Order(StringComparer.Ordinal));
			collector.EmitError(
				string.Empty,
				$"Unknown product id(s) in --products: {string.Join(", ", unknown)}. Products in the checked-in migration scope: {known}. Add an entry to MigrateFromWebScope.All before running the migration."
			);
			return null;
		}

		return selected;
	}
}
