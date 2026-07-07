// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.SiteSearch.Cli.Commands;

namespace Elastic.SiteSearch.Tests;

public class IndicesCleanupPlannerTests
{
	private static readonly AliasEntry TestEntry = new(
		Source: "test-source",
		Variant: "lexical",
		Environment: "prod",
		LatestAlias: "test-source.lexical-prod-latest",
		IndexPattern: "test-source.lexical-prod-*"
	);

	private static Dictionary<string, IReadOnlySet<string>> Idx(params (string Name, string[] Aliases)[] entries) =>
		entries.ToDictionary(
			e => e.Name,
			e => (IReadOnlySet<string>)e.Aliases.ToHashSet(StringComparer.OrdinalIgnoreCase),
			StringComparer.OrdinalIgnoreCase);

	[Fact]
	public void Empty_input_returns_empty_plan()
	{
		var plan = IndicesCleanupPlanner.Plan(Idx(), [TestEntry], keep: 2);

		plan.ToKeep.Should().BeEmpty();
		plan.ToDelete.Should().BeEmpty();
		plan.Warnings.Should().BeEmpty();
	}

	[Fact]
	public void Single_active_index_is_always_kept()
	{
		var indexAliases = Idx(
			("test-source.lexical-prod-2026.01.01.000000", ["test-source.lexical-prod-latest"]));

		var plan = IndicesCleanupPlanner.Plan(indexAliases, [TestEntry], keep: 2);

		plan.ToKeep.Should().ContainSingle(i => i.Name == "test-source.lexical-prod-2026.01.01.000000");
		plan.ToDelete.Should().BeEmpty();
	}

	[Fact]
	public void Active_is_newest_keep2_deletes_older_two()
	{
		var indexAliases = Idx(
			("test-source.lexical-prod-2026.04.15.000000", ["test-source.lexical-prod-latest"]),
			("test-source.lexical-prod-2026.04.14.000000", []),
			("test-source.lexical-prod-2026.04.13.000000", []),
			("test-source.lexical-prod-2026.04.12.000000", []));

		var plan = IndicesCleanupPlanner.Plan(indexAliases, [TestEntry], keep: 2);

		plan.ToKeep.Should().HaveCount(2);
		plan.ToKeep.Should().Contain(i => i.Name == "test-source.lexical-prod-2026.04.15.000000" && i.IsActive);
		plan.ToKeep.Should().Contain(i => i.Name == "test-source.lexical-prod-2026.04.14.000000" && !i.IsActive);

		plan.ToDelete.Should().HaveCount(2);
		plan.ToDelete.Should().Contain(i => i.Name == "test-source.lexical-prod-2026.04.13.000000");
		plan.ToDelete.Should().Contain(i => i.Name == "test-source.lexical-prod-2026.04.12.000000");
	}

	[Fact]
	public void Active_is_middle_keep2_is_still_retained()
	{
		// Active is NOT the newest — it still must survive
		var indexAliases = Idx(
			("test-source.lexical-prod-2026.04.15.000000", []),
			("test-source.lexical-prod-2026.04.14.000000", []),
			("test-source.lexical-prod-2026.04.13.000000", ["test-source.lexical-prod-latest"]),
			("test-source.lexical-prod-2026.04.12.000000", []));

		var plan = IndicesCleanupPlanner.Plan(indexAliases, [TestEntry], keep: 2);

		// Active (2026.04.13) always kept; budget = 2 so 1 non-active kept = newest (2026.04.15)
		plan.ToKeep.Should().HaveCount(2);
		plan.ToKeep.Should().Contain(i => i.Name == "test-source.lexical-prod-2026.04.13.000000" && i.IsActive);
		plan.ToKeep.Should().Contain(i => i.Name == "test-source.lexical-prod-2026.04.15.000000" && !i.IsActive);

		plan.ToDelete.Should().HaveCount(2);
		plan.ToDelete.Should().Contain(i => i.Name == "test-source.lexical-prod-2026.04.14.000000");
		plan.ToDelete.Should().Contain(i => i.Name == "test-source.lexical-prod-2026.04.12.000000");
	}

	[Fact]
	public void Keep1_active_counts_no_non_active_kept()
	{
		var indexAliases = Idx(
			("test-source.lexical-prod-2026.04.15.000000", ["test-source.lexical-prod-latest"]),
			("test-source.lexical-prod-2026.04.14.000000", []),
			("test-source.lexical-prod-2026.04.13.000000", []));

		var plan = IndicesCleanupPlanner.Plan(indexAliases, [TestEntry], keep: 1);

		plan.ToKeep.Should().ContainSingle(i => i.IsActive);
		plan.ToDelete.Should().HaveCount(2);
	}

	[Fact]
	public void Indices_with_non_date_suffix_are_skipped_with_warning()
	{
		var indexAliases = Idx(
			("test-source.lexical-prod-latest", []),           // alias itself, not a backing index
			("test-source.lexical-prod-not-a-date", []),       // malformed
			("test-source.lexical-prod-2026.04.15.000000", ["test-source.lexical-prod-latest"]));

		var plan = IndicesCleanupPlanner.Plan(indexAliases, [TestEntry], keep: 2);

		plan.Warnings.Should().HaveCount(2);
		plan.ToKeep.Should().ContainSingle();
		plan.ToDelete.Should().BeEmpty();
	}

