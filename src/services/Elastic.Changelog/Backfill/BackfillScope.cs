// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;
using Elastic.Documentation;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Backfill;

/// <summary>
/// One product's backfill scope — where its release-notes Markdown lives (either a pinned
/// raw.githubusercontent.com ref or the published elastic.co site page) and the optional version
/// cutoff above which the live bundle pipeline takes over.
/// </summary>
public sealed record BackfillScope
{
	public required string ProductId { get; init; }

	/// <summary>
	/// Site-relative path (without leading slash, without <c>.md</c> extension) used to build the
	/// elastic.co export URL: <c>https://www.elastic.co/docs/release-notes/{Path}.md</c>.
	/// </summary>
	public required string Path { get; init; }

	/// <summary>
	/// Default lifecycle applied to every release parsed from this page, unless the page itself
	/// carries an <c>{applies_to}</c> directive overriding it.
	/// </summary>
	public Lifecycle DefaultLifecycle { get; init; } = Lifecycle.Ga;

	// Repo-source fields — Owner, Repo, Ref, RepoPath all required together; absent means site source.

	/// <summary>GitHub owner of the source repository (e.g. <c>elastic</c>).</summary>
	public string? Owner { get; init; }

	/// <summary>Source repository name (e.g. <c>elastic-otel-java</c>).</summary>
	public string? Repo { get; init; }

	/// <summary>Pinned git ref (commit SHA) at which the Markdown is fetched (reproducible runs).</summary>
	public string? Ref { get; init; }

	/// <summary>
	/// Repository-relative path of the release-notes Markdown file (e.g. <c>docs/release-notes/index.md</c>).
	/// Used only when this scope is a repo source; distinct from <see cref="Path"/> which is the site-relative path.
	/// </summary>
	public string? RepoPath { get; init; }

	/// <summary>Inclusive upper version bound; releases above it belong to the live pipeline.</summary>
	public string? Cutoff { get; init; }

	/// <summary>
	/// Returns true when this scope is sourced from a pinned GitHub ref rather than the published
	/// elastic.co site page.
	/// </summary>
	public bool IsRepoSource => Owner is not null && Repo is not null && Ref is not null && RepoPath is not null;

	/// <summary>
	/// Every product the backfill knows how to source. Scope entries that carry repo-source fields
	/// fetch from the pinned raw.githubusercontent.com ref; all others fetch the published
	/// elastic.co site page (<c>https://www.elastic.co/docs/release-notes/{Path}.md</c>).
	///
	/// <para>
	/// The page→product mapping is deliberately checked in rather than derived: bundle product ids
	/// appear in no published metadata (page frontmatter carries the site taxonomy, not bundle ids),
	/// so each entry pins its source explicitly. A run covers the whole table unless narrowed with
	/// <c>--products</c>.
	/// </para>
	/// </summary>
	public static ImmutableArray<BackfillScope> All { get; } =
	[
		// Repo-source entries: the published elastic.co page is an empty <changelog></changelog>
		// stub for these products, so the hand-authored Markdown from a pinned git ref is the only
		// surviving source of their release notes.
		new()
		{
			ProductId = "edot-java",
			Path = "edot/sdks/java",
			Owner = "elastic",
			Repo = "elastic-otel-java",
			// Last commit before the repo switched to native docs-builder bundle YAMLs (#1023):
			// the final hand-authored state of the published release-notes Markdown.
			Ref = "9a61ce4faaf08e272c433a083bcc6f0e96d80e0a",
			RepoPath = "docs/release-notes/index.md",
			Cutoff = "1.10.0"
		},
		// Site-source entries: published elastic.co pages that render expanded release-notes content.
		new() { ProductId = "elasticsearch", Path = "elasticsearch" },
		new() { ProductId = "kibana", Path = "kibana" },
		new() { ProductId = "elastic-agent", Path = "elastic-agent" },
		new() { ProductId = "fleet-server", Path = "fleet-server" },
		new() { ProductId = "logstash", Path = "logstash" },
		new() { ProductId = "beats", Path = "beats" },
		new() { ProductId = "cloud-serverless", Path = "cloud-serverless" },
		new() { ProductId = "cloud-hosted", Path = "cloud-hosted" },
		new() { ProductId = "cloud-enterprise", Path = "cloud-enterprise" },
		new() { ProductId = "cloud-on-k8s", Path = "cloud-on-k8s" },
		new() { ProductId = "observability", Path = "observability" },
		new() { ProductId = "elastic-security", Path = "security" },
		new() { ProductId = "ecs", Path = "ecs" },
		new() { ProductId = "apm", Path = "apm" },
		new() { ProductId = "apm-agent-dotnet", Path = "apm/agents/dotnet" },
		new() { ProductId = "apm-agent-go", Path = "apm/agents/go" },
		new() { ProductId = "apm-agent-java", Path = "apm/agents/java" },
		new() { ProductId = "apm-agent-nodejs", Path = "apm/agents/nodejs" },
		new() { ProductId = "apm-agent-php", Path = "apm/agents/php" },
		new() { ProductId = "apm-agent-python", Path = "apm/agents/python" },
		new() { ProductId = "apm-agent-ruby", Path = "apm/agents/ruby" },
		new() { ProductId = "apm-agent-rum-js", Path = "apm/agents/rum-js" },
		new() { ProductId = "edot-android", Path = "edot/sdks/android" },
		new() { ProductId = "edot-ios", Path = "edot/sdks/ios" },
		new() { ProductId = "edot-dotnet", Path = "edot/sdks/dotnet" },
		new() { ProductId = "edot-node", Path = "edot/sdks/node" },
		new() { ProductId = "edot-python", Path = "edot/sdks/python" },
		new() { ProductId = "edot-php", Path = "edot/sdks/php" },
		new() { ProductId = "edot-rum", Path = "edot/sdks/rum" },
		new() { ProductId = "edot-cf-aws", Path = "edot/cloud-forwarder/aws" },
		new() { ProductId = "elasticsearch-client-java", Path = "elasticsearch/clients/java" },
		new() { ProductId = "elasticsearch-client-javascript", Path = "elasticsearch/clients/javascript" },
		new() { ProductId = "elasticsearch-client-dotnet", Path = "elasticsearch/clients/dotnet" },
		new() { ProductId = "elasticsearch-client-php", Path = "elasticsearch/clients/php" },
		new() { ProductId = "elasticsearch-client-python", Path = "elasticsearch/clients/python" },
		new() { ProductId = "elasticsearch-client-ruby", Path = "elasticsearch/clients/ruby" },
		new() { ProductId = "elasticsearch-hadoop", Path = "elasticsearch-hadoop" },
	];

	/// <summary>
	/// Resolves a <c>--products</c> selection against the table — the whole table when the selection
	/// is empty — or null (with an error emitted) when any requested id is unknown.
	/// </summary>
	public static IReadOnlyList<BackfillScope>? Select(IDiagnosticsCollector collector, IReadOnlyList<string> products)
	{
		if (products.Count == 0)
			return All;

		var byId = All.ToDictionary(s => s.ProductId, StringComparer.Ordinal);
		var selected = new List<BackfillScope>(products.Count);
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
				$"Unknown product id(s) in --products: {string.Join(", ", unknown)}. Products in the checked-in backfill scope: {known}. Add an entry to BackfillScope.All to include a new product."
			);
			return null;
		}

		return selected;
	}
}
