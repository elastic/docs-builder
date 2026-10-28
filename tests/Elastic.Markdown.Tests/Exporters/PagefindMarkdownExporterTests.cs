// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Exporters.Pagefind;

namespace Elastic.Markdown.Tests.Exporters;

public class PagefindMarkdownExporterTests
{
	[Test]
	public void ResolveTitle_MetaTitle_OverridesPageTitle() =>
		PagefindMarkdownExporter
			.ResolveTitle("Search across clusters", "Cross-cluster search", "/docs/ccs")
			.Should()
			.Be("Search across clusters");

	[Test]
	[Arguments(null)]
	[Arguments("")]
	[Arguments("  ")]
	public void ResolveTitle_AbsentOrBlankMetaTitle_UsesPageTitle(string? metaTitle) =>
		PagefindMarkdownExporter.ResolveTitle(metaTitle, "Cross-cluster search", "/docs/ccs").Should().Be("Cross-cluster search");

	[Test]
	public void ResolveTitle_NoTitles_UsesUrl() => PagefindMarkdownExporter.ResolveTitle(null, null, "/docs/ccs").Should().Be("/docs/ccs");
}
