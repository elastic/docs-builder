// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.IO;
using Elastic.Markdown.Tests.Inline;

namespace Elastic.Markdown.Tests.Assembler;

/// <summary>
/// Navigation relies on hx-boost targeting #main-container, so markdown links must
/// not carry per-link htmx attributes. Cross-links stay same-site (no target=_blank).
/// </summary>
public class AssemblerHtmxMarkdownLinkTests() : LinkTestBase("Go to [test](kibana://index.md)")
{
	protected override BuildContext CreateBuildContext(
		TestDiagnosticsCollector collector,
		MockFileSystem fileSystem,
		IConfigurationContext configurationContext
	) =>
		new(collector, TestHelpers.CreateDocumentationFileSystem(fileSystem), configurationContext)
		{
			UrlPathPrefix = "/docs/platform/elasticsearch",
			BuildType = BuildType.Assembler
		};

	[Test]
	public void CrossLink_HasNoSelectOobButKeepsPreload()
	{
		Html.Should().NotContain("hx-select-oob");
		Html.Should().Contain("preload=\"mousedown\"");
	}

	[Test]
	public void CrossLink_NoTargetBlank() => Html.Should().NotContain("target=\"_blank\"");

	[Test]
	public void EmitsCrossLink()
	{
		Collector.CrossLinks.Should().HaveCount(1);
		Collector.CrossLinks.Should().Contain("kibana://index.md");
	}

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

/// <summary>Internal links in assembler carry no per-link htmx attributes.</summary>
public class AssemblerHtmxInternalLinkTests() : LinkTestBase("[Requirements](testing/req.md)")
{
	protected override BuildContext CreateBuildContext(
		TestDiagnosticsCollector collector,
		MockFileSystem fileSystem,
		IConfigurationContext configurationContext
	) =>
		new(collector, TestHelpers.CreateDocumentationFileSystem(fileSystem), configurationContext)
		{
			UrlPathPrefix = "/docs/platform/elasticsearch",
			BuildType = BuildType.Assembler
		};

	[Test]
	public void InternalLink_HasNoPerLinkHtmxAttributes() => Html.Should().NotContain("hx-select-oob");

	[Test]
	public void EmitsNoCrossLink() => Collector.CrossLinks.Should().HaveCount(0);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

/// <summary>Absolute path links in assembler carry no per-link htmx attributes.</summary>
public class AssemblerHtmxAbsolutePathLinkTests() : LinkTestBase("""
[Elasticsearch](/_static/img/observability.png)
""")
{
	protected override BuildContext CreateBuildContext(
		TestDiagnosticsCollector collector,
		MockFileSystem fileSystem,
		IConfigurationContext configurationContext
	) =>
		new(collector, TestHelpers.CreateDocumentationFileSystem(fileSystem), configurationContext)
		{
			UrlPathPrefix = "/docs",
			BuildType = BuildType.Assembler
		};

	[Test]
	public void AbsolutePathLink_HasNoSelectOobButKeepsPreload()
	{
		Html.Should().NotContain("hx-select-oob");
		Html.Should().Contain("preload=\"mousedown\"");
	}

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

/// <summary>Reference-style internal links in assembler carry no per-link htmx attributes.</summary>
public class AssemblerHtmxReferenceLinkTests() : LinkTestBase("""
[test][test]

[test]: testing/req.md
""")
{
	protected override BuildContext CreateBuildContext(
		TestDiagnosticsCollector collector,
		MockFileSystem fileSystem,
		IConfigurationContext configurationContext
	) =>
		new(collector, TestHelpers.CreateDocumentationFileSystem(fileSystem), configurationContext)
		{
			UrlPathPrefix = "/docs/platform/elasticsearch",
			BuildType = BuildType.Assembler
		};

	[Test]
	public void ReferenceLink_HasNoPerLinkHtmxAttributes() => Html.Should().NotContain("hx-select-oob");

	[Test]
	public void EmitsNoCrossLink() => Collector.CrossLinks.Should().HaveCount(0);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

/// <summary>Empty-text cross-links in assembler carry no per-link htmx attributes (and emit error).</summary>
public class AssemblerHtmxEmptyTextCrossLinkTests() : LinkTestBase("""

Go to [](kibana://index.md)
""")
{
	protected override BuildContext CreateBuildContext(
		TestDiagnosticsCollector collector,
		MockFileSystem fileSystem,
		IConfigurationContext configurationContext
	) =>
		new(collector, TestHelpers.CreateDocumentationFileSystem(fileSystem), configurationContext)
		{
			UrlPathPrefix = "/docs/platform/elasticsearch",
			BuildType = BuildType.Assembler
		};

	[Test]
	public void EmptyTextCrossLink_HasNoPerLinkHtmxAttributes() => Html.Should().NotContain("hx-select-oob");

	[Test]
	public void EmptyTextCrossLink_NoTargetBlank() => Html.Should().NotContain("target=\"_blank\"");

	[Test]
	public void HasError() =>
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("empty link text"));

	[Test]
	public void EmitsCrossLink()
	{
		Collector.CrossLinks.Should().HaveCount(1);
		Collector.CrossLinks.Should().Contain("kibana://index.md");
	}
}

/// <summary>Insert-page-title links (empty text, internal target) carry no per-link htmx attributes.</summary>
public class AssemblerHtmxInsertPageTitleTests() : LinkTestBase("""
[](testing/req.md)
""")
{
	protected override BuildContext CreateBuildContext(
		TestDiagnosticsCollector collector,
		MockFileSystem fileSystem,
		IConfigurationContext configurationContext
	) =>
		new(collector, TestHelpers.CreateDocumentationFileSystem(fileSystem), configurationContext)
		{
			UrlPathPrefix = "/docs/platform/elasticsearch",
			BuildType = BuildType.Assembler
		};

	[Test]
	public void InsertPageTitle_HasNoPerLinkHtmxAttributes() => Html.Should().NotContain("hx-select-oob");

	[Test]
	public void EmitsNoCrossLink() => Collector.CrossLinks.Should().HaveCount(0);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

/// <summary>HTTP links in assembler get target="_blank" and no htmx attributes.</summary>
public class AssemblerHtmxExternalLinkTests() : LinkTestBase("""
[link to app]({{some-url-with-a-version}})
""")
{
	protected override BuildContext CreateBuildContext(
		TestDiagnosticsCollector collector,
		MockFileSystem fileSystem,
		IConfigurationContext configurationContext
	) =>
		new(collector, TestHelpers.CreateDocumentationFileSystem(fileSystem), configurationContext)
		{
			UrlPathPrefix = "/docs/platform/elasticsearch",
			BuildType = BuildType.Assembler
		};

	[Test]
	public void ExternalLink_DoesNotGetHtmxAttributes_ForAssembler()
	{
		// HTTP links get target="_blank", not hx-select-oob
		Html.Should().Contain("target=\"_blank\"");
		Html.Should().NotContain("hx-select-oob");
	}

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}
