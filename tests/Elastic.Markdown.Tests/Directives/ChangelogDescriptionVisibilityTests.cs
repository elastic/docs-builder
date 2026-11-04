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
	[Test]
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

	[Test]
	public void KeepDescriptions_AlwaysReturnsFalse()
	{
		var result = ChangelogInlineRenderer.ShouldHideEntryDescriptionsForRepo(
			"kibana",
			[],
			ChangelogDescriptionVisibility.KeepDescriptions
		);

		result.Should().BeFalse();
	}

	[Test]
	public void KeepFeatureDescriptions_AlwaysReturnsTrue()
	{
		var result = ChangelogInlineRenderer.ShouldHideEntryDescriptionsForRepo(
			"kibana",
			[],
			ChangelogDescriptionVisibility.KeepFeatureDescriptions
		);

		result.Should().BeTrue();
	}

	[Test]
	public void CombinedFeatureAndHighlightOverlays_AlwaysReturnsTrue()
	{
		var visibility = ChangelogDescriptionVisibility.KeepFeatureDescriptions | ChangelogDescriptionVisibility.KeepHighlightDescriptions;

		var result = ChangelogInlineRenderer.ShouldHideEntryDescriptionsForRepo("kibana", [], visibility);

		result.Should().BeTrue();
	}

	[Test]
	public void AutoPlusFeatureOverlay_WithPublicRepo_HidesDefaultBodies()
	{
		var visibility = ChangelogDescriptionVisibility.Auto | ChangelogDescriptionVisibility.KeepFeatureDescriptions;

		var result = ChangelogInlineRenderer.ShouldHideEntryDescriptionsForRepo("kibana", [], visibility);

		result.Should().BeTrue();
	}

	[Test]
	public void Auto_WithEmptyPrivateRepos_HidesBodies()
	{
		var result = ChangelogInlineRenderer.ShouldHideEntryDescriptionsForRepo("kibana", [], ChangelogDescriptionVisibility.Auto);

		result.Should().BeTrue();
	}

	[Test]
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

	[Test]
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

	[Test]
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

	[Test]
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
[InheritsTests]
public class ChangelogDescriptionVisibilityDefaultTests() : DirectiveTest<ChangelogBlock>("""
	:::{changelog}
	:::
	""")
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

	[Test]
	public void PropertyDefaultsToAuto() => Block!.DescriptionVisibility.Should().Be(ChangelogDescriptionVisibility.Auto);

	/// <summary>Public bundle with no assembler private repos ⇒ auto hides record bodies.</summary>
	[Test]
	public void HtmlOmitsBodyTextForPublicBundle() => Html.Should().NotContain("BODY_DEFAULT_AUTO_VISIBILITY");

	[Test]
	public void HtmlStillRendersTitles() => Html.Should().Contain("Feature delta");
}

[InheritsTests]
public class ChangelogDescriptionVisibilityAutoShowsForPrivateRepoTests() : DirectiveTest<ChangelogBlock>("""
	:::{changelog}
	:::
	""")
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

	public override async Task InitializeAsync()
	{
		await base.InitializeAsync();
		_ = Block!.PrivateRepositories.Add("elasticsearch");
	}

	[Test]
	public void MarkdownIncludesBodyTextWhenRepoIsPrivateForAutoMode()
	{
		var markdown = ChangelogInlineRenderer.RenderChangelogMarkdown(Block!);
		markdown.Should().Contain("BODY_PRIVATE_VISIBILITY_TEST");
	}

	[Test]
	public void MarkdownRendersTitle()
	{
		var markdown = ChangelogInlineRenderer.RenderChangelogMarkdown(Block!);
		markdown.Should().Contain("Feature epsilon");
	}
}

[InheritsTests]
public class ChangelogDescriptionVisibilityKeepExplicitTests() : DirectiveTest<ChangelogBlock>(
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

	[Test]
	public void KeepsBodyOnFullyPublicRepos() => Html.Should().Contain("BODY_KEEP_VISIBILITY");
}

