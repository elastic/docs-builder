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
using Xunit;

namespace Elastic.Markdown.Tests.Assembler;

/// <summary>Tests that assembler builds produce correct HTMX attributes on markdown cross-links (same-site, not target=_blank).</summary>
public class AssemblerHtmxMarkdownLinkTests(ITestOutputHelper output) : LinkTestBase(output, "Go to [test](kibana://index.md)")
{
	protected override BuildContext CreateBuildContext(
		TestDiagnosticsCollector collector,
		MockFileSystem fileSystem,
		IConfigurationContext configurationContext) =>
		new(collector, fileSystem, configurationContext)
		{
			UrlPathPrefix = "/docs/platform/elasticsearch",
			BuildType = BuildType.Assembler
		};

	[Fact]
	public void CrossLink_UsesGranularSwap_ForAssembler() =>
		Html.Should().Contain("hx-select-oob=\"#content-container,#toc-nav,#nav-tree,#nav-dropdown\"");

	[Fact]
	public void CrossLink_HasPreload() =>
		Html.Should().Contain("preload=\"mousedown\"");

	[Fact]
	public void CrossLink_NoTargetBlank() =>
		Html.Should().NotContain("target=\"_blank\"");

	[Fact]
	public void EmitsCrossLink()
	{
		Collector.CrossLinks.Should().HaveCount(1);
		Collector.CrossLinks.Should().Contain("kibana://index.md");
	}

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

/// <summary>Internal links in assembler use #content-container,#toc-nav (same as isolated).</summary>
public class AssemblerHtmxInternalLinkTests(ITestOutputHelper output) : LinkTestBase(output, "[Requirements](testing/req.md)")
{
	protected override BuildContext CreateBuildContext(
		TestDiagnosticsCollector collector,
		MockFileSystem fileSystem,
		IConfigurationContext configurationContext) =>
		new(collector, fileSystem, configurationContext)
		{
			UrlPathPrefix = "/docs/platform/elasticsearch",
			BuildType = BuildType.Assembler
		};

	[Fact]
	public void InternalLink_UsesContentContainerAndTocNav_ForAssembler()
	{
		// Assembler: same-docset links use #content-container,#toc-nav (same as isolated)
		Html.Should().Contain("hx-select-oob=\"#content-container,#toc-nav\"");
	}

	[Fact]
	public void EmitsNoCrossLink() => Collector.CrossLinks.Should().HaveCount(0);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

/// <summary>Absolute path links in assembler get HTMX attributes (granular swap when nav roots not same).</summary>
public class AssemblerHtmxAbsolutePathLinkTests(ITestOutputHelper output) : LinkTestBase(output,
"""
[Elasticsearch](/_static/img/observability.png)
"""
)
{
	protected override BuildContext CreateBuildContext(
		TestDiagnosticsCollector collector,
		MockFileSystem fileSystem,
		IConfigurationContext configurationContext) =>
		new(collector, fileSystem, configurationContext)
		{
			UrlPathPrefix = "/docs",
			BuildType = BuildType.Assembler
		};

	[Fact]
	public void AbsolutePathLink_GetsHtmxAttributes_ForAssembler()
	{
		// Assembler: absolute path links get HTMX (granular swap when hasSameTopLevelGroup is false)
		Html.Should().Contain("hx-select-oob=\"#content-container,#toc-nav,#nav-tree,#nav-dropdown\"");
		Html.Should().Contain("preload=\"mousedown\"");
	}

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

/// <summary>Reference-style internal links in assembler use #content-container,#toc-nav.</summary>
public class AssemblerHtmxReferenceLinkTests(ITestOutputHelper output) : LinkTestBase(output,
"""
[test][test]

[test]: testing/req.md
"""
)
{
	protected override BuildContext CreateBuildContext(
		TestDiagnosticsCollector collector,
		MockFileSystem fileSystem,
		IConfigurationContext configurationContext) =>
		new(collector, fileSystem, configurationContext)
		{
			UrlPathPrefix = "/docs/platform/elasticsearch",
			BuildType = BuildType.Assembler
		};

	[Fact]
	public void ReferenceLink_UsesContentContainerAndTocNav_ForAssembler()
	{
		Html.Should().Contain("hx-select-oob=\"#content-container,#toc-nav\"");
	}

	[Fact]
	public void EmitsNoCrossLink() => Collector.CrossLinks.Should().HaveCount(0);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

/// <summary>Empty-text cross-links in assembler still get granular swap (and emit error).</summary>
public class AssemblerHtmxEmptyTextCrossLinkTests(ITestOutputHelper output) : LinkTestBase(output,
"""

Go to [](kibana://index.md)
"""
)
{
	protected override BuildContext CreateBuildContext(
		TestDiagnosticsCollector collector,
		MockFileSystem fileSystem,
		IConfigurationContext configurationContext) =>
		new(collector, fileSystem, configurationContext)
		{
			UrlPathPrefix = "/docs/platform/elasticsearch",
			BuildType = BuildType.Assembler
		};

	[Fact]
	public void EmptyTextCrossLink_UsesGranularSwap_ForAssembler() =>
		Html.Should().Contain("hx-select-oob=\"#content-container,#toc-nav,#nav-tree,#nav-dropdown\"");

	[Fact]
	public void EmptyTextCrossLink_NoTargetBlank() =>
		Html.Should().NotContain("target=\"_blank\"");

	[Fact]
	public void HasError() =>
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("empty link text"));

	[Fact]
	public void EmitsCrossLink()
	{
		Collector.CrossLinks.Should().HaveCount(1);
		Collector.CrossLinks.Should().Contain("kibana://index.md");
	}
}

/// <summary>Insert-page-title links (empty text, internal target) use #content-container,#toc-nav.</summary>
public class AssemblerHtmxInsertPageTitleTests(ITestOutputHelper output) : LinkTestBase(output,
"""
[](testing/req.md)
"""
)
{
	protected override BuildContext CreateBuildContext(
		TestDiagnosticsCollector collector,
		MockFileSystem fileSystem,
		IConfigurationContext configurationContext) =>
		new(collector, fileSystem, configurationContext)
		{
			UrlPathPrefix = "/docs/platform/elasticsearch",
			BuildType = BuildType.Assembler
		};

	[Fact]
	public void InsertPageTitle_UsesContentContainerAndTocNav_ForAssembler()
	{
		Html.Should().Contain("hx-select-oob=\"#content-container,#toc-nav\"");
	}

	[Fact]
	public void EmitsNoCrossLink() => Collector.CrossLinks.Should().HaveCount(0);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

/// <summary>HTTP links in assembler do NOT get HTMX attributes (target="_blank" instead).</summary>
public class AssemblerHtmxExternalLinkTests(ITestOutputHelper output) : LinkTestBase(output,
"""
[link to app]({{some-url-with-a-version}})
"""
)
{
	protected override BuildContext CreateBuildContext(
		TestDiagnosticsCollector collector,
		MockFileSystem fileSystem,
		IConfigurationContext configurationContext) =>
		new(collector, fileSystem, configurationContext)
		{
			UrlPathPrefix = "/docs/platform/elasticsearch",
			BuildType = BuildType.Assembler
		};

	[Fact]
	public void ExternalLink_DoesNotGetHtmxAttributes_ForAssembler()
	{
		// HTTP links get target="_blank", not hx-select-oob
		Html.Should().Contain("target=\"_blank\"");
		Html.Should().NotContain("hx-select-oob");
	}

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}
