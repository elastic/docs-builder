// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Diagnostics;
using FluentAssertions;
using JetBrains.Annotations;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Tests.Inline;

public abstract class LinkTestBase(ITestOutputHelper output, [LanguageInjection("markdown")] string content)
	: InlineTest<LinkInline>(
		output,
		content,
		new Dictionary<string, string>
		{
			{ "some-url-with-a-version", "https://github.com/elastic/fake-repo/tree/v1.17.0" },
			{ "some-url-path-prefix", "/something" },
		}
	)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion =
"""
# Special Requirements

To follow this tutorial you will need to install the following components:
""";
		fileSystem.AddFile(@"docs/testing/req.md", inclusion);
		fileSystem.AddFile(@"docs/_static/img/observability.png", new MockFileData(""));
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
			"""<p><a href="/docs/_static/img/observability.png" hx-get="/docs/_static/img/observability.png" hx-select-oob="#primary-nav,#secondary-nav,#content-container" hx-swap="none" hx-push-url="true" hx-indicator="#htmx-indicator" preload="true">Elasticsearch</a></p>"""
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
			"""<p><a href="/docs/testing/req" hx-get="/docs/testing/req" hx-select-oob="#primary-nav,#secondary-nav,#content-container" hx-swap="none" hx-push-url="true" hx-indicator="#htmx-indicator" preload="true">Requirements</a></p>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);

	[Fact]
	public void EmitsCrossLink() => Collector.CrossLinks.Should().HaveCount(0);
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
			"""<p><a href="/docs/testing/req" hx-get="/docs/testing/req" hx-select-oob="#primary-nav,#secondary-nav,#content-container" hx-swap="none" hx-push-url="true" hx-indicator="#htmx-indicator" preload="true">Special Requirements</a></p>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);

	[Fact]
	public void EmitsCrossLink() => Collector.CrossLinks.Should().HaveCount(0);
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
			"""<p><a href="/docs/testing/req" hx-get="/docs/testing/req" hx-select-oob="#primary-nav,#secondary-nav,#content-container" hx-swap="none" hx-push-url="true" hx-indicator="#htmx-indicator" preload="true">test</a></p>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);

	[Fact]
	public void EmitsCrossLink() => Collector.CrossLinks.Should().HaveCount(0);
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
			"""<p><a href="https://docs-v3-preview.elastic.dev/elastic/kibana/tree/main/">test</a></p>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);

	[Fact]
	public void EmitsCrossLink()
	{
		Collector.CrossLinks.Should().HaveCount(1);
		Collector.CrossLinks.Should().Contain("kibana://index.md");
	}
}

public class CrossLinkTest(ITestOutputHelper output) : LinkTestBase(output,
	"""

	Go to [test](kibana://index.md)
	"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p>Go to <a href="https://docs-v3-preview.elastic.dev/elastic/kibana/tree/main/">test</a></p>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);

	[Fact]
	public void EmitsCrossLink()
	{
		Collector.CrossLinks.Should().HaveCount(1);
		Collector.CrossLinks.Should().Contain("kibana://index.md");
	}
}

public class LinkWithUnresolvedInterpolationError(ITestOutputHelper output) : LinkTestBase(output,
	"""
	[global search field]({{this-variable-does-not-exist}}/introduction.html#kibana-navigation-search)
	"""
)
{
	[Fact]
	public void HasErrors()
	{
		Collector.Diagnostics.Should().HaveCount(1);
		Collector.Diagnostics.First().Severity.Should().Be(Severity.Error);
		Collector.Diagnostics.First().Message.Should().Contain("he url contains unresolved template expressions: '{{this-variable-does-not-exist}}/introduction.html#kibana-navigation-search'. Please check if there is an appropriate global or frontmatter subs variable.");
	}
}

public class ExternalLinksWithInterpolationSuccess(ITestOutputHelper output) : LinkTestBase(output,
	"""
	[link to app]({{some-url-with-a-version}})
	"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p><a href="https://github.com/elastic/fake-repo/tree/v1.17.0">link to app</a></p>"""
		);

	[Fact]
	public void HasNoWarningsOrErrors()
	{
		Collector.Diagnostics.Should().HaveCount(0);
	}
}

public class InternalLinksWithInterpolationWarning(ITestOutputHelper output) : LinkTestBase(output,
	"""
	[link to app]({{some-url-path-prefix}}/hello-world)
	"""
)
{
	[Fact]
	public void HasWarnings()
	{
		Collector.Diagnostics.Should().HaveCount(1);
		Collector.Diagnostics.First().Severity.Should().Be(Severity.Error);
		Collector.Diagnostics.First().Message.Should().Contain("Link is resolved to '/something/hello-world'. Only external links are allowed to be resolved from template expressions.");
	}
}




public class NonExistingLinks(ITestOutputHelper output) : LinkTestBase(output,
	"""
	[Non Existing Link](/non-existing.md)
	"""
)
{
	[Fact]
	public void HasErrors() => Collector.Diagnostics
		.Where(d => d.Severity == Severity.Error)
		.Should().HaveCount(1);

	[Fact]
	public void HasNoWarning() => Collector.Diagnostics
		.Where(d => d.Severity == Severity.Warning)
		.Should().HaveCount(0);
}

public class CommentedNonExistingLinks(ITestOutputHelper output) : LinkTestBase(output,
	"""
	% [Non Existing Link](/non-existing.md)
	"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().BeNullOrWhiteSpace();

	[Fact]
	public void HasErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class CommentedNonExistingLinks2(ITestOutputHelper output) : LinkTestBase(output,
	"""
	% Hello, this is a [Non Existing Link](/non-existing.md).
	Links:
	- [](/testing/req.md)
	% - [Non Existing Link](/non-existing.md)
	- [](/testing/req.md)
	"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.TrimEnd().Should().Be("""
		<p>Links:</p>
		<ul>
		<li><a href="/docs/testing/req" hx-get="/docs/testing/req" hx-select-oob="#primary-nav,#secondary-nav,#content-container" hx-swap="none" hx-push-url="true" hx-indicator="#htmx-indicator" preload="true">Special Requirements</a></li>
		</ul>
		<ul>
		<li><a href="/docs/testing/req" hx-get="/docs/testing/req" hx-select-oob="#primary-nav,#secondary-nav,#content-container" hx-swap="none" hx-push-url="true" hx-indicator="#htmx-indicator" preload="true">Special Requirements</a></li>
		</ul>
		""");

	[Fact]
	public void HasErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class NonExistingLinkShouldFail(ITestOutputHelper output) : LinkTestBase(output,
	"""
	[Non Existing Link](/non-existing.md)
	- [Non Existing Link](/non-existing.md)
	This is another [Non Existing Link](/non-existing.md)
	% This is a commented [Non Existing Link](/non-existing.md)
	"""
)
{

	[Fact]
	public void HasErrors() => Collector.Diagnostics.Should().HaveCount(3);
}
