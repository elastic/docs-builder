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
			ChangelogDescriptionVisibility.HideDescriptions
		);

		result.Should().BeTrue();
	}

	[Fact]
	public void KeepDescriptions_AlwaysReturnsFalse()
	{
		var result = ChangelogInlineRenderer.ShouldHideEntryDescriptionsForRepo(
			"kibana",
			[],
			ChangelogDescriptionVisibility.KeepDescriptions
		);

		result.Should().BeFalse();
	}

	[Fact]
	public void KeepFeatureDescriptions_AlwaysReturnsTrue()
	{
		var result = ChangelogInlineRenderer.ShouldHideEntryDescriptionsForRepo(
			"kibana",
			[],
			ChangelogDescriptionVisibility.KeepFeatureDescriptions
		);

		result.Should().BeTrue();
	}

	[Fact]
	public void CombinedFeatureAndHighlightOverlays_AlwaysReturnsTrue()
	{
		var visibility = ChangelogDescriptionVisibility.KeepFeatureDescriptions | ChangelogDescriptionVisibility.KeepHighlightDescriptions;

		var result = ChangelogInlineRenderer.ShouldHideEntryDescriptionsForRepo("kibana", [], visibility);

		result.Should().BeTrue();
	}

	[Fact]
	public void AutoPlusFeatureOverlay_WithPublicRepo_HidesDefaultBodies()
	{
		var visibility = ChangelogDescriptionVisibility.Auto | ChangelogDescriptionVisibility.KeepFeatureDescriptions;

		var result = ChangelogInlineRenderer.ShouldHideEntryDescriptionsForRepo("kibana", [], visibility);

		result.Should().BeTrue();
	}

	[Fact]
	public void Auto_WithEmptyPrivateRepos_HidesBodies()
	{
		var result = ChangelogInlineRenderer.ShouldHideEntryDescriptionsForRepo("kibana", [], ChangelogDescriptionVisibility.Auto);

		result.Should().BeTrue();
	}

	[Fact]
	public void Auto_WithPublicRepoOnly_HidesBodies()
	{
		var privateRepos = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "secret-repo" };

		var result = ChangelogInlineRenderer.ShouldHideEntryDescriptionsForRepo(
			"kibana",
			privateRepos,
			ChangelogDescriptionVisibility.Auto
		);

		result.Should().BeTrue();
	}

	[Fact]
	public void Auto_WithPrivateRepo_ShowsBodies()
	{
		var privateRepos = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "kibana" };

		var result = ChangelogInlineRenderer.ShouldHideEntryDescriptionsForRepo(
			"kibana",
			privateRepos,
			ChangelogDescriptionVisibility.Auto
		);

		result.Should().BeFalse();
	}

	[Fact]
	public void Auto_WithMergedBundle_OnePrivateConstituent_ShowsBodies()
	{
		var privateRepos = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "kibana" };

		var result = ChangelogInlineRenderer.ShouldHideEntryDescriptionsForRepo(
			"elasticsearch+kibana",
			privateRepos,
			ChangelogDescriptionVisibility.Auto
		);

		result.Should().BeFalse();
	}

	[Fact]
	public void Auto_WithMergedBundle_AllPublicConstituents_HidesBodies()
	{
		var privateRepos = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "other-private" };

		var result = ChangelogInlineRenderer.ShouldHideEntryDescriptionsForRepo(
			"elasticsearch+kibana",
			privateRepos,
			ChangelogDescriptionVisibility.Auto
		);

		result.Should().BeTrue();
	}
}

/// <summary>
/// Omitting :description-visibility: defaults to <see cref="ChangelogDescriptionVisibility.Auto"/>.
/// </summary>
public class ChangelogDescriptionVisibilityDefaultTests(ITestOutputHelper output) : DirectiveTest<ChangelogBlock>(
	output,
	"""
	:::{changelog}
	:::
	"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
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
			"""
			)
		);

	[Fact]
	public void PropertyDefaultsToAuto() => Block!.DescriptionVisibility.Should().Be(ChangelogDescriptionVisibility.Auto);

	/// <summary>Public bundle with no assembler private repos ⇒ auto hides record bodies.</summary>
	[Fact]
	public void HtmlOmitsBodyTextForPublicBundle() => Html.Should().NotContain("BODY_DEFAULT_AUTO_VISIBILITY");

	[Fact]
	public void HtmlStillRendersTitles() => Html.Should().Contain("Feature delta");
}

public class ChangelogDescriptionVisibilityAutoShowsForPrivateRepoTests(ITestOutputHelper output) : DirectiveTest<ChangelogBlock>(
	output,
	"""
	:::{changelog}
	:::
	"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
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
			"""
			)
		);

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

public class ChangelogDescriptionVisibilityKeepExplicitTests(ITestOutputHelper output) : DirectiveTest<ChangelogBlock>(
	output,
	"""
	:::{changelog}
	:description-visibility: keep-descriptions
	:::
	"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
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
			"""
			)
		);

	[Fact]
	public void KeepsBodyOnFullyPublicRepos() => Html.Should().Contain("BODY_KEEP_VISIBILITY");
}

public class ChangelogDescriptionVisibilityHideExplicitTests(ITestOutputHelper output) : DirectiveTest<ChangelogBlock>(
	output,
	"""
	:::{changelog}
	:description-visibility: hide-descriptions
	:::
	"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
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
			"""
			)
		);

	[Fact]
	public void OmitBody() => Html.Should().NotContain("BODY_HIDE_VISIBILITY");

	[Fact]
	public void MarkdownRendersTitlesWithoutBodies()
	{
		var markdown = ChangelogInlineRenderer.RenderChangelogMarkdown(Block!);
		markdown.Should().Contain("Feature hide");
		markdown.Should().NotContain("BODY_HIDE_VISIBILITY");
	}
}

