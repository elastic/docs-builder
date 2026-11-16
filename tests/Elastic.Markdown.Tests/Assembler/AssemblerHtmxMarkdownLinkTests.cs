// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Testing;
using Elastic.Markdown.IO;
using Elastic.Markdown.Tests.Inline;
using Nullean.Snapshots;
using Nullean.Snapshots.Html;

namespace Elastic.Markdown.Tests.Assembler;

[InheritsTests]
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
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>Go to
			<a href="https://docs-v3-preview.elastic.dev/elastic/kibana/tree/main/" preload="mousedown">test</a>
		</p>
		"""
		);

	[Test]
	public void EmitsCrossLink()
	{
		Collector.CrossLinks.Should().HaveCount(1);
		Collector.CrossLinks.Should().Contain("kibana://index.md");
	}

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
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
	public void InternalLink_HasNoPerLinkHtmxAttributes() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>
			<a href="/docs/platform/elasticsearch/testing/req" preload="mousedown">Requirements</a>
		</p>
		"""
		);

	[Test]
	public void EmitsNoCrossLink() => Collector.CrossLinks.Should().HaveCount(0);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
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
	public void AbsolutePathLink_HasNoSelectOobButKeepsPreload() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>
			<a href="/docs/_static/img/observability.png" preload="mousedown">Elasticsearch</a>
		</p>
		"""
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
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
	public void ReferenceLink_HasNoPerLinkHtmxAttributes() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>
			<a href="/docs/platform/elasticsearch/testing/req" preload="mousedown">test</a>
		</p>
		"""
		);

	[Test]
	public void EmitsNoCrossLink() => Collector.CrossLinks.Should().HaveCount(0);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
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
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>Go to
			<a href="https://docs-v3-preview.elastic.dev/elastic/kibana/tree/main/" preload="mousedown"></a>
		</p>
		"""
		);

	[Test]
	public void HasError() =>
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error {repo}/docs/index.md:4 Crosslink 'kibana://index.md' has empty link text. Please provide explicit link text."
			);

	[Test]
	public void EmitsCrossLink()
	{
		Collector.CrossLinks.Should().HaveCount(1);
		Collector.CrossLinks.Should().Contain("kibana://index.md");
	}
}

[InheritsTests]
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
	public void InsertPageTitle_HasNoPerLinkHtmxAttributes() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>
			<a href="/docs/platform/elasticsearch/testing/req" preload="mousedown">Special Requirements</a>
		</p>
		"""
		);

	[Test]
	public void EmitsNoCrossLink() => Collector.CrossLinks.Should().HaveCount(0);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
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
	// HTTP links get target="_blank", not hx-select-oob
	public void ExternalLink_DoesNotGetHtmxAttributes_ForAssembler() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>
			<a href="https://github.com/elastic/fake-repo/tree/v1.17.0" target="_blank" rel="noopener noreferrer">link to app</a>
		</p>
		"""
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}
