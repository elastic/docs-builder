// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Markdown.IO;
using Elastic.Markdown.Tests.Inline;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Codex;

/// <summary>Codex cross-links resolve to path-only URLs with htmx attributes including codex-breadcrumbs.</summary>
public class CodexHtmxCrossLinkTests(ITestOutputHelper output) : LinkTestBase(output, "Go to [test](kibana://index.md)")
{
	protected override BuildContext CreateBuildContext(
		TestDiagnosticsCollector collector,
		MockFileSystem fileSystem,
		IConfigurationContext configurationContext) =>
		new(collector, fileSystem, configurationContext)
		{
			UrlPathPrefix = "/r/codex-environments",
			BuildType = BuildType.Codex
		};

	protected override ICrossLinkResolver CreateCrossLinkResolver() =>
		new TestCodexCrossLinkResolver(useRelativePaths: true);

	[Fact]
	public void CrossLink_ProducesPathOnlyHref()
	{
		Html.Should().Contain("href=\"/r/kibana/\"");
		Html.Should().NotContain("https://codex.elastic.dev");
	}

	[Fact]
	public void CrossLink_HasHtmxWithCodexBreadcrumbs() =>
		Html.Should().Contain("#codex-breadcrumbs");

	[Fact]
	public void CrossLink_HasHtmxSelectOob() =>
		Html.Should().Contain("hx-select-oob=");

	[Fact]
	public void CrossLink_HasPreload() =>
		Html.Should().Contain("preload=\"mousedown\"");

	[Fact]
	public void CrossLink_NoTargetBlank() =>
		Html.Should().NotContain("target=\"_blank\"");

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

/// <summary>Isolated cross-links resolve to absolute URLs with target=_blank and no htmx.</summary>
public class IsolatedCodexCrossLinkTests(ITestOutputHelper output) : LinkTestBase(output, "Go to [test](kibana://index.md)")
{
	protected override BuildContext CreateBuildContext(
		TestDiagnosticsCollector collector,
		MockFileSystem fileSystem,
		IConfigurationContext configurationContext) =>
		new(collector, fileSystem, configurationContext)
		{
			UrlPathPrefix = "/docs",
			BuildType = BuildType.Isolated
		};

	protected override ICrossLinkResolver CreateCrossLinkResolver() =>
		new TestCodexCrossLinkResolver(useRelativePaths: false);

	[Fact]
	public void IsolatedCrossLink_HasAbsoluteHref() =>
		Html.Should().Contain("https://codex.elastic.dev/r/kibana/");

	[Fact]
	public void IsolatedCrossLink_HasTargetBlank() =>
		Html.Should().Contain("target=\"_blank\"");

	[Fact]
	public void IsolatedCrossLink_NoHtmx() =>
		Html.Should().NotContain("hx-select-oob");

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}
