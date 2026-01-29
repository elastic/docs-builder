// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Myst.Directives.Changelog;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

/// <summary>
/// Tests for the ShouldHideLinksForRepo method which determines if links should be hidden
/// based on whether any component of the bundle's repository is private.
/// </summary>
public class ChangelogShouldHideLinksForRepoTests
{
	[Fact]
	public void ReturnsTrue_WhenSingleRepoIsPrivate()
	{
		var privateRepos = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "private-repo" };

		var result = ChangelogInlineRenderer.ShouldHideLinksForRepo("private-repo", privateRepos);

		result.Should().BeTrue();
	}

	[Fact]
	public void ReturnsFalse_WhenSingleRepoIsNotPrivate()
	{
		var privateRepos = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "other-repo" };

		var result = ChangelogInlineRenderer.ShouldHideLinksForRepo("elasticsearch", privateRepos);

		result.Should().BeFalse();
	}

	[Fact]
	public void ReturnsFalse_WhenPrivateReposIsEmpty()
	{
		var privateRepos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		var result = ChangelogInlineRenderer.ShouldHideLinksForRepo("elasticsearch", privateRepos);

		result.Should().BeFalse();
	}

	[Fact]
	public void ReturnsTrue_WhenMergedRepoContainsPrivateRepo()
	{
		// Merged bundle repos are joined with '+'
		var privateRepos = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "private-repo" };

		var result = ChangelogInlineRenderer.ShouldHideLinksForRepo("elasticsearch+kibana+private-repo", privateRepos);

		result.Should().BeTrue();
	}

	[Fact]
	public void ReturnsFalse_WhenMergedRepoContainsNoPrivateRepos()
	{
		var privateRepos = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "other-private" };

		var result = ChangelogInlineRenderer.ShouldHideLinksForRepo("elasticsearch+kibana", privateRepos);

		result.Should().BeFalse();
	}

	[Fact]
	public void IsCaseInsensitive_ForRepoNames()
	{
		var privateRepos = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Private-Repo" };

		var result = ChangelogInlineRenderer.ShouldHideLinksForRepo("private-repo", privateRepos);

		result.Should().BeTrue();
	}

	[Fact]
	public void HandlesWhitespace_InMergedRepoNames()
	{
		var privateRepos = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "private-repo" };

		// The split uses TrimEntries, so whitespace around names should be handled
		var result = ChangelogInlineRenderer.ShouldHideLinksForRepo("elasticsearch + private-repo + kibana", privateRepos);

		result.Should().BeTrue();
	}
}

/// <summary>
/// Tests that links are shown for public repositories.
/// </summary>
public class ChangelogLinksDefaultBehaviorTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogLinksDefaultBehaviorTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Feature with PR and issues
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  pr: "123456"
		  issues:
		  - "78901"
		  - "78902"
		"""));

	[Fact]
	public void PrivateRepositoriesPropertyIsAccessible() =>
		// The PrivateRepositories property should be accessible
		// (may contain repos from embedded assembler.yml)
		Block!.PrivateRepositories.Should().NotBeNull();

	[Fact]
	public void RendersPrLinksForPublicRepo()
	{
		// elasticsearch is a public repo, so PR link should be visible in the output
		Html.Should().Contain("123456");
		Html.Should().Contain("github.com");
	}

	[Fact]
	public void RendersIssueLinksForPublicRepo()
	{
		// elasticsearch is public, so issue links should be visible
		Html.Should().Contain("78901");
		Html.Should().Contain("78902");
	}
}

/// <summary>
/// Tests that links are hidden when the bundle's repository is marked as private.
/// Uses internal setter to simulate private repo detection from assembler.yml.
/// </summary>
public class ChangelogLinksHiddenForPrivateRepoTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogLinksHiddenForPrivateRepoTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Feature with PR and issues
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  pr: "123456"
		  issues:
		  - "78901"
		  - "78902"
		"""));

	public override async ValueTask InitializeAsync()
	{
		await base.InitializeAsync();
		// Simulate that 'elasticsearch' is a private repository
		Block!.PrivateRepositories.Add("elasticsearch");
	}

	[Fact]
	public void PrivateRepositoriesContainsConfiguredRepo() =>
		Block!.PrivateRepositories.Should().Contain("elasticsearch");

	[Fact]
	public void HidesPrLinksForPrivateRepo()
	{
		// Re-render after setting private repos
		var markdown = ChangelogInlineRenderer.RenderChangelogMarkdown(Block!);

		// PR links should NOT be rendered as clickable links
		markdown.Should().NotBeNull();
		markdown.Should().Contain("123456"); // PR number still appears
		markdown.Should().Contain("%"); // Links are commented out
	}

	[Fact]
	public void HidesIssueLinksForPrivateRepo()
	{
		// Re-render after setting private repos
		var markdown = ChangelogInlineRenderer.RenderChangelogMarkdown(Block!);

		// Issue links should be commented out
		markdown.Should().Contain("78901");
		markdown.Should().Contain("78902");
		markdown.Should().Contain("%"); // Links are commented out
	}
}

