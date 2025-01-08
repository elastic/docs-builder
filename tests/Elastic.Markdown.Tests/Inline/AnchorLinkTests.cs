// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using JetBrains.Annotations;
using Markdig.Syntax.Inlines;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.Inline;

public abstract class AnchorLinkTestBase(ITestOutputHelper output, [LanguageInjection("markdown")] string content)
	: InlineTest<LinkInline>(output,
$"""
## Hello world

A paragraph

{content}

""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion =
"""
---
title: Special Requirements
---

## Sub Requirements

To follow this tutorial you will need to install the following components:

## New Requirements [new-reqs]

These are new requirements
""";
		fileSystem.AddFile(@"docs/source/testing/req.md", inclusion);
		fileSystem.AddFile(@"docs/source/_static/img/observability.png", new MockFileData(""));
	}

}

public class InPageAnchorTests(ITestOutputHelper output) : AnchorLinkTestBase(output,
"""
[Hello](#hello-world)
"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p><a href="#hello-world">Hello</a></p>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class ExternalPageAnchorTests(ITestOutputHelper output) : AnchorLinkTestBase(output,
"""
[Sub Requirements](testing/req.md#sub-requirements)
"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p><a href="testing/req.html#sub-requirements">Sub Requirements</a></p>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}


public class ExternalPageCustomAnchorTests(ITestOutputHelper output) : AnchorLinkTestBase(output,
"""
[Sub Requirements](testing/req.md#new-reqs)
"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p><a href="testing/req.html#new-reqs">Sub Requirements</a></p>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class ExternalPageAnchorAutoTitleTests(ITestOutputHelper output) : AnchorLinkTestBase(output,
"""
[](testing/req.md#sub-requirements)
"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p><a href="testing/req.html#sub-requirements">Special Requirements &gt; Sub Requirements</a></p>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}


public class InPageBadAnchorTests(ITestOutputHelper output) : AnchorLinkTestBase(output,
"""
[Hello](#hello-world2)
"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p><a href="#hello-world2">Hello</a></p>"""
		);

	[Fact]
	public void HasError() => Collector.Diagnostics.Should().HaveCount(1)
		.And.Contain(d => d.Message.Contains("`hello-world2` does not exist"));
}

public class ExternalPageBadAnchorTests(ITestOutputHelper output) : AnchorLinkTestBase(output,
"""
[Sub Requirements](testing/req.md#sub-requirements2)
"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p><a href="testing/req.html#sub-requirements2">Sub Requirements</a></p>"""
		);

	[Fact]
	public void HasError() => Collector.Diagnostics.Should().HaveCount(1)
		.And.Contain(d => d.Message.Contains("`sub-requirements2` does not exist"));
}
