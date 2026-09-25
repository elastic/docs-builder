// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using JetBrains.Annotations;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Tests.Inline;

[InheritsTests]
public abstract class DirectiveBlockLinkTests([LanguageInjection("markdown")] string content) : InlineTest<LinkInline>(
	$$"""
:::{warning}
:name: caution_ref
This is a 'warning' admonition
:::

{{content}}

"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion = """
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

[InheritsTests]
public class InPageDirectiveLinkTests() : DirectiveBlockLinkTests("""
[Hello](#caution_ref)
""")
{
	[Test]
	public void GeneratesHtml() =>
		// language=html
		Html.ShouldContainHtml("""<p><a href="#caution_ref">Hello</a></p>""");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

[InheritsTests]
public class ExternalDirectiveLinkTests() : DirectiveBlockLinkTests("""
[Sub Requirements](testing/req.md#hint_ref)
""")
{
	[Test]
	public void GeneratesHtml() => Html.ShouldContainHtml("""<p><a href="/docs/testing/req#hint_ref">Sub Requirements</a></p>""");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}
