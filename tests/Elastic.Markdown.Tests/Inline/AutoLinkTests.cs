// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using FluentAssertions;
using JetBrains.Annotations;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Tests.Inline;

/// <summary>
/// Base class for autolink tests that expect a LinkInline to be found.
/// </summary>
public abstract class AutoLinkTestBase(ITestOutputHelper output, [LanguageInjection("markdown")] string content)
	: InlineTest<LinkInline>(output, content)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();
}

/// <summary>
/// Base class for autolink tests that expect NO LinkInline to be found.
/// </summary>
public abstract class AutoLinkNotFoundTestBase(ITestOutputHelper output, [LanguageInjection("markdown")] string content)
	: InlineTest(output, content)
{
}

public class BasicAutoLinkTests(ITestOutputHelper output) : AutoLinkTestBase(output,
"""
Check out https://example.com for more info.
"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		Html.Should().Contain(
			"""<a href="https://example.com" target="_blank" rel="noopener noreferrer">https://example.com</a>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkWithPathTests(ITestOutputHelper output) : AutoLinkTestBase(output,
"""
Visit https://example.com/path/to/page for details.
"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		Html.Should().Contain(
			"""<a href="https://example.com/path/to/page" target="_blank" rel="noopener noreferrer">https://example.com/path/to/page</a>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkWithQueryStringTests(ITestOutputHelper output) : AutoLinkTestBase(output,
"""
See https://example.com/search?q=test&page=1 for results.
"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		Html.Should().Contain(
			"""<a href="https://example.com/search?q=test&amp;page=1" target="_blank" rel="noopener noreferrer">https://example.com/search?q=test&amp;page=1</a>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkWithAnchorTests(ITestOutputHelper output) : AutoLinkTestBase(output,
"""
Jump to https://example.com/page#section for the section.
"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		Html.Should().Contain(
			"""<a href="https://example.com/page#section" target="_blank" rel="noopener noreferrer">https://example.com/page#section</a>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkTrailingPeriodTests(ITestOutputHelper output) : AutoLinkTestBase(output,
"""
Check out https://example.com.
"""
)
{
	[Fact]
	public void ExcludesTrailingPeriod() =>
		Html.Should().Contain(
			"""<a href="https://example.com" target="_blank" rel="noopener noreferrer">https://example.com</a>."""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkTrailingCommaTests(ITestOutputHelper output) : AutoLinkTestBase(output,
"""
Visit https://example.com, https://another.com, or https://third.com for info.
"""
)
{
	[Fact]
	public void ExcludesTrailingCommas() =>
		Html.Should().Contain(
			"""<a href="https://example.com" target="_blank" rel="noopener noreferrer">https://example.com</a>,"""
		).And.Contain(
			"""<a href="https://another.com" target="_blank" rel="noopener noreferrer">https://another.com</a>,"""
		).And.Contain(
			"""<a href="https://third.com" target="_blank" rel="noopener noreferrer">https://third.com</a>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkInParenthesesTests(ITestOutputHelper output) : AutoLinkTestBase(output,
"""
See the docs (https://example.com) for details.
"""
)
{
	[Fact]
	public void ExcludesClosingParen() =>
		Html.Should().Contain(
			"""(<a href="https://example.com" target="_blank" rel="noopener noreferrer">https://example.com</a>)"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkWithBalancedParensTests(ITestOutputHelper output) : AutoLinkTestBase(output,
"""
Check https://en.wikipedia.org/wiki/Rust_(programming_language) for more.
"""
)
{
	[Fact]
	public void IncludesBalancedParens() =>
		Html.Should().Contain(
			"""<a href="https://en.wikipedia.org/wiki/Rust_(programming_language)" target="_blank" rel="noopener noreferrer">https://en.wikipedia.org/wiki/Rust_(programming_language)</a>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkElasticDocsHintTests(ITestOutputHelper output) : AutoLinkTestBase(output,
"""
See https://www.elastic.co/docs/deploy-manage for deployment info.
"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		Html.Should().Contain(
			"""<a href="https://www.elastic.co/docs/deploy-manage" target="_blank" rel="noopener noreferrer">https://www.elastic.co/docs/deploy-manage</a>"""
		);

	[Fact]
	public void EmitsHint()
	{
		Collector.Diagnostics.Should().ContainSingle(d =>
			d.Severity == Severity.Hint &&
			d.Message.Contains("elastic.co/docs") &&
			d.Message.Contains("crosslink or relative link")
		);
	}
}

public class AutoLinkInCodeBlockTests(ITestOutputHelper output) : AutoLinkNotFoundTestBase(output,
"""
```
https://example.com/should/not/be/linked
```
"""
)
{
	[Fact]
	public void DoesNotCreateLink() =>
		Html.Should().NotContain("<a href=");

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkInInlineCodeTests(ITestOutputHelper output) : AutoLinkNotFoundTestBase(output,
"""
Use the URL `https://example.com/api` in your config.
"""
)
{
	[Fact]
	public void DoesNotCreateLinkInInlineCode() =>
		Html.Should().Contain("<code>https://example.com/api</code>")
			.And.NotContain("""<a href="https://example.com/api""");

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkDoesNotMatchHttpTests(ITestOutputHelper output) : AutoLinkNotFoundTestBase(output,
"""
This http://example.com should not be autolinked.
"""
)
{
	[Fact]
	public void DoesNotCreateLink() =>
		Html.Should().NotContain("<a href=");

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkWithStandardLinkTests(ITestOutputHelper output) : AutoLinkTestBase(output,
"""
Visit [Example](https://example.com) or https://another.com for more.
"""
)
{
	[Fact]
	public void BothLinksWork() =>
		Html.Should().Contain(
			"""<a href="https://example.com" target="_blank" rel="noopener noreferrer">Example</a>"""
		).And.Contain(
			"""<a href="https://another.com" target="_blank" rel="noopener noreferrer">https://another.com</a>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class MultipleAutoLinksTests(ITestOutputHelper output) : AutoLinkTestBase(output,
"""
First https://first.com then https://second.com and finally https://third.com are all linked.
"""
)
{
	[Fact]
	public void AllLinksAreCreated() =>
		Html.Should().Contain("""<a href="https://first.com""")
			.And.Contain("""<a href="https://second.com""")
			.And.Contain("""<a href="https://third.com""");

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}