[InheritsTests]
public class ChangelogDescriptionVisibilityHideExplicitTests() : DirectiveTest<ChangelogBlock>(
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

	[Test]
	public void OmitBody() => Html.Should().NotContain("BODY_HIDE_VISIBILITY");

	[Test]
	public void MarkdownRendersTitlesWithoutBodies()
	{
		var markdown = ChangelogInlineRenderer.RenderChangelogMarkdown(Block!);
		markdown.Should().Contain("Feature hide");
		markdown.Should().NotContain("BODY_HIDE_VISIBILITY");
	}
}

[InheritsTests]
public class ChangelogDescriptionVisibilityInvalidTests() : DirectiveTest<ChangelogBlock>(
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

	[Test]
	public void FallsBackToAuto() => Block!.DescriptionVisibility.Should().Be(ChangelogDescriptionVisibility.Auto);

	[Test]
	public void EmitsWarning() => Collector.Warnings.Should().BeGreaterThan(0);

	[Test]
	public void AutoTreatsFullyPublic_AsHideBody() => Html.Should().NotContain("BODY_INVALID_VISIBILITY");
}

[InheritsTests]
public class ChangelogKeepFeatureDescriptionsTests() : DirectiveTest<ChangelogBlock>(
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

	[Test]
	public void ParsesKeepFeatureDescriptions() =>
		Block!.DescriptionVisibility.Should().Be(ChangelogDescriptionVisibility.KeepFeatureDescriptions);

	[Test]
	public void ShowsFeatureBodies() => Html.Should().Contain("FEATURE_ONLY_BODY");

	[Test]
	public void HidesEnhancementBodies() => Html.Should().NotContain("ENHANCEMENT_HIDDEN_BODY");
}

[InheritsTests]
public class ChangelogCombinedFeatureAndHighlightDescriptionsTests() : DirectiveTest<ChangelogBlock>(
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

	[Test]
	public void ParsesBothOverlays()
	{
		Block!.DescriptionVisibility.HasFlag(ChangelogDescriptionVisibility.KeepFeatureDescriptions).Should().BeTrue();
		Block!.DescriptionVisibility.HasFlag(ChangelogDescriptionVisibility.KeepHighlightDescriptions).Should().BeTrue();
	}

	[Test]
	public void ShowsHighlightBodies() => Html.Should().Contain("HIGHLIGHT_SECTION_BODY");

	[Test]
	public void ShowsFeatureBodies() => Html.Should().Contain("FEATURE_SECTION_BODY");

	[Test]
	public void HidesEnhancementBodies() => Html.Should().NotContain("ENHANCEMENT_HIDDEN_BODY");
}

[InheritsTests]
public class ChangelogAutoPlusFeatureDescriptionsTests() : DirectiveTest<ChangelogBlock>(
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

	[Test]
	public void ParsesAutoPlusFeatureOverlay()
	{
		Block!.DescriptionVisibility.HasFlag(ChangelogDescriptionVisibility.Auto).Should().BeTrue();
		Block!.DescriptionVisibility.HasFlag(ChangelogDescriptionVisibility.KeepFeatureDescriptions).Should().BeTrue();
	}

	[Test]
	public void ShowsFeatureBodiesOnPublicRepos() => Html.Should().Contain("FEATURE_AUTO_OVERLAY_BODY");

	[Test]
	public void HidesEnhancementBodiesOnPublicRepos() => Html.Should().NotContain("ENHANCEMENT_AUTO_BODY");
}

[InheritsTests]
public class ChangelogDuplicateBaseDescriptionVisibilityTests() : DirectiveTest<ChangelogBlock>(
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

	[Test]
	public void KeepsFirstBaseToken() => Block!.DescriptionVisibility.Should().Be(ChangelogDescriptionVisibility.KeepDescriptions);

	[Test]
	public void EmitsWarning() => Collector.Warnings.Should().BeGreaterThan(0);

	[Test]
	public void RendersBodiesUsingFirstBase() => Html.Should().Contain("BODY_FIRST_BASE_WINS");
}

[InheritsTests]
public class ChangelogHideDescriptionsPlusFeatureOverlayTests() : DirectiveTest<ChangelogBlock>(
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

	[Test]
	public void ShowsFeatureBodiesDespiteHideBase() => Html.Should().Contain("FEATURE_OVERLAY_WINS");

	[Test]
	public void HidesEnhancementBodies() => Html.Should().NotContain("ENHANCEMENT_STILL_HIDDEN");
}