/// <summary>
/// Tests link hiding behavior with detailed entries (breaking changes, deprecations).
/// </summary>
public class ChangelogLinksHiddenInDetailedEntriesTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogLinksHiddenInDetailedEntriesTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Breaking change with PR
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: API has changed.
		  impact: Users must update.
		  action: Follow migration guide.
		  pr: "999888"
		  issues:
		  - "777666"
		- title: Deprecation with PR
		  type: deprecation
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Old API deprecated.
		  impact: Will be removed.
		  action: Use new API.
		  pr: "555444"
		"""));

	public override async ValueTask InitializeAsync()
	{
		await base.InitializeAsync();
		// Simulate that 'elasticsearch' is a private repository
		Block!.PrivateRepositories.Add("elasticsearch");
	}

	[Fact]
	public void HidesLinksInBreakingChangesSection()
	{
		var markdown = ChangelogInlineRenderer.RenderChangelogMarkdown(Block!);

		// The breaking change section should have the links hidden (commented out)
		markdown.Should().Contain("Breaking change with PR");
		markdown.Should().Contain("999888"); // PR number appears
		markdown.Should().Contain("%"); // Links are commented out
	}

	[Fact]
	public void HidesLinksInDeprecationsSection()
	{
		var markdown = ChangelogInlineRenderer.RenderChangelogMarkdown(Block!);

		// The deprecation section should have the links hidden
		markdown.Should().Contain("Deprecation with PR");
		markdown.Should().Contain("555444"); // PR number appears
		markdown.Should().Contain("%"); // Links are commented out
	}

	[Fact]
	public void RendersImpactAndActionSections()
	{
		var markdown = ChangelogInlineRenderer.RenderChangelogMarkdown(Block!);

		// Impact and action should still be rendered
		markdown.Should().Contain("Impact");
		markdown.Should().Contain("Users must update");
		markdown.Should().Contain("Action");
		markdown.Should().Contain("Follow migration guide");
	}
}

/// <summary>
/// Tests that links are shown for public repos even when some private repos are configured.
/// </summary>
public class ChangelogLinksShownForPublicRepoTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogLinksShownForPublicRepoTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Feature from public repo
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  pr: "111111"
		"""));

	public override async ValueTask InitializeAsync()
	{
		await base.InitializeAsync();
		// Configure a different repo as private - not elasticsearch
		Block!.PrivateRepositories.Add("private-internal-repo");
	}

	[Fact]
	public void ShowsPrLinksForPublicRepo()
	{
		var markdown = ChangelogInlineRenderer.RenderChangelogMarkdown(Block!);

		// PR links should be visible (not commented out) since elasticsearch is public
		markdown.Should().Contain("111111");
		// The link should be a proper GitHub URL, not commented out
		markdown.Should().Contain("[#111111]");
		markdown.Should().Contain("github.com/elastic/elasticsearch/pull/111111");
	}
}

