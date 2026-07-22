// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Markdown.Myst.Directives.Changelog;

namespace Elastic.Markdown.Tests.Directives;

/// <summary>Unit tests for <see cref="ChangelogInlineRenderer.ShouldHideEntryDescriptionsForRepo"/>.</summary>
public class ChangelogShouldHideEntryDescriptionsTests
{
	[Fact]
	public void HideDescriptions_AlwaysReturnsTrue()
	{
		var privateRepos = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "x" };

		var result = ChangelogInlineRenderer.ShouldHideEntryDescriptionsForRepo(
			"kibana",
			privateRepos,
			ChangelogDescriptionVisibility.HideDescriptions);

		result.Should().BeTrue();
	}

	[Fact]
	public void KeepDescriptions_AlwaysReturnsFalse()
	{
		var result = ChangelogInlineRenderer.ShouldHideEntryDescriptionsForRepo(
			"kibana",
			[],
			ChangelogDescriptionVisibility.KeepDescriptions);

		result.Should().BeFalse();
	}

	[Fact]
	public void Auto_WithEmptyPrivateRepos_HidesBodies()
	{
		var result = ChangelogInlineRenderer.ShouldHideEntryDescriptionsForRepo(
			"kibana",
			[],
			ChangelogDescriptionVisibility.Auto);

		result.Should().BeTrue();
	}

	[Fact]
	public void Auto_WithPublicRepoOnly_HidesBodies()
	{
		var privateRepos = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "secret-repo" };

		var result = ChangelogInlineRenderer.ShouldHideEntryDescriptionsForRepo(
			"kibana",
			privateRepos,
			ChangelogDescriptionVisibility.Auto);

		result.Should().BeTrue();
	}

	[Fact]
	public void Auto_WithPrivateRepo_ShowsBodies()
	{
		var privateRepos = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "kibana" };

		var result = ChangelogInlineRenderer.ShouldHideEntryDescriptionsForRepo(
			"kibana",
			privateRepos,
			ChangelogDescriptionVisibility.Auto);

		result.Should().BeFalse();
	}

	[Fact]
	public void Auto_WithMergedBundle_OnePrivateConstituent_ShowsBodies()
	{
		var privateRepos = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "kibana" };

		var result = ChangelogInlineRenderer.ShouldHideEntryDescriptionsForRepo(
			"elasticsearch+kibana",
			privateRepos,
			ChangelogDescriptionVisibility.Auto);

		result.Should().BeFalse();
	}

	[Fact]
	public void Auto_WithMergedBundle_AllPublicConstituents_HidesBodies()
	{
		var privateRepos = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "other-private" };

		var result = ChangelogInlineRenderer.ShouldHideEntryDescriptionsForRepo(
			"elasticsearch+kibana",
			privateRepos,
			ChangelogDescriptionVisibility.Auto);

		result.Should().BeTrue();
	}
}

/// <summary>
/// Omitting :description-visibility: defaults to <see cref="ChangelogDescriptionVisibility.Auto"/>.
/// </summary>
public class ChangelogDescriptionVisibilityDefaultTests(ITestOutputHelper output) : DirectiveTest<ChangelogBlock>(output,
	"""
	:::{changelog}
	:::
	""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
			"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Feature delta
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  description: BODY_DEFAULT_AUTO_VISIBILITY
			"""));

	[Fact]
	public void PropertyDefaultsToAuto() =>
		Block!.DescriptionVisibility.Should().Be(ChangelogDescriptionVisibility.Auto);

	/// <summary>Public bundle with no assembler private repos ⇒ auto hides record bodies.</summary>
	[Fact]
	public void HtmlOmitsBodyTextForPublicBundle() =>
		Html.Should().NotContain("BODY_DEFAULT_AUTO_VISIBILITY");

	[Fact]
	public void HtmlStillRendersTitles() =>
		Html.Should().Contain("Feature delta");
}

public class ChangelogDescriptionVisibilityAutoShowsForPrivateRepoTests(ITestOutputHelper output) : DirectiveTest<ChangelogBlock>(output,
	"""
	:::{changelog}
	:::
	""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
			"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Feature epsilon
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  description: BODY_PRIVATE_VISIBILITY_TEST
			"""));

	public override async ValueTask InitializeAsync()
	{
		await base.InitializeAsync();
		_ = Block!.PrivateRepositories.Add("elasticsearch");
	}

	[Fact]
	public void MarkdownIncludesBodyTextWhenRepoIsPrivateForAutoMode()
	{
		var markdown = ChangelogInlineRenderer.RenderChangelogMarkdown(Block!);
		markdown.Should().Contain("BODY_PRIVATE_VISIBILITY_TEST");
	}

	[Fact]
	public void MarkdownRendersTitle()
	{
		var markdown = ChangelogInlineRenderer.RenderChangelogMarkdown(Block!);
		markdown.Should().Contain("Feature epsilon");
	}
}

public class ChangelogDescriptionVisibilityKeepExplicitTests(ITestOutputHelper output) : DirectiveTest<ChangelogBlock>(output,
	"""
	:::{changelog}
	:description-visibility: keep-descriptions
	:::
	""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
			"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Feature keep
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  description: BODY_KEEP_VISIBILITY
			"""));

	[Fact]
	public void KeepsBodyOnFullyPublicRepos() =>
		Html.Should().Contain("BODY_KEEP_VISIBILITY");
}

public class ChangelogDescriptionVisibilityHideExplicitTests(ITestOutputHelper output) : DirectiveTest<ChangelogBlock>(output,
	"""
	:::{changelog}
	:description-visibility: hide-descriptions
	:::
	""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
			"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Feature hide
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  description: BODY_HIDE_VISIBILITY
			"""));

	[Fact]
	public void OmitBody() =>
		Html.Should().NotContain("BODY_HIDE_VISIBILITY");

	[Fact]
	public void MarkdownRendersTitlesWithoutBodies()
	{
		var markdown = ChangelogInlineRenderer.RenderChangelogMarkdown(Block!);
		markdown.Should().Contain("Feature hide");
		markdown.Should().NotContain("BODY_HIDE_VISIBILITY");
	}
}

public class ChangelogDescriptionVisibilityInvalidTests(ITestOutputHelper output) : DirectiveTest<ChangelogBlock>(output,
	"""
	:::{changelog}
	:description-visibility: nonsense-value
	:::
	""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
			"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Feature warn
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  description: BODY_INVALID_VISIBILITY
			"""));

	[Fact]
	public void FallsBackToAuto() =>
		Block!.DescriptionVisibility.Should().Be(ChangelogDescriptionVisibility.Auto);

	[Fact]
	public void EmitsWarning() =>
		Collector.Warnings.Should().BeGreaterThan(0);

	[Fact]
	public void AutoTreatsFullyPublic_AsHideBody() =>
		Html.Should().NotContain("BODY_INVALID_VISIBILITY");
}
