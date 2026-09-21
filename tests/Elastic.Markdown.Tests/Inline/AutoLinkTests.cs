// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using JetBrains.Annotations;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Tests.Inline;

/// <summary>
/// Base class for autolink tests that expect a LinkInline to be found.
/// </summary>
public abstract class AutoLinkTestBase([LanguageInjection("markdown")] string content) : InlineTest<LinkInline>(content)
{
	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();
}

/// <summary>
/// Base class for autolink tests that expect NO LinkInline to be found.
/// </summary>
public abstract class AutoLinkNotFoundTestBase([LanguageInjection("markdown")] string content) : InlineTest(content)
{
}

public class BasicAutoLinkTests() : AutoLinkTestBase("""
Check out https://docs.test.io for more info.
""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.Should().Contain("""<a href="https://docs.test.io" target="_blank" rel="noopener noreferrer">https://docs.test.io</a>""");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkWithPathTests() : AutoLinkTestBase("""
Visit https://docs.test.io/path/to/page for details.
""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.Should().Contain(
			"""<a href="https://docs.test.io/path/to/page" target="_blank" rel="noopener noreferrer">https://docs.test.io/path/to/page</a>"""
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkWithQueryStringTests() : AutoLinkTestBase("""
See https://docs.test.io/search?q=test&page=1 for results.
""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.Should().Contain(
			"""<a href="https://docs.test.io/search?q=test&amp;page=1" target="_blank" rel="noopener noreferrer">https://docs.test.io/search?q=test&amp;page=1</a>"""
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkWithAnchorTests() : AutoLinkTestBase("""
Jump to https://docs.test.io/page#section for the section.
""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.Should().Contain(
			"""<a href="https://docs.test.io/page#section" target="_blank" rel="noopener noreferrer">https://docs.test.io/page#section</a>"""
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkTrailingPeriodTests() : AutoLinkTestBase("""
Check out https://docs.test.io.
""")
{
	[Test]
	public void ExcludesTrailingPeriod() =>
		Html.Should().Contain("""<a href="https://docs.test.io" target="_blank" rel="noopener noreferrer">https://docs.test.io</a>.""");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkTrailingCommaTests() : AutoLinkTestBase(
	"""
Visit https://first.test.io, https://second.test.io, or https://third.test.io for info.
"""
)
{
	[Test]
	public void ExcludesTrailingCommas() =>
		Html
			.Should()
			.Contain("""<a href="https://first.test.io" target="_blank" rel="noopener noreferrer">https://first.test.io</a>,""")
			.And
			.Contain("""<a href="https://second.test.io" target="_blank" rel="noopener noreferrer">https://second.test.io</a>,""")
			.And
			.Contain("""<a href="https://third.test.io" target="_blank" rel="noopener noreferrer">https://third.test.io</a>""");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkInParenthesesTests() : AutoLinkTestBase("""
See the docs (https://docs.test.io) for details.
""")
{
	[Test]
	public void ExcludesClosingParen() =>
		Html.Should().Contain("""(<a href="https://docs.test.io" target="_blank" rel="noopener noreferrer">https://docs.test.io</a>)""");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkWithBalancedParensTests() : AutoLinkTestBase(
	"""
Check https://en.wikipedia.org/wiki/Rust_(programming_language) for more.
"""
)
{
	[Test]
	public void IncludesBalancedParens() =>
		Html.Should().Contain(
			"""<a href="https://en.wikipedia.org/wiki/Rust_(programming_language)" target="_blank" rel="noopener noreferrer">https://en.wikipedia.org/wiki/Rust_(programming_language)</a>"""
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkElasticDocsHintTests() : AutoLinkTestBase("""
See https://www.elastic.co/docs/deploy-manage for deployment info.
""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.Should().Contain(
			"""<a href="https://www.elastic.co/docs/deploy-manage" target="_blank" rel="noopener noreferrer">https://www.elastic.co/docs/deploy-manage</a>"""
		);

	[Test]
	public void EmitsHint() =>
		Collector
			.Diagnostics
			.Should()
			.ContainSingle(
				d => d.Severity == Severity.Hint && d.Message.Contains("elastic.co/docs") && d.Message.Contains(
					"crosslink or relative link"
				)
			);
}

public class AutoLinkInCodeBlockTests() : AutoLinkNotFoundTestBase("""
```
https://docs.test.io/should/not/be/linked
```
""")
{
	[Test]
	public void DoesNotCreateLink() => Html.Should().NotContain("<a href=");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkInInlineCodeTests() : AutoLinkNotFoundTestBase("""
Use the URL `https://docs.test.io/api` in your config.
""")
{
	[Test]
	public void DoesNotCreateLinkInInlineCode() =>
		Html.Should().Contain("<code>https://docs.test.io/api</code>").And.NotContain("""<a href="https://docs.test.io/api""");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkDoesNotMatchHttpTests() : AutoLinkNotFoundTestBase("""
This http://docs.test.io should not be autolinked.
""")
{
	[Test]
	public void DoesNotCreateLink() => Html.Should().NotContain("<a href=");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkWithStandardLinkTests() : AutoLinkTestBase(
	"""
Visit [Docs](https://docs.test.io) or https://other.test.io for more.
"""
)
{
	[Test]
	public void BothLinksWork() =>
		Html
			.Should()
			.Contain("""<a href="https://docs.test.io" target="_blank" rel="noopener noreferrer">Docs</a>""")
			.And
			.Contain("""<a href="https://other.test.io" target="_blank" rel="noopener noreferrer">https://other.test.io</a>""");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

// Regression test for elastic/docs-builder#3317: no nested <a> when a URL is the link text.
public class AutoLinkInsideLinkTextTests() : AutoLinkTestBase(
	"""
Upload to a service like [https://gist.github.com](https://gist.github.com).
"""
)
{
	[Test]
	public void DoesNotCreateNestedAnchor() =>
		Html
			.Should()
			.Contain("""<a href="https://gist.github.com" target="_blank" rel="noopener noreferrer">https://gist.github.com</a>""")
			.And
			.NotMatchRegex(@"<a\b[^>]*><a\b");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkInsideLinkTextWithSurroundingTextTests() : AutoLinkTestBase(
	"""
See [the page at https://example.test.io for details](https://docs.test.io).
"""
)
{
	[Test]
	public void DoesNotAutolinkUrlInsideLinkText() =>
		Html.Should().Contain(
			"""<a href="https://docs.test.io" target="_blank" rel="noopener noreferrer">the page at https://example.test.io for details</a>"""
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

// Verify that image-inside-link is unaffected by the IsNestedInsideLink guard (images bypass it via the IsImage branch).
public class ImageInsideLinkTests() : InlineTest<LinkInline>("""
[![alt text](https://example.com/image.png)](https://example.com)
""")
{
	[Test]
	public void RendersOuterAnchor() =>
		Html.Should().Contain("""<a href="https://example.com" target="_blank" rel="noopener noreferrer">""");

	[Test]
	public void RendersImage() => Html.Should().Contain("<img src=\"https://example.com/image.png\"");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class MultipleAutoLinksTests() : AutoLinkTestBase(
	"""
First https://first.com then https://second.com and finally https://third.com are all linked.
"""
)
{
	[Test]
	public void AllLinksAreCreated() =>
		Html
			.Should()
			.Contain("""<a href="https://first.com""")
			.And
			.Contain("""<a href="https://second.com""")
			.And
			.Contain("""<a href="https://third.com""");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

// === Exclusion rule tests ===

public class AutoLinkWithPortExclusionTests() : AutoLinkNotFoundTestBase("""
Connect to https://www.elastic.co:443/guide for the guide.
""")
{
	[Test]
	public void DoesNotCreateLinkForUrlWithPort() => Html.Should().NotContain("<a href=").And.Contain("https://www.elastic.co:443/guide");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkLocalhostExclusionTests() : AutoLinkNotFoundTestBase("""
Check https://localhost/api for the local API.
""")
{
	[Test]
	public void DoesNotCreateLinkForLocalhost() => Html.Should().NotContain("<a href=").And.Contain("https://localhost/api");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkLoopbackExclusionTests() : AutoLinkNotFoundTestBase("""
Test at https://127.0.0.1/health for health check.
""")
{
	[Test]
	public void DoesNotCreateLinkForLoopback() => Html.Should().NotContain("<a href=").And.Contain("https://127.0.0.1/health");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkExampleDomainExclusionTests() : AutoLinkNotFoundTestBase("""
See https://example.com/docs for examples.
""")
{
	[Test]
	public void DoesNotCreateLinkForExampleDomain() => Html.Should().NotContain("<a href=").And.Contain("https://example.com/docs");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkExampleSubdomainExclusionTests() : AutoLinkNotFoundTestBase("""
Visit https://system.example.com/setup for setup.
""")
{
	[Test]
	public void DoesNotCreateLinkForExampleSubdomain() =>
		Html.Should().NotContain("<a href=").And.Contain("https://system.example.com/setup");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkTemplatePlaceholderExclusionTests() : AutoLinkNotFoundTestBase(
	"""
Use https://{{cluster_id}}.es.test.co/api for your cluster.
"""
)
{
	[Test]
	public void DoesNotCreateLinkForTemplatePlaceholder() => Html.Should().NotContain("<a href=").And.Contain("https://");

	// Note: We expect an error because {{cluster_id}} is an undefined substitution key,
	// but the important assertion is that the URL is not autolinked.
}

public class AutoLinkAsciiDocStyleExclusionTests() : AutoLinkNotFoundTestBase(
	"""
See https://www.iana.org/assignments[IANA for assignments.
"""
)
{
	[Test]
	public void DoesNotCreateLinkForAsciiDocStyle() => Html.Should().NotContain("""<a href="https://www.iana.org/assignments[IANA""");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AutoLinkValidUrlStillWorksTests() : AutoLinkTestBase("""
Check https://www.elastic.co/guide for docs.
""")
{
	[Test]
	public void CreatesLinkForValidUrl() =>
		Html.Should().Contain(
			"""<a href="https://www.elastic.co/guide" target="_blank" rel="noopener noreferrer">https://www.elastic.co/guide</a>"""
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}
