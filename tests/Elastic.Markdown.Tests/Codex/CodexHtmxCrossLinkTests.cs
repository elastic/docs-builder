// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Markdown.IO;
using Elastic.Markdown.Tests.Inline;

namespace Elastic.Markdown.Tests.Codex;

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
	public void CrossLink_ProducesPathOnlyHref()
	{
		Html.Should().Contain("href=\"/r/kibana/\"");
		Html.Should().NotContain("https://codex.elastic.dev");
	}

	[Test]
	public void CrossLink_HasNoSelectOobButKeepsPreload()
	{
		Html.Should().NotContain("hx-select-oob");
		Html.Should().Contain("preload=\"mousedown\"");
	}

	[Test]
	public void CrossLink_NoTargetBlank() => Html.Should().NotContain("target=\"_blank\"");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

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
	public void IsolatedCrossLink_HasAbsoluteHref() => Html.Should().Contain("https://codex.elastic.dev/r/kibana/");

	[Test]
	public void IsolatedCrossLink_HasTargetBlank() => Html.Should().Contain("target=\"_blank\"");

	[Test]
	public void IsolatedCrossLink_NoHtmx() => Html.Should().NotContain("hx-select-oob");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}
