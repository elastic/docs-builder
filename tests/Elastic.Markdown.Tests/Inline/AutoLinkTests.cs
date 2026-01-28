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
Check out https://docs.test.io for more info.
"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		Html.Should().Contain(
			"""<a href="https://docs.test.io" target="_blank" rel="noopener noreferrer">https://docs.test.io</a>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkWithPathTests(ITestOutputHelper output) : AutoLinkTestBase(output,
"""
Visit https://docs.test.io/path/to/page for details.
"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		Html.Should().Contain(
			"""<a href="https://docs.test.io/path/to/page" target="_blank" rel="noopener noreferrer">https://docs.test.io/path/to/page</a>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkWithQueryStringTests(ITestOutputHelper output) : AutoLinkTestBase(output,
"""
See https://docs.test.io/search?q=test&page=1 for results.
"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		Html.Should().Contain(
			"""<a href="https://docs.test.io/search?q=test&amp;page=1" target="_blank" rel="noopener noreferrer">https://docs.test.io/search?q=test&amp;page=1</a>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkWithAnchorTests(ITestOutputHelper output) : AutoLinkTestBase(output,
"""
Jump to https://docs.test.io/page#section for the section.
"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		Html.Should().Contain(
			"""<a href="https://docs.test.io/page#section" target="_blank" rel="noopener noreferrer">https://docs.test.io/page#section</a>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkTrailingPeriodTests(ITestOutputHelper output) : AutoLinkTestBase(output,
"""
Check out https://docs.test.io.
"""
)
{
	[Fact]
	public void ExcludesTrailingPeriod() =>
		Html.Should().Contain(
			"""<a href="https://docs.test.io" target="_blank" rel="noopener noreferrer">https://docs.test.io</a>."""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkTrailingCommaTests(ITestOutputHelper output) : AutoLinkTestBase(output,
"""
Visit https://first.test.io, https://second.test.io, or https://third.test.io for info.
"""
)
{
	[Fact]
	public void ExcludesTrailingCommas() =>
		Html.Should().Contain(
			"""<a href="https://first.test.io" target="_blank" rel="noopener noreferrer">https://first.test.io</a>,"""
		).And.Contain(
			"""<a href="https://second.test.io" target="_blank" rel="noopener noreferrer">https://second.test.io</a>,"""
		).And.Contain(
			"""<a href="https://third.test.io" target="_blank" rel="noopener noreferrer">https://third.test.io</a>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkInParenthesesTests(ITestOutputHelper output) : AutoLinkTestBase(output,
"""
See the docs (https://docs.test.io) for details.
"""
)
{
	[Fact]
	public void ExcludesClosingParen() =>
		Html.Should().Contain(
			"""(<a href="https://docs.test.io" target="_blank" rel="noopener noreferrer">https://docs.test.io</a>)"""
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
https://docs.test.io/should/not/be/linked
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
Use the URL `https://docs.test.io/api` in your config.
"""
)
{
	[Fact]
	public void DoesNotCreateLinkInInlineCode() =>
		Html.Should().Contain("<code>https://docs.test.io/api</code>")
			.And.NotContain("""<a href="https://docs.test.io/api""");

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkDoesNotMatchHttpTests(ITestOutputHelper output) : AutoLinkNotFoundTestBase(output,
"""
This http://docs.test.io should not be autolinked.
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
Visit [Docs](https://docs.test.io) or https://other.test.io for more.
"""
)
{
	[Fact]
	public void BothLinksWork() =>
		Html.Should().Contain(
			"""<a href="https://docs.test.io" target="_blank" rel="noopener noreferrer">Docs</a>"""
		).And.Contain(
			"""<a href="https://other.test.io" target="_blank" rel="noopener noreferrer">https://other.test.io</a>"""
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

// === Exclusion rule tests ===

public class AutoLinkWithPortExclusionTests(ITestOutputHelper output) : AutoLinkNotFoundTestBase(output,
"""
Connect to https://www.elastic.co:443/guide for the guide.
"""
)
{
	[Fact]
	public void DoesNotCreateLinkForUrlWithPort() =>
		Html.Should().NotContain("<a href=")
			.And.Contain("https://www.elastic.co:443/guide");

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkLocalhostExclusionTests(ITestOutputHelper output) : AutoLinkNotFoundTestBase(output,
"""
Check https://localhost/api for the local API.
"""
)
{
	[Fact]
	public void DoesNotCreateLinkForLocalhost() =>
		Html.Should().NotContain("<a href=")
			.And.Contain("https://localhost/api");

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkLoopbackExclusionTests(ITestOutputHelper output) : AutoLinkNotFoundTestBase(output,
"""
Test at https://127.0.0.1/health for health check.
"""
)
{
	[Fact]
	public void DoesNotCreateLinkForLoopback() =>
		Html.Should().NotContain("<a href=")
			.And.Contain("https://127.0.0.1/health");

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkExampleDomainExclusionTests(ITestOutputHelper output) : AutoLinkNotFoundTestBase(output,
"""
See https://example.com/docs for examples.
"""
)
{
	[Fact]
	public void DoesNotCreateLinkForExampleDomain() =>
		Html.Should().NotContain("<a href=")
			.And.Contain("https://example.com/docs");

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkExampleSubdomainExclusionTests(ITestOutputHelper output) : AutoLinkNotFoundTestBase(output,
"""
Visit https://system.example.com/setup for setup.
"""
)
{
	[Fact]
	public void DoesNotCreateLinkForExampleSubdomain() =>
		Html.Should().NotContain("<a href=")
			.And.Contain("https://system.example.com/setup");

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkTemplatePlaceholderExclusionTests(ITestOutputHelper output) : AutoLinkNotFoundTestBase(output,
"""
Use https://{{cluster_id}}.es.test.co/api for your cluster.
"""
)
{
	[Fact]
	public void DoesNotCreateLinkForTemplatePlaceholder() =>
		Html.Should().NotContain("<a href=")
			.And.Contain("https://");

	// Note: We expect an error because {{cluster_id}} is an undefined substitution key,
	// but the important assertion is that the URL is not autolinked.
}

public class AutoLinkAsciiDocStyleExclusionTests(ITestOutputHelper output) : AutoLinkNotFoundTestBase(output,
"""
See https://www.iana.org/assignments[IANA for assignments.
"""
)
{
	[Fact]
	public void DoesNotCreateLinkForAsciiDocStyle() =>
		Html.Should().NotContain("""<a href="https://www.iana.org/assignments[IANA""");

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkValidUrlStillWorksTests(ITestOutputHelper output) : AutoLinkTestBase(output,
"""
Check https://www.elastic.co/guide for docs.
"""
)
{
	[Fact]
	public void CreatesLinkForValidUrl() =>
		Html.Should().Contain(
			"""<a href="https://www.elastic.co/guide" target="_blank" rel="noopener noreferrer">https://www.elastic.co/guide</a>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}