	[Fact]
	public void Multiple_groups_are_planned_independently()
	{
		var lexicalEntry = new AliasEntry("test-source", "lexical", "prod", "test-source.lexical-prod-latest", "test-source.lexical-prod-*");
		var semanticEntry = new AliasEntry("test-source", "semantic", "prod", "test-source.semantic-prod-latest", "test-source.semantic-prod-*");

		var indexAliases = Idx(
			("test-source.lexical-prod-2026.04.15.000000", ["test-source.lexical-prod-latest"]),
			("test-source.lexical-prod-2026.04.14.000000", []),
			("test-source.lexical-prod-2026.04.13.000000", []),
			("test-source.semantic-prod-2026.04.15.000000", ["test-source.semantic-prod-latest"]),
			("test-source.semantic-prod-2026.04.14.000000", []),
			("test-source.semantic-prod-2026.04.13.000000", []));

		var plan = IndicesCleanupPlanner.Plan(indexAliases, [lexicalEntry, semanticEntry], keep: 2);

		// Each group: keep 2 (active + 1 newest non-active), delete 1 oldest
		plan.ToKeep.Should().HaveCount(4);
		plan.ToDelete.Should().HaveCount(2);
		plan.ToDelete.Should().Contain(i => i.Name == "test-source.lexical-prod-2026.04.13.000000");
		plan.ToDelete.Should().Contain(i => i.Name == "test-source.semantic-prod-2026.04.13.000000");
	}

	[Fact]
	public void Unrelated_indices_in_response_are_ignored()
	{
		var indexAliases = Idx(
			("test-source.lexical-prod-2026.04.15.000000", ["test-source.lexical-prod-latest"]),
			("some-other-index-2026.04.15.000000", []),
			("completely-unrelated", ["some-alias"]));

		var plan = IndicesCleanupPlanner.Plan(indexAliases, [TestEntry], keep: 2);

		plan.ToKeep.Should().ContainSingle(i => i.Name.Contains("test-source"));
		plan.ToDelete.Should().BeEmpty();
	}

	[Fact]
	public void BuildAliasEntries_returns_ten_entries()
	{
		var entries = IndicesCleanupPlanner.BuildAliasEntries("public", "prod");

		entries.Should().HaveCount(10);
		entries.Should().Contain(e => e.LatestAlias == "docs-assembler.lexical-prod-latest");
		entries.Should().Contain(e => e.LatestAlias == "docs-assembler.semantic-prod-latest");
		entries.Should().Contain(e => e.Source == "site-public" || e.LatestAlias.StartsWith("site-public."));
		entries.Should().Contain(e => e.Source == "labs-public" || e.LatestAlias.StartsWith("labs-public."));
		entries.Should().Contain(e => e.Source == "guide-public" || e.LatestAlias.StartsWith("guide-public."));
		entries.Should().Contain(e => e.LatestAlias == "ws-catalog.lexical-prod-latest");
		entries.Should().Contain(e => e.LatestAlias == "ws-catalog.semantic-prod-latest");
		entries.Should().OnlyContain(e => e.LatestAlias.EndsWith("-latest"));
		entries.Should().OnlyContain(e => e.IndexPattern.EndsWith("-*"));
	}

	[Fact]
	public void PageAlias_on_older_index_keeps_it_regardless_of_keep_budget()
	{
		// ws-content-prod points to an older index that -latest does not; it must not be deleted
		var semanticEntry = new AliasEntry("ws-catalog", "semantic", "prod",
			"ws-catalog.semantic-prod-latest", "ws-catalog.semantic-prod-*");
		var indexAliases = Idx(
			("ws-catalog.semantic-prod-2026.04.15.000000", ["ws-catalog.semantic-prod-latest"]),
			("ws-catalog.semantic-prod-2026.04.14.000000", ["ws-content-prod"]),
			("ws-catalog.semantic-prod-2026.04.13.000000", []));

		// keep=1 would normally only retain the active index and delete the other two;
		// but the page-alias index must also survive
		var plan = IndicesCleanupPlanner.Plan(indexAliases, [semanticEntry], keep: 1,
			pageAlias: "ws-content-prod");

		plan.ToKeep.Should().HaveCount(2);
		plan.ToKeep.Should().Contain(i => i.Name == "ws-catalog.semantic-prod-2026.04.15.000000" && i.IsActive);
		plan.ToKeep.Should().Contain(i => i.Name == "ws-catalog.semantic-prod-2026.04.14.000000" && i.IsActive);
		plan.ToDelete.Should().ContainSingle(i => i.Name == "ws-catalog.semantic-prod-2026.04.13.000000");
	}

	[Fact]
	public void PageAlias_pointing_to_different_index_than_semantic_latest_emits_warning()
	{
		var semanticEntry = new AliasEntry("ws-catalog", "semantic", "prod",
			"ws-catalog.semantic-prod-latest", "ws-catalog.semantic-prod-*");
		var indexAliases = Idx(
			// -latest points here
			("ws-catalog.semantic-prod-2026.04.15.000000", ["ws-catalog.semantic-prod-latest"]),
			// pages alias points to an older index — mismatch!
			("ws-catalog.semantic-prod-2026.04.14.000000", ["ws-content-prod"]));

		var plan = IndicesCleanupPlanner.Plan(indexAliases, [semanticEntry], keep: 2,
			pageAlias: "ws-content-prod");

		// Both indices must be kept (one via -latest, one via page alias)
		plan.ToDelete.Should().BeEmpty();
		plan.ToKeep.Should().HaveCount(2);
		// And a warning must be emitted
		plan.Warnings.Should().ContainSingle(w => w.Contains("ws-content-prod") && w.Contains("differs"));
	}
}