/// <summary>
/// Tests link hiding with merged bundles where one repo is private.
/// </summary>
public class ChangelogLinksWithMergedBundlesTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogLinksWithMergedBundlesTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:::
		""")
	{
		// Add bundles from two repos with the same target version (will be merged)
		FileSystem.AddFile("docs/changelog/bundles/elasticsearch-2025-08-05.yaml", new MockFileData(
			// language=yaml
			"""
			products:
			- product: elasticsearch
			  target: 2025-08-05
			entries:
			- title: ES Feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 2025-08-05
			  pr: "111111"
			"""));

		FileSystem.AddFile("docs/changelog/bundles/kibana-2025-08-05.yaml", new MockFileData(
			// language=yaml
			"""
			products:
			- product: kibana
			  target: 2025-08-05
			entries:
			- title: Kibana Feature
			  type: feature
			  products:
			  - product: kibana
			    target: 2025-08-05
			  pr: "222222"
			"""));
	}

	public override async ValueTask InitializeAsync()
	{
		await base.InitializeAsync();
		// Kibana is a private repo
		Block!.PrivateRepositories.Add("kibana");
	}

	[Fact]
	public void MergedBundleRepoContainsBothRepos()
	{
		// Bundles with same target version are merged, repo names combined with '+'
		Block!.LoadedBundles.Should().HaveCount(1);
		Block!.LoadedBundles[0].Repo.Should().Contain("elasticsearch");
		Block!.LoadedBundles[0].Repo.Should().Contain("kibana");
		Block!.LoadedBundles[0].Repo.Should().Contain("+");
	}

	[Fact]
	public void HidesLinksWhenAnyMergedRepoIsPrivate()
	{
		var markdown = ChangelogInlineRenderer.RenderChangelogMarkdown(Block!);

		// Since kibana is private, all links in the merged bundle should be hidden
		markdown.Should().Contain("ES Feature");
		markdown.Should().Contain("Kibana Feature");
		// Links should be commented out (% prefix)
		markdown.Should().Contain("%");
	}
}

/// <summary>
/// Tests that merged bundles with only public repos show links.
/// </summary>
public class ChangelogLinksWithMergedPublicReposTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogLinksWithMergedPublicReposTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:::
		""")
	{
		// Add bundles from two public repos with the same target version
		FileSystem.AddFile("docs/changelog/bundles/elasticsearch-2025-08-05.yaml", new MockFileData(
			// language=yaml
			"""
			products:
			- product: elasticsearch
			  target: 2025-08-05
			entries:
			- title: ES Feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 2025-08-05
			  pr: "111111"
			"""));

		FileSystem.AddFile("docs/changelog/bundles/kibana-2025-08-05.yaml", new MockFileData(
			// language=yaml
			"""
			products:
			- product: kibana
			  target: 2025-08-05
			entries:
			- title: Kibana Feature
			  type: feature
			  products:
			  - product: kibana
			    target: 2025-08-05
			  pr: "222222"
			"""));
	}

	public override async ValueTask InitializeAsync()
	{
		await base.InitializeAsync();
		// Only unrelated repos are private - elasticsearch and kibana are public
		Block!.PrivateRepositories.Add("some-other-private-repo");
	}

	[Fact]
	public void ShowsLinksWhenAllMergedReposArePublic()
	{
		var markdown = ChangelogInlineRenderer.RenderChangelogMarkdown(Block!);

		// Both repos are public, so links should be visible
		markdown.Should().Contain("ES Feature");
		markdown.Should().Contain("Kibana Feature");
		// Links should be proper GitHub URLs
		markdown.Should().Contain("[#111111]");
		markdown.Should().Contain("[#222222]");
		markdown.Should().Contain("github.com");
	}
}
