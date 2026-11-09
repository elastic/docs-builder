// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Testing;
using JetBrains.Annotations;
using Markdig.Syntax.Inlines;
using Nullean.Snapshots;
using Nullean.Snapshots.Html;

namespace Elastic.Markdown.Tests.Inline;

/// <summary>
/// Base class for autolink tests that expect a LinkInline to be found.
/// </summary>
[InheritsTests]
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

[InheritsTests]
public class BasicAutoLinkTests() : AutoLinkTestBase("""
Check out https://docs.test.io for more info.
""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>Check out
			<a href="https://docs.test.io" target="_blank" rel="noopener noreferrer">https://docs.test.io</a>
			for more info.</p>
		""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
public class AutoLinkWithPathTests() : AutoLinkTestBase("""
Visit https://docs.test.io/path/to/page for details.
""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>Visit
			<a href="https://docs.test.io/path/to/page" target="_blank" rel="noopener noreferrer">https://docs.test.io/path/to/page</a>
			for details.</p>
		""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
public class AutoLinkWithQueryStringTests() : AutoLinkTestBase("""
See https://docs.test.io/search?q=test&page=1 for results.
""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>See
			<a href="https://docs.test.io/search?q=test&amp;page=1" target="_blank" rel="noopener noreferrer">https://docs.test.io/search?q=test&amp;page=1</a>
			for results.</p>
		""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
public class AutoLinkWithAnchorTests() : AutoLinkTestBase("""
Jump to https://docs.test.io/page#section for the section.
""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>Jump to
			<a href="https://docs.test.io/page#section" target="_blank" rel="noopener noreferrer">https://docs.test.io/page#section</a>
			for the section.</p>
		""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
public class AutoLinkTrailingPeriodTests() : AutoLinkTestBase("""
Check out https://docs.test.io.
""")
{
	[Test]
	public void ExcludesTrailingPeriod() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>Check out
			<a href="https://docs.test.io" target="_blank" rel="noopener noreferrer">https://docs.test.io</a>.</p>
		""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
public class AutoLinkTrailingCommaTests() : AutoLinkTestBase(
	"""
Visit https://first.test.io, https://second.test.io, or https://third.test.io for info.
"""
)
{
	[Test]
	public void ExcludesTrailingCommas() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>Visit
			<a href="https://first.test.io" target="_blank" rel="noopener noreferrer">https://first.test.io</a>,
			<a href="https://second.test.io" target="_blank" rel="noopener noreferrer">https://second.test.io</a>, or
			<a href="https://third.test.io" target="_blank" rel="noopener noreferrer">https://third.test.io</a>
			for info.</p>
		""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
public class AutoLinkInParenthesesTests() : AutoLinkTestBase("""
See the docs (https://docs.test.io) for details.
""")
{
	[Test]
	public void ExcludesClosingParen() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"<p>See the docs (<a href=\"https://docs.test.io\" target=\"_blank\" rel=\"noopener noreferrer\">https://docs.test.io</a>) for details.</p>",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
public class AutoLinkWithBalancedParensTests() : AutoLinkTestBase(
	"""
Check https://en.wikipedia.org/wiki/Rust_(programming_language) for more.
"""
)
{
	[Test]
	public void IncludesBalancedParens() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>Check
			<a href="https://en.wikipedia.org/wiki/Rust_(programming_language)" target="_blank" rel="noopener noreferrer">https://en.wikipedia.org/wiki/Rust_(programming_language)</a>
			for more.</p>
		""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
public class AutoLinkElasticDocsHintTests() : AutoLinkTestBase("""
See https://www.elastic.co/docs/deploy-manage for deployment info.
""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>See
			<a href="https://www.elastic.co/docs/deploy-manage" target="_blank" rel="noopener noreferrer">https://www.elastic.co/docs/deploy-manage</a>
			for deployment info.</p>
		""",
			DocsHtml.Body()
		);

	[Test]
	public void EmitsHint() =>
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"hint {repo}/docs/index.md:3 Autolink points to elastic.co/docs. Consider using a crosslink or relative link instead."
			);
}

public class AutoLinkInCodeBlockTests() : AutoLinkNotFoundTestBase("""
```
https://docs.test.io/should/not/be/linked
```
""")
{
	[Test]
	public void DoesNotCreateLink() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<div class="highlight- notranslate">
			<div class="highlight">
				<pre>
					<code class="language-">https://docs.test.io/should/not/be/linked
					</code>
				</pre>
			</div>
		</div>
		""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

public class AutoLinkInInlineCodeTests() : AutoLinkNotFoundTestBase("""
Use the URL `https://docs.test.io/api` in your config.
""")
{
	[Test]
	public void DoesNotCreateLinkInInlineCode() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>Use the URL
			<code>https://docs.test.io/api</code>
			in your config.</p>
		""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

public class AutoLinkDoesNotMatchHttpTests() : AutoLinkNotFoundTestBase("""
This http://docs.test.io should not be autolinked.
""")
{
	[Test]
	public void DoesNotCreateLink() =>
		Html.Should().MatchInlineHtmlSnapshot("<p>This http://docs.test.io should not be autolinked.</p>", DocsHtml.Body());

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
public class AutoLinkWithStandardLinkTests() : AutoLinkTestBase(
	"""
Visit [Docs](https://docs.test.io) or https://other.test.io for more.
"""
)
{
	[Test]
	public void BothLinksWork() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>Visit
			<a href="https://docs.test.io" target="_blank" rel="noopener noreferrer">Docs</a>
			or
			<a href="https://other.test.io" target="_blank" rel="noopener noreferrer">https://other.test.io</a>
			for more.</p>
		""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
// Regression test for elastic/docs-builder#3317: no nested <a> when a URL is the link text.
public class AutoLinkInsideLinkTextTests() : AutoLinkTestBase(
	"""
Upload to a service like [https://gist.github.com](https://gist.github.com).
"""
)
{
	[Test]
	public void DoesNotCreateNestedAnchor()
	{
		Html.Should().MatchInlineHtmlSnapshot(
			"""
			<p>Upload to a service like
				<a href="https://gist.github.com" target="_blank" rel="noopener noreferrer">https://gist.github.com</a>.</p>
			""",
			DocsHtml.Body()
		);
		// The HTML parser would split nested anchors in the snapshot, so look for them in the raw output.
		Html.Should().NotMatchRegex(@"<a\b[^>]*><a\b");
	}

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
public class AutoLinkInsideLinkTextWithSurroundingTextTests() : AutoLinkTestBase(
	"""
See [the page at https://example.test.io for details](https://docs.test.io).
"""
)
{
	[Test]
	public void DoesNotAutolinkUrlInsideLinkText() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>See
			<a href="https://docs.test.io" target="_blank" rel="noopener noreferrer">the page at https://example.test.io for details</a>.</p>
		""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

// Verify that image-inside-link is unaffected by the IsNestedInsideLink guard (images bypass it via the IsImage branch).
[InheritsTests]
public class ImageInsideLinkTests() : InlineTest<LinkInline>("""
[![alt text](https://example.com/image.png)](https://example.com)
""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>
			<a href="https://example.com" target="_blank" rel="noopener noreferrer">
				<img src="https://example.com/image.png" alt="alt text" title="alt text">
			</a>
		</p>
		""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
public class MultipleAutoLinksTests() : AutoLinkTestBase(
	"""
First https://first.com then https://second.com and finally https://third.com are all linked.
"""
)
{
	[Test]
	public void AllLinksAreCreated() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>First
			<a href="https://first.com" target="_blank" rel="noopener noreferrer">https://first.com</a>
			then
			<a href="https://second.com" target="_blank" rel="noopener noreferrer">https://second.com</a>
			and finally
			<a href="https://third.com" target="_blank" rel="noopener noreferrer">https://third.com</a>
			are all linked.</p>
		""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

// === Exclusion rule tests ===

public class AutoLinkWithPortExclusionTests() : AutoLinkNotFoundTestBase("""
Connect to https://www.elastic.co:443/guide for the guide.
""")
{
	[Test]
	public void DoesNotCreateLinkForUrlWithPort() =>
		Html.Should().MatchInlineHtmlSnapshot("<p>Connect to https://www.elastic.co:443/guide for the guide.</p>", DocsHtml.Body());

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

public class AutoLinkLocalhostExclusionTests() : AutoLinkNotFoundTestBase("""
Check https://localhost/api for the local API.
""")
{
	[Test]
	public void DoesNotCreateLinkForLocalhost() =>
		Html.Should().MatchInlineHtmlSnapshot("<p>Check https://localhost/api for the local API.</p>", DocsHtml.Body());

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

public class AutoLinkLoopbackExclusionTests() : AutoLinkNotFoundTestBase("""
Test at https://127.0.0.1/health for health check.
""")
{
	[Test]
	public void DoesNotCreateLinkForLoopback() =>
		Html.Should().MatchInlineHtmlSnapshot("<p>Test at https://127.0.0.1/health for health check.</p>", DocsHtml.Body());

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

public class AutoLinkExampleDomainExclusionTests() : AutoLinkNotFoundTestBase("""
See https://example.com/docs for examples.
""")
{
	[Test]
	public void DoesNotCreateLinkForExampleDomain() =>
		Html.Should().MatchInlineHtmlSnapshot("<p>See https://example.com/docs for examples.</p>", DocsHtml.Body());

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

public class AutoLinkExampleSubdomainExclusionTests() : AutoLinkNotFoundTestBase("""
Visit https://system.example.com/setup for setup.
""")
{
	[Test]
	public void DoesNotCreateLinkForExampleSubdomain() =>
		Html.Should().MatchInlineHtmlSnapshot("<p>Visit https://system.example.com/setup for setup.</p>", DocsHtml.Body());

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

public class AutoLinkTemplatePlaceholderExclusionTests() : AutoLinkNotFoundTestBase(
	"""
Use https://{{cluster_id}}.es.test.co/api for your cluster.
"""
)
{
	[Test]
	public void DoesNotCreateLinkForTemplatePlaceholder() =>
		Html.Should().MatchInlineHtmlSnapshot("<p>Use https://{{cluster_id}}.es.test.co/api for your cluster.</p>", DocsHtml.Body());

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
	public void DoesNotCreateLinkForAsciiDocStyle() =>
		Html.Should().MatchInlineHtmlSnapshot("<p>See https://www.iana.org/assignments[IANA for assignments.</p>", DocsHtml.Body());

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
public class AutoLinkValidUrlStillWorksTests() : AutoLinkTestBase("""
Check https://www.elastic.co/guide for docs.
""")
{
	[Test]
	public void CreatesLinkForValidUrl() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>Check
			<a href="https://www.elastic.co/guide" target="_blank" rel="noopener noreferrer">https://www.elastic.co/guide</a>
			for docs.</p>
		""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}
