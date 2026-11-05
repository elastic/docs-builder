// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Testing;
using Elastic.Markdown.Myst.Directives.Changelog;
using Nullean.Snapshots.Html;

namespace Elastic.Markdown.Tests.Directives;

/// <summary>
/// Prestage visibility filtering (release-notes onboarding RFC, B1): CDN-mode <c>{changelog}</c>
/// hides bundles targeting versions newer than the versioning system's current release when the
/// build publishes the <c>current</c> content source (production), keeps them on <c>next</c>
/// (staging), and never filters local/isolated builds or products without a semver versioning
/// system. The test versions configuration pins stack current to 8.0.0.
/// </summary>
[InheritsTests]
public abstract class ChangelogVersionVisibilityTestBase() : DirectiveTest<ChangelogBlock>(
	// language=markdown
	"""
	:::{changelog}
	:cdn: elasticsearch
	:::
	"""
)
{
	protected const string ReleasedTitle = "Released change";
	protected const string UnreleasedTitle = "Unreleased staged change";

	protected override IReleaseNotesResolver GetReleaseNotesResolver() =>
		ChangelogCdnTestResolver.For(
			"elasticsearch",
			("7.9.0.yaml",
			// language=yaml
			"""
				products:
				- product: elasticsearch
				  target: 7.9.0
				  repo: elasticsearch
				  owner: elastic
				entries:
				- title: Released change
				  type: enhancement
				  products:
				  - product: elasticsearch
				    target: 7.9.0
				  prs:
				  - "100"
				"""),
			("8.1.0.yaml",
			// language=yaml
			"""
				products:
				- product: elasticsearch
				  target: 8.1.0
				  repo: elasticsearch
				  owner: elastic
				entries:
				- title: Unreleased staged change
				  type: enhancement
				  products:
				  - product: elasticsearch
				    target: 8.1.0
				  prs:
				  - "200"
				""")
		);
}

[InheritsTests]
public class ChangelogVisibilityOnProductionTests() : ChangelogVersionVisibilityTestBase()
{
	protected override ContentSource? GetContentSource() => ContentSource.Current;

	[Test]
	public void HidesBundlesTargetingUnreleasedVersions() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="heading-wrapper" id="7.9.0">
			<h2>
				<a class="headerlink" href="#7.9.0">7.9.0</a>
			</h2>
		</div>
		<div class="heading-wrapper" id="elasticsearch-7.9.0-features-enhancements">
			<h3>
				<a class="headerlink" href="#elasticsearch-7.9.0-features-enhancements">Features and enhancements</a>
			</h3>
		</div>
		<ul>
			<li>Released change.
				<a href="https://github.com/elastic/elasticsearch/pull/100">#100</a>
			</li>
		</ul>
		"""
		);

	[Test]
	public void EmitsHintForHiddenBundle() =>
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Hint && d.Message.Contains("elasticsearch 8.1.0"));
}

[InheritsTests]
public class ChangelogVisibilityOnStagingTests() : ChangelogVersionVisibilityTestBase()
{
	protected override ContentSource? GetContentSource() => ContentSource.Next;

	[Test]
	public void ShowsUnreleasedBundlesForPreReleaseReview() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="heading-wrapper" id="8.1.0">
			<h2>
				<a class="headerlink" href="#8.1.0">8.1.0</a>
			</h2>
		</div>
		<div class="heading-wrapper" id="elasticsearch-8.1.0-features-enhancements">
			<h3>
				<a class="headerlink" href="#elasticsearch-8.1.0-features-enhancements">Features and enhancements</a>
			</h3>
		</div>
		<ul>
			<li>Unreleased staged change.
				<a href="https://github.com/elastic/elasticsearch/pull/200">#200</a>
			</li>
		</ul>
		<div class="heading-wrapper" id="7.9.0">
			<h2>
				<a class="headerlink" href="#7.9.0">7.9.0</a>
			</h2>
		</div>
		<div class="heading-wrapper" id="elasticsearch-7.9.0-features-enhancements">
			<h3>
				<a class="headerlink" href="#elasticsearch-7.9.0-features-enhancements">Features and enhancements</a>
			</h3>
		</div>
		<ul>
			<li>Released change.
				<a href="https://github.com/elastic/elasticsearch/pull/100">#100</a>
			</li>
		</ul>
		"""
		);
}

[InheritsTests]
public class ChangelogVisibilityOnIsolatedBuildTests() : ChangelogVersionVisibilityTestBase()
{
	// No override: isolated/local builds have no content source and render everything.

	[Test]
	public void ShowsAllBundles() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="heading-wrapper" id="8.1.0">
			<h2>
				<a class="headerlink" href="#8.1.0">8.1.0</a>
			</h2>
		</div>
		<div class="heading-wrapper" id="elasticsearch-8.1.0-features-enhancements">
			<h3>
				<a class="headerlink" href="#elasticsearch-8.1.0-features-enhancements">Features and enhancements</a>
			</h3>
		</div>
		<ul>
			<li>Unreleased staged change.
				<a href="https://github.com/elastic/elasticsearch/pull/200">#200</a>
			</li>
		</ul>
		<div class="heading-wrapper" id="7.9.0">
			<h2>
				<a class="headerlink" href="#7.9.0">7.9.0</a>
			</h2>
		</div>
		<div class="heading-wrapper" id="elasticsearch-7.9.0-features-enhancements">
			<h3>
				<a class="headerlink" href="#elasticsearch-7.9.0-features-enhancements">Features and enhancements</a>
			</h3>
		</div>
		<ul>
			<li>Released change.
				<a href="https://github.com/elastic/elasticsearch/pull/100">#100</a>
			</li>
		</ul>
		"""
		);
}

/// <summary>
/// Products without a registered semver versioning system (date-promotion products, products not in
/// products.yml) are never filtered — their targets are dates or unknown schemes, not stack versions.
/// </summary>
[InheritsTests]
public class ChangelogVisibilityUnversionedProductTests() : DirectiveTest<ChangelogBlock>(
	// language=markdown
	"""
	:::{changelog}
	:cdn: cdn-visibility-unversioned
	:::
	"""
)
{
	protected override ContentSource? GetContentSource() => ContentSource.Current;

	protected override IReleaseNotesResolver GetReleaseNotesResolver() =>
		ChangelogCdnTestResolver.For(
			"cdn-visibility-unversioned",
			("9.9.0.yaml",
			// language=yaml
			"""
				products:
				- product: cdn-visibility-unversioned
				  target: 9.9.0
				  repo: widget
				  owner: elastic
				entries:
				- title: Future-looking change
				  type: enhancement
				  products:
				  - product: cdn-visibility-unversioned
				    target: 9.9.0
				  prs:
				  - "300"
				""")
		);

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="heading-wrapper" id="9.9.0">
			<h2>
				<a class="headerlink" href="#9.9.0">9.9.0</a>
			</h2>
		</div>
		<div class="heading-wrapper" id="widget-9.9.0-features-enhancements">
			<h3>
				<a class="headerlink" href="#widget-9.9.0-features-enhancements">Features and enhancements</a>
			</h3>
		</div>
		<ul>
			<li>Future-looking change.
				<a href="https://github.com/elastic/widget/pull/300">#300</a>
			</li>
		</ul>
		"""
		);
}
