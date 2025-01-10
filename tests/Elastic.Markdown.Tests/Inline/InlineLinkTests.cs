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
		fileSystem.AddFile(@"docs/source/testing/req.md", inclusion);
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
[Requirements](testing/req.md)
"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p><a href="testing/req.html">Requirements</a></p>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class InsertPageTitleTests(ITestOutputHelper output) : LinkTestBase(output,
"""
[](testing/req.md)
"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p><a href="testing/req.html">Special Requirements</a></p>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class LinkReferenceTest(ITestOutputHelper output) : LinkTestBase(output,
	"""
	[test][test]

	[test]: testing/req.md
	"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p><a href="testing/req.html">test</a></p>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class CrossLinkReferenceTest(ITestOutputHelper output) : LinkTestBase(output,
	"""
	[test][test]

	[test]: kibana://index.md
	"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			// TODO: The link is not rendered correctly yet, will be fixed in a follow-up
			"""<p><a href="kibana://index.html">test</a></p>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);

	[Fact]
	public void EmitsCrossLink()
	{
		Collector.CrossLinks.Should().HaveCount(1);
		Collector.CrossLinks.Should().ContainKey("kibana://index.md");
	}
}

public class CrossLinkTest(ITestOutputHelper output) : LinkTestBase(output,
	"""
	[test](kibana://index.md)
	"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			// TODO: The link is not rendered correctly yet, will be fixed in a follow-up
			"""<p><a href="kibana://index.html">test</a></p>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);

	[Fact]
	public void EmitsCrossLink()
	{
		Collector.CrossLinks.Should().HaveCount(1);
		Collector.CrossLinks.Should().ContainKey("kibana://index.md");
	}
}

public class DuplicateCrossLinkTest(ITestOutputHelper output) : LinkTestBase(output,
	"""
	[a](kibana://index.md)
	[b](kibana://index.md)
	[c](elasticsearch://index.md)
	"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			// TODO: The link is not rendered correctly yet, will be fixed in a follow-up
			"""
			<p><a href="kibana://index.html">a</a><br />
			<a href="kibana://index.html">b</a><br />
			<a href="elasticsearch://index.html">c</a></p>
			"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);

	[Fact]
	public void EmitsCrossLink()
	{
		Collector.CrossLinks.Should().HaveCount(2);
		Collector.CrossLinks.Should().ContainKey("kibana://index.md");
		Collector.CrossLinks.Should().ContainKey("elasticsearch://index.md");
	}
}

