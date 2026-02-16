// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.IO;
using Elastic.Markdown.Tests.Inline;
using FluentAssertions;
using Markdig.Syntax.Inlines;
using Xunit;

namespace Elastic.Markdown.Tests.Codex;

/// <summary>Tests that codex builds produce correct HTMX attributes on markdown links (e.g. #main-container for cross-links).</summary>
public class CodexHtmxMarkdownLinkTests(ITestOutputHelper output) : LinkTestBase(output, "Go to [test](kibana://index.md)")
{
	protected override BuildContext CreateBuildContext(
		TestDiagnosticsCollector collector,
		MockFileSystem fileSystem,
		IConfigurationContext configurationContext) =>
		new(collector, fileSystem, configurationContext)
		{
			UrlPathPrefix = "/internal-docs/r/elasticsearch-docs",
			BuildType = BuildType.Codex,
			SiteRootPath = "/internal-docs/"
		};

	[Fact]
	public void CrossLink_UsesMainContainer_ForCodex()
	{
		// Codex: cross-links (different docset) use #main-container
		Html.Should().Contain("hx-select-oob=\"#main-container\"");
	}

	[Fact]
	public void CrossLink_DoesNotUseDefaultGranularSwap()
	{
		// Default (non-codex) uses #content-container,#toc-nav,#nav-tree,#nav-dropdown for cross-links
		Html.Should().NotContain("hx-select-oob=\"#content-container,#toc-nav,#nav-tree,#nav-dropdown\"");
	}

	[Fact]
	public void EmitsCrossLink()
	{
		Collector.CrossLinks.Should().HaveCount(1);
		Collector.CrossLinks.Should().Contain("kibana://index.md");
	}
}

/// <summary>Internal links in codex use #content-container,#toc-nav (same as default).</summary>
public class CodexHtmxInternalLinkTests(ITestOutputHelper output) : LinkTestBase(output, "[Requirements](testing/req.md)")
{
	protected override BuildContext CreateBuildContext(
		TestDiagnosticsCollector collector,
		MockFileSystem fileSystem,
		IConfigurationContext configurationContext) =>
		new(collector, fileSystem, configurationContext)
		{
			UrlPathPrefix = "/internal-docs/r/elasticsearch-docs",
			BuildType = BuildType.Codex,
			SiteRootPath = "/internal-docs/"
		};

	[Fact]
	public void InternalLink_UsesContentContainerAndTocNav_ForCodex()
	{
		// Codex: same-docset links use #content-container,#toc-nav (same as default)
		Html.Should().Contain("hx-select-oob=\"#content-container,#toc-nav\"");
	}
}
