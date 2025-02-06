// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using JetBrains.Annotations;
using Markdig.Syntax.Inlines;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.Inline;

public abstract class DirectiveBlockLinkTests(ITestOutputHelper output, [LanguageInjection("markdown")] string content)
	: InlineTest<LinkInline>(output,
$$"""
:::{warning}
:name: caution_ref
This is a 'warning' admonition
:::

{{content}}

""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion =
"""
# Special Requirements

:::{important}
:name: hint_ref
This is an 'important' admonition
:::
""";
		fileSystem.AddFile(@"docs/testing/req.md", inclusion);
		fileSystem.AddFile(@"docs/_static/img/observability.png", new MockFileData(""));
	}

}

public class InPageDirectiveLinkTests(ITestOutputHelper output) : DirectiveBlockLinkTests(output,
"""
[Hello](#caution_ref)
"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p><a href="#caution_ref">Hello</a></p>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class ExternalDirectiveLinkTests(ITestOutputHelper output) : DirectiveBlockLinkTests(output,
"""
[Sub Requirements](testing/req.md#hint_ref)
"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p><a href="/testing/req.html#hint_ref">Sub Requirements</a></p>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}