public class ChangelogDescriptionVisibilityInvalidTests(ITestOutputHelper output) : DirectiveTest<ChangelogBlock>(
	output,
	"""
	:::{changelog}
	:description-visibility: nonsense-value
	:::
	"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
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
			"""
			)
		);

	[Fact]
	public void FallsBackToAuto() => Block!.DescriptionVisibility.Should().Be(ChangelogDescriptionVisibility.Auto);

	[Fact]
	public void EmitsWarning() => Collector.Warnings.Should().BeGreaterThan(0);

	[Fact]
	public void AutoTreatsFullyPublic_AsHideBody() => Html.Should().NotContain("BODY_INVALID_VISIBILITY");
}

public class ChangelogKeepFeatureDescriptionsTests(ITestOutputHelper output) : DirectiveTest<ChangelogBlock>(
	output,
	"""
	:::{changelog}
	:description-visibility: keep-feature-descriptions
	:::
	"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Feature with body
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  description: FEATURE_ONLY_BODY
			- title: Enhancement with body
			  type: enhancement
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  description: ENHANCEMENT_HIDDEN_BODY
			"""
			)
		);

	[Fact]
	public void ParsesKeepFeatureDescriptions() =>
		Block!.DescriptionVisibility.Should().Be(ChangelogDescriptionVisibility.KeepFeatureDescriptions);

	[Fact]
	public void ShowsFeatureBodies() => Html.Should().Contain("FEATURE_ONLY_BODY");

	[Fact]
	public void HidesEnhancementBodies() => Html.Should().NotContain("ENHANCEMENT_HIDDEN_BODY");
}

public class ChangelogCombinedFeatureAndHighlightDescriptionsTests(ITestOutputHelper output) : DirectiveTest<ChangelogBlock>(
	output,
	"""
	:::{changelog}
	:highlights:
	:description-visibility: keep-feature-descriptions, keep-highlight-descriptions
	:::
	"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Highlighted feature
			  type: feature
			  highlight: true
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  description: HIGHLIGHT_SECTION_BODY
			- title: Regular feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  description: FEATURE_SECTION_BODY
			- title: Enhancement with body
			  type: enhancement
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  description: ENHANCEMENT_HIDDEN_BODY
			"""
			)
		);

	[Fact]
	public void ParsesBothOverlays()
	{
		Block!.DescriptionVisibility.HasFlag(ChangelogDescriptionVisibility.KeepFeatureDescriptions).Should().BeTrue();
		Block!.DescriptionVisibility.HasFlag(ChangelogDescriptionVisibility.KeepHighlightDescriptions).Should().BeTrue();
	}

	[Fact]
	public void ShowsHighlightBodies() => Html.Should().Contain("HIGHLIGHT_SECTION_BODY");

	[Fact]
	public void ShowsFeatureBodies() => Html.Should().Contain("FEATURE_SECTION_BODY");

	[Fact]
	public void HidesEnhancementBodies() => Html.Should().NotContain("ENHANCEMENT_HIDDEN_BODY");
}

public class ChangelogAutoPlusFeatureDescriptionsTests(ITestOutputHelper output) : DirectiveTest<ChangelogBlock>(
	output,
	"""
	:::{changelog}
	:description-visibility: auto, keep-feature-descriptions
	:::
	"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Feature with body
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  description: FEATURE_AUTO_OVERLAY_BODY
			- title: Enhancement with body
			  type: enhancement
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  description: ENHANCEMENT_AUTO_BODY
			"""
			)
		);

	[Fact]
	public void ParsesAutoPlusFeatureOverlay()
	{
		Block!.DescriptionVisibility.HasFlag(ChangelogDescriptionVisibility.Auto).Should().BeTrue();
		Block!.DescriptionVisibility.HasFlag(ChangelogDescriptionVisibility.KeepFeatureDescriptions).Should().BeTrue();
	}

	[Fact]
	public void ShowsFeatureBodiesOnPublicRepos() => Html.Should().Contain("FEATURE_AUTO_OVERLAY_BODY");

	[Fact]
	public void HidesEnhancementBodiesOnPublicRepos() => Html.Should().NotContain("ENHANCEMENT_AUTO_BODY");
}

public class ChangelogDuplicateBaseDescriptionVisibilityTests(ITestOutputHelper output) : DirectiveTest<ChangelogBlock>(
	output,
	"""
	:::{changelog}
	:description-visibility: keep-descriptions, hide-descriptions
	:::
	"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Feature keep first
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  description: BODY_FIRST_BASE_WINS
			"""
			)
		);

	[Fact]
	public void KeepsFirstBaseToken() => Block!.DescriptionVisibility.Should().Be(ChangelogDescriptionVisibility.KeepDescriptions);

	[Fact]
	public void EmitsWarning() => Collector.Warnings.Should().BeGreaterThan(0);

	[Fact]
	public void RendersBodiesUsingFirstBase() => Html.Should().Contain("BODY_FIRST_BASE_WINS");
}

public class ChangelogHideDescriptionsPlusFeatureOverlayTests(ITestOutputHelper output) : DirectiveTest<ChangelogBlock>(
	output,
	"""
	:::{changelog}
	:description-visibility: hide-descriptions, keep-feature-descriptions
	:::
	"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Feature with body
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  description: FEATURE_OVERLAY_WINS
			- title: Enhancement with body
			  type: enhancement
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  description: ENHANCEMENT_STILL_HIDDEN
			"""
			)
		);

	[Fact]
	public void ShowsFeatureBodiesDespiteHideBase() => Html.Should().Contain("FEATURE_OVERLAY_WINS");

	[Fact]
	public void HidesEnhancementBodies() => Html.Should().NotContain("ENHANCEMENT_STILL_HIDDEN");
}
