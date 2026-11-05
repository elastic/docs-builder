// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Testing;
using Elastic.Markdown.IO;
using Elastic.Markdown.Tests.Inline;
using Nullean.Snapshots;
using Nullean.Snapshots.Html;

namespace Elastic.Markdown.Tests.Codex;

[InheritsTests]
/// <summary>Codex cross-links resolve to path-only URLs; navigation relies on hx-boost targeting #main-container, so links carry no per-link htmx attributes.</summary>
public class CodexHtmxCrossLinkTests() : LinkTestBase("Go to [test](kibana://index.md)")
{
	protected override BuildContext CreateBuildContext(
		TestDiagnosticsCollector collector,
		MockFileSystem fileSystem,
		IConfigurationContext configurationContext
	) =>
		new(collector, TestHelpers.CreateDocumentationFileSystem(fileSystem), configurationContext)
		{
			UrlPathPrefix = "/r/codex-environments",
			BuildType = BuildType.Codex
		};

	protected override ICrossLinkResolver CreateCrossLinkResolver() => new TestCodexCrossLinkResolver(useRelativePaths: true);

	[Test]
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>Go to
			<a href="/r/kibana/" preload="mousedown">test</a>
		</p>
		"""
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
/// <summary>Isolated cross-links resolve to absolute URLs with target=_blank and no htmx.</summary>
public class IsolatedCodexCrossLinkTests() : LinkTestBase("Go to [test](kibana://index.md)")
{
	protected override BuildContext CreateBuildContext(
		TestDiagnosticsCollector collector,
		MockFileSystem fileSystem,
		IConfigurationContext configurationContext
	) =>
		new(collector, TestHelpers.CreateDocumentationFileSystem(fileSystem), configurationContext)
		{
			UrlPathPrefix = "/docs",
			BuildType = BuildType.Isolated
		};

	protected override ICrossLinkResolver CreateCrossLinkResolver() => new TestCodexCrossLinkResolver(useRelativePaths: false);

	[Test]
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>Go to
			<a href="https://codex.elastic.dev/r/kibana/" target="_blank" rel="noopener noreferrer">test</a>
		</p>
		"""
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}
