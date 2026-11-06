// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Testing;
using Elastic.Markdown.Myst.Directives.Changelog;
using Markdig.Syntax;
using Nullean.Snapshots;
using Nullean.Snapshots.Html;

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
	public void KeepHighlightDescriptions_AlwaysReturnsTrue()
	{
		// Default path hides descriptions; Highlights section overrides separately in the renderer.
		var result = ChangelogInlineRenderer.ShouldHideEntryDescriptionsForRepo(
			"kibana",
			[],
			ChangelogDescriptionVisibility.KeepHighlightDescriptions
		);

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
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="heading-wrapper" id="9.3.0">
			<h2>
				<a class="headerlink" href="#9.3.0">9.3.0</a>
			</h2>
		</div>
		<div class="heading-wrapper" id="elasticsearch-9.3.0-features-enhancements">
			<h3>
				<a class="headerlink" href="#elasticsearch-9.3.0-features-enhancements">Features and enhancements</a>
			</h3>
		</div>
		<ul>
			<li>Feature delta.</li>
		</ul>
		"""
		);
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

	protected override void OnFixtureBuilt(MarkdownDocument document)
	{
		var block = document.Descendants<ChangelogBlock>().First();
		block.PrivateRepositories.Add("elasticsearch");
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
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="heading-wrapper" id="9.3.0">
			<h2>
				<a class="headerlink" href="#9.3.0">9.3.0</a>
			</h2>
		</div>
		<div class="heading-wrapper" id="elasticsearch-9.3.0-features-enhancements">
			<h3>
				<a class="headerlink" href="#elasticsearch-9.3.0-features-enhancements">Features and enhancements</a>
			</h3>
		</div>
		<ul>
			<li>
				<p>Feature keep.</p>
				<p>BODY_KEEP_VISIBILITY</p>
			</li>
		</ul>
		"""
		);
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
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="heading-wrapper" id="9.3.0">
			<h2>
				<a class="headerlink" href="#9.3.0">9.3.0</a>
			</h2>
		</div>
		<div class="heading-wrapper" id="elasticsearch-9.3.0-features-enhancements">
			<h3>
				<a class="headerlink" href="#elasticsearch-9.3.0-features-enhancements">Features and enhancements</a>
			</h3>
		</div>
		<ul>
			<li>Feature hide.</li>
		</ul>
		"""
		);

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
	public void EmitsWarning() =>
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"warning {repo}/docs/index.md:3 Invalid :description-visibility: value 'nonsense-value'. Valid values are: auto, keep-descriptions, keep-highlight-descriptions, hide-descriptions. Using auto."
			);

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="heading-wrapper" id="9.3.0">
			<h2>
				<a class="headerlink" href="#9.3.0">9.3.0</a>
			</h2>
		</div>
		<div class="heading-wrapper" id="elasticsearch-9.3.0-features-enhancements">
			<h3>
				<a class="headerlink" href="#elasticsearch-9.3.0-features-enhancements">Features and enhancements</a>
			</h3>
		</div>
		<ul>
			<li>Feature warn.</li>
		</ul>
		"""
		);
}
