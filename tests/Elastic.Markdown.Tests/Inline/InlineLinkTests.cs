// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using JetBrains.Annotations;
using Markdig.Syntax.Inlines;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.Inline;

public abstract class LinkTestBase(ITestOutputHelper output, [LanguageInjection("markdown")] string content)
	: InlineTest<LinkInline>(output, content)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion =
"""
---
title: Special Requirements
---

To follow this tutorial you will need to install the following components:
""";
		fileSystem.AddFile(@"docs/source/elastic/search-labs/search/req.md", inclusion);
		fileSystem.AddFile(@"docs/source/_static/img/observability.png", new MockFileData(""));
	}

}

public class InlineLinkTests(ITestOutputHelper output) : LinkTestBase(output,
"""
[Elasticsearch](/_static/img/observability.png)
"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p><a href="/_static/img/observability.png">Elasticsearch</a></p>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class LinkToPageTests(ITestOutputHelper output) : LinkTestBase(output,
"""
[Requirements](elastic/search-labs/search/req.md)
"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p><a href="elastic/search-labs/search/req.html">Requirements</a></p>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class InsertPageTitleTests(ITestOutputHelper output) : LinkTestBase(output,
"""
[](elastic/search-labs/search/req.md)
"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p><a href="elastic/search-labs/search/req.html">Special Requirements</a></p>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}
