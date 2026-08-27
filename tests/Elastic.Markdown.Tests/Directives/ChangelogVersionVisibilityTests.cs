// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Myst.Directives.Changelog;

namespace Elastic.Markdown.Tests.Directives;

/// <summary>
/// Prestage visibility filtering (release-notes onboarding RFC, B1): CDN-mode <c>{changelog}</c>
/// hides bundles targeting versions newer than the versioning system's current release when the
/// build publishes the <c>current</c> content source (production), keeps them on <c>next</c>
/// (staging), and never filters local/isolated builds or products without a semver versioning
/// system. The test versions configuration pins stack current to 8.0.0.
/// </summary>
public abstract class ChangelogVersionVisibilityTestBase(ITestOutputHelper output) : DirectiveTest<ChangelogBlock>(output,
	// language=markdown
	"""
	:::{changelog}
	:cdn: elasticsearch
	:::
	""")
{
	protected const string ReleasedTitle = "Released change";
	protected const string UnreleasedTitle = "Unreleased staged change";

	protected override IReleaseNotesResolver GetReleaseNotesResolver() =>
		ChangelogCdnTestResolver.For("elasticsearch",
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
				"""));
}

public class ChangelogVisibilityOnProductionTests(ITestOutputHelper output) : ChangelogVersionVisibilityTestBase(output)
{
	protected override ContentSource? GetContentSource() => ContentSource.Current;

	[Fact]
	public void HidesBundlesTargetingUnreleasedVersions()
	{
		Html.Should().Contain(ReleasedTitle);
		Html.Should().NotContain(UnreleasedTitle, "8.1.0 is newer than the current release (8.0.0) and production publishes 'current'");
	}

	[Fact]
	public void EmitsHintForHiddenBundle() =>
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Hint && d.Message.Contains("elasticsearch 8.1.0"));
}

public class ChangelogVisibilityOnStagingTests(ITestOutputHelper output) : ChangelogVersionVisibilityTestBase(output)
{
	protected override ContentSource? GetContentSource() => ContentSource.Next;

	[Fact]
	public void ShowsUnreleasedBundlesForPreReleaseReview()
	{
		Html.Should().Contain(ReleasedTitle);
		Html.Should().Contain(UnreleasedTitle, "staging publishes 'next' so staged bundles are reviewable before release day");
	}
}

public class ChangelogVisibilityOnIsolatedBuildTests(ITestOutputHelper output) : ChangelogVersionVisibilityTestBase(output)
{
	// No override: isolated/local builds have no content source and render everything.

	[Fact]
	public void ShowsAllBundles()
	{
		Html.Should().Contain(ReleasedTitle);
		Html.Should().Contain(UnreleasedTitle);
	}
}

/// <summary>
/// Products without a registered semver versioning system (date-promotion products, products not in
/// products.yml) are never filtered — their targets are dates or unknown schemes, not stack versions.
/// </summary>
public class ChangelogVisibilityUnversionedProductTests(ITestOutputHelper output) : DirectiveTest<ChangelogBlock>(output,
	// language=markdown
	"""
	:::{changelog}
	:cdn: cdn-visibility-unversioned
	:::
	""")
{
	protected override ContentSource? GetContentSource() => ContentSource.Current;

	protected override IReleaseNotesResolver GetReleaseNotesResolver() =>
		ChangelogCdnTestResolver.For("cdn-visibility-unversioned",
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
				"""));

	[Fact]
	public void DoesNotFilterProductsWithoutVersioningSystem() =>
		Html.Should().Contain("Future-looking change");
}
