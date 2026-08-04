// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.SiteSearch.Cli.Elasticsearch;

namespace Elastic.SiteSearch.Tests;

public class SearchResourceEnvParsingTests
{
	// ── Happy-path: dot-variant aliases ──────────────────────────────────────────

	[Fact]
	public void Parses_env_from_semantic_alias()
	{
		var ok = SearchResourceSynchronizer.TryDeriveEnvironment("website-search.semantic-prod-latest", out var env);
		ok.Should().BeTrue();
		env.Should().Be("prod");
	}

	[Fact]
	public void Parses_env_from_lexical_alias()
	{
		var ok = SearchResourceSynchronizer.TryDeriveEnvironment("docs-assembler.lexical-staging-latest", out var env);
		ok.Should().BeTrue();
		env.Should().Be("staging");
	}

	[Fact]
	public void Parses_env_from_site_alias_with_buildtype()
	{
		var ok = SearchResourceSynchronizer.TryDeriveEnvironment("site-docset.semantic-dev-latest", out var env);
		ok.Should().BeTrue();
		env.Should().Be("dev");
	}

	[Fact]
	public void Parses_env_from_labs_alias()
	{
		var ok = SearchResourceSynchronizer.TryDeriveEnvironment("labs-docset.lexical-prod-latest", out var env);
		ok.Should().BeTrue();
		env.Should().Be("prod");
	}

	[Fact]
	public void Parses_env_with_hyphenated_buildtype()
	{
		// e.g. site-my-type.semantic-staging-latest — env is "staging", not "type-staging"
		var ok = SearchResourceSynchronizer.TryDeriveEnvironment("site-my-type.semantic-staging-latest", out var env);
		ok.Should().BeTrue();
		env.Should().Be("staging");
	}

	// ── Happy-path: page alias ────────────────────────────────────────────────────

	[Fact]
	public void Parses_env_from_ws_content_alias()
	{
		var ok = SearchResourceSynchronizer.TryDeriveEnvironment("ws-content-dev", out var env);
		ok.Should().BeTrue();
		env.Should().Be("dev");
	}

	[Fact]
	public void Parses_env_from_ws_content_staging()
	{
		var ok = SearchResourceSynchronizer.TryDeriveEnvironment("ws-content-staging", out var env);
		ok.Should().BeTrue();
		env.Should().Be("staging");
	}

	// ── Failure cases ─────────────────────────────────────────────────────────────

	[Fact]
	public void Returns_false_for_bare_index_name()
	{
		var ok = SearchResourceSynchronizer.TryDeriveEnvironment("my-index-name", out var env);
		ok.Should().BeFalse();
		env.Should().BeEmpty();
	}

	[Fact]
	public void Returns_false_for_empty_string()
	{
		var ok = SearchResourceSynchronizer.TryDeriveEnvironment("", out var env);
		ok.Should().BeFalse();
		env.Should().BeEmpty();
	}

	[Fact]
	public void Returns_false_for_alias_missing_latest_suffix()
	{
		// Missing "-latest" at the end — not a write alias
		var ok = SearchResourceSynchronizer.TryDeriveEnvironment("website-search.semantic-prod", out var env);
		ok.Should().BeFalse();
		env.Should().BeEmpty();
	}

	// ── SearchResourceNames ───────────────────────────────────────────────────────

	[Fact]
	public void SearchResourceNames_synonym_set_matches_expected_pattern()
	{
		SearchResourceNames.SynonymSet("prod").Should().Be("docs-assembler-prod");
		SearchResourceNames.SynonymSet("staging").Should().Be("docs-assembler-staging");
	}

	[Fact]
	public void SearchResourceNames_query_ruleset_matches_expected_pattern()
	{
		SearchResourceNames.QueryRuleset("prod").Should().Be("docs-ruleset-assembler-prod");
		SearchResourceNames.QueryRuleset("staging").Should().Be("docs-ruleset-assembler-staging");
	}
}
