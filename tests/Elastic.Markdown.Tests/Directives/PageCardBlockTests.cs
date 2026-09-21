// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Navigation;
using Elastic.Markdown.Myst.Directives.PageCard;

namespace Elastic.Markdown.Tests.Directives;

/// <summary>
/// Verifies that page-card links resolve through the navigation lookup so that assembled
/// builds (where a docset lives at a path_prefix beyond UrlPathPrefix) get correct URLs.
/// The navigation lookup mirrors what DiagnosticLinkInlineParser.UpdateLinkUrl does for
/// ordinary inline links.
/// </summary>
public class PageCardRelativeMdLinkTests() : DirectiveTest<PageCardBlock>("""
	:::{page-card} [Other page](./other-page.md)
	:::
	""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile("docs/other-page.md", new MockFileData("# Other Page\n\nSome content."));

	[Test]
	public void ResolvesUrlViaNavigationLookup() => Block!.ResolvedUrl.Should().Be("/other-page");

	[Test]
	public void EmitsNoErrors() => Collector.Diagnostics.Should().NotContain(d => d.Severity == Severity.Error);
}

/// <summary>
/// Simulates an assembled build where navigation.yml rehomes the docset at a path_prefix.
/// The docset-relative path alone would yield /other-page; only the navigation lookup knows
/// about the rehomed prefix. This is the case that broke CLI reference links on the live site.
/// </summary>
public class PageCardRehomedDocsetLinkTests() : DirectiveTest<PageCardBlock>(
	"""
	:::{page-card} [Other page](./other-page.md)
	:::
	"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile("docs/other-page.md", new MockFileData("# Other Page\n\nSome content."));

	public override async Task InitializeAsync()
	{
		((INavigationHomeAccessor)Set.Navigation).HomeProvider = new NavigationHomeProvider("/reference/rehomed", Set.Navigation);
		await base.InitializeAsync();
	}

	[Test]
	public void ResolvesUrlWithRehomedPrefix() => Block!.ResolvedUrl.Should().Be("/reference/rehomed/other-page");

	[Test]
	public void EmitsNoErrors() => Collector.Diagnostics.Should().NotContain(d => d.Severity == Severity.Error);
}

/// <summary>
/// An anchored link must still resolve through the navigation lookup: the anchor is not part of
/// the file path, so it has to be split off before probing and re-appended afterwards.
/// </summary>
public class PageCardRehomedAnchoredLinkTests() : DirectiveTest<PageCardBlock>(
	"""
	:::{page-card} [Install](./other-page.md#install)
	:::
	"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile("docs/other-page.md", new MockFileData("# Other Page\n\n## Install\n\nContent."));

	public override async Task InitializeAsync()
	{
		((INavigationHomeAccessor)Set.Navigation).HomeProvider = new NavigationHomeProvider("/reference/rehomed", Set.Navigation);
		await base.InitializeAsync();
	}

	[Test]
	public void ResolvesUrlWithRehomedPrefixAndAnchor() => Block!.ResolvedUrl.Should().Be("/reference/rehomed/other-page#install");

	[Test]
	public void EmitsNoErrors() => Collector.Diagnostics.Should().NotContain(d => d.Severity == Severity.Error);
}

/// <summary>Namespace-style link (no .md extension) probes for /index.md variant.</summary>
public class PageCardRelativeFolderLinkTests() : DirectiveTest<PageCardBlock>("""
	:::{page-card} [Sub section](./subdir)
	:::
	""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile("docs/subdir/index.md", new MockFileData("# Sub Section\n\nContent."));

	[Test]
	public void ResolvesIndexPageUrl() => Block!.ResolvedUrl.Should().Be("/subdir");

	[Test]
	public void EmitsNoErrors() => Collector.Diagnostics.Should().NotContain(d => d.Severity == Severity.Error);
}

/// <summary>Absolute HTTP URLs are rejected with an error.</summary>
public class PageCardAbsoluteUrlErrorTests() : DirectiveTest<PageCardBlock>(
	"""
	:::{page-card} [External](https://example.com/page)
	:::
	"""
)
{
	[Test]
	public void EmitsErrorForAbsoluteUrl() =>
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("absolute URL"));
}
