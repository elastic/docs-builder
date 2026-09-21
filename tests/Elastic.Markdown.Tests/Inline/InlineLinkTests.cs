// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using JetBrains.Annotations;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Tests.Inline;

public abstract class LinkTestBase([LanguageInjection("markdown")] string content) : InlineTest<LinkInline>(
	content,
	new Dictionary<string, string>
	{
		{ "some-url-with-a-version", "https://github.com/elastic/fake-repo/tree/v1.17.0" },
		{ "some-url-path-prefix", "/something" },
	}
)
{
	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion = """
# Special Requirements

To follow this tutorial you will need to install the following components:
""";
		fileSystem.AddFile(@"docs/testing/req.md", inclusion);
		fileSystem.AddFile(@"docs/_static/img/observability.png", new MockFileData(""));
	}
}

public class InlineLinkTests() : LinkTestBase("""
[Elasticsearch](/_static/img/observability.png)
""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.ShouldContainHtml(
			"""<p><a href="/docs/_static/img/observability.png" hx-get="/docs/_static/img/observability.png" hx-select-oob="#content-container,#toc-nav" hx-swap="none" hx-push-url="true" hx-indicator="#htmx-indicator" preload="mousedown">Elasticsearch</a></p>"""
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class LinkToPageTests() : LinkTestBase("""
[Requirements](testing/req.md)
""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.ShouldContainHtml(
			"""<p><a href="/docs/testing/req" hx-get="/docs/testing/req" hx-select-oob="#content-container,#toc-nav" hx-swap="none" hx-push-url="true" hx-indicator="#htmx-indicator" preload="mousedown">Requirements</a></p>"""
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);

	[Test]
	public void EmitsCrossLink() => Collector.CrossLinks.Should().HaveCount(0);
}

public class InsertPageTitleTests() : LinkTestBase("""
[](testing/req.md)
""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.ShouldContainHtml(
			"""<p><a href="/docs/testing/req" hx-get="/docs/testing/req" hx-select-oob="#content-container,#toc-nav" hx-swap="none" hx-push-url="true" hx-indicator="#htmx-indicator" preload="mousedown">Special Requirements</a></p>"""
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);

	[Test]
	public void EmitsCrossLink() => Collector.CrossLinks.Should().HaveCount(0);
}

public class RepositoryLinksTest() : LinkTestBase("""
	[test][test]

	[test]: testing/req.md
	""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.ShouldContainHtml(
			"""<p><a href="/docs/testing/req" hx-get="/docs/testing/req" hx-select-oob="#content-container,#toc-nav" hx-swap="none" hx-push-url="true" hx-indicator="#htmx-indicator" preload="mousedown">test</a></p>"""
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);

	[Test]
	public void EmitsCrossLink() => Collector.CrossLinks.Should().HaveCount(0);
}

public class CrossLinkReferenceTest() : LinkTestBase("""
	[test][test]

	[test]: kibana://index.md
	""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.ShouldContainHtml(
			"""<p><a href="https://docs-v3-preview.elastic.dev/elastic/kibana/tree/main/" target="_blank" rel="noopener noreferrer">test</a></p>"""
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);

	[Test]
	public void EmitsCrossLink()
	{
		Collector.CrossLinks.Should().HaveCount(1);
		Collector.CrossLinks.Should().Contain("kibana://index.md");
	}
}

public class CrossLinkTest() : LinkTestBase("""

	Go to [test](kibana://index.md)
	""")
{
	[Test]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p>Go to <a href="https://docs-v3-preview.elastic.dev/elastic/kibana/tree/main/" target="_blank" rel="noopener noreferrer">test</a></p>"""
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);

	[Test]
	public void EmitsCrossLink()
	{
		Collector.CrossLinks.Should().HaveCount(1);
		Collector.CrossLinks.Should().Contain("kibana://index.md");
	}
}

public class CrossLinkEmptyTextTest() : LinkTestBase("""

	Go to [](kibana://index.md)
	""")
{
	[Test]
	public void GeneratesHtml() =>
		// language=html - empty crosslinks now emit an error
		Html.Should().Contain(
			"""<p>Go to <a href="https://docs-v3-preview.elastic.dev/elastic/kibana/tree/main/" target="_blank" rel="noopener noreferrer"></a></p>"""
		);

	[Test]
	public void HasError() =>
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("empty link text"));

	[Test]
	public void EmitsCrossLink()
	{
		Collector.CrossLinks.Should().HaveCount(1);
		Collector.CrossLinks.Should().Contain("kibana://index.md");
	}
}

public class CrossLinkEmptyTextNoTitleTest() : LinkTestBase("""

	Go to [](kibana://get-started/index.md)
	""")
{
	[Test]
	public void GeneratesHtml() =>
		// language=html - empty crosslinks emit an error; isolated builds get target=_blank
		Html.Should().Contain(
			"""<p>Go to <a href="https://docs-v3-preview.elastic.dev/elastic/kibana/tree/main/get-started" target="_blank" rel="noopener noreferrer"></a></p>"""
		);

	[Test]
	public void HasError() =>
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("empty link text"));

	[Test]
	public void EmitsCrossLink()
	{
		Collector.CrossLinks.Should().HaveCount(1);
		Collector.CrossLinks.Should().Contain("kibana://get-started/index.md");
	}
}

public class LinkWithUnresolvedInterpolationError() : LinkTestBase(
	"""
	[global search field]({{this-variable-does-not-exist}}/introduction.html#kibana-navigation-search)
	"""
)
{
	[Test]
	public void HasErrors()
	{
		Collector.Diagnostics.Should().HaveCount(1);
		Collector.Diagnostics.First().Severity.Should().Be(Severity.Error);
		Collector
			.Diagnostics
			.First()
			.Message
			.Should()
			.Contain(
				"he url contains unresolved template expressions: '{{this-variable-does-not-exist}}/introduction.html#kibana-navigation-search'. Please check if there is an appropriate global or frontmatter subs variable."
			);
	}
}

public class ExternalLinksWithInterpolationSuccess() : LinkTestBase("""
	[link to app]({{some-url-with-a-version}})
	""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.ShouldContainHtml(
			"""<p><a href="https://github.com/elastic/fake-repo/tree/v1.17.0" target="_blank" rel="noopener noreferrer">link to app</a></p>"""
		);

	[Test]
	public void HasNoWarningsOrErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class InternalLinksWithInterpolationWarning() : LinkTestBase("""
	[link to app]({{some-url-path-prefix}}/hello-world)
	""")
{
	[Test]
	public void HasWarnings()
	{
		Collector.Diagnostics.Should().HaveCount(1);
		Collector.Diagnostics.First().Severity.Should().Be(Severity.Error);
		Collector
			.Diagnostics
			.First()
			.Message
			.Should()
			.Contain(
				"Link is resolved to '/something/hello-world'. Only external links are allowed to be resolved from template expressions."
			);
	}
}

public class NonExistingLinks() : LinkTestBase("""
	[Non Existing Link](/non-existing.md)
	""")
{
	[Test]
	public void HasErrors() => Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Should().HaveCount(1);

	[Test]
	public void HasNoWarning() => Collector.Diagnostics.Where(d => d.Severity == Severity.Warning).Should().HaveCount(0);
}

public class CommentedNonExistingLinks() : LinkTestBase("""
	% [Non Existing Link](/non-existing.md)
	""")
{
	[Test]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().BeNullOrWhiteSpace();

	[Test]
	public void HasErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class CommentedNonExistingLinks2() : LinkTestBase(
	"""
	% Hello, this is a [Non Existing Link](/non-existing.md).
	Links:
	- [](/testing/req.md)
	% - [Non Existing Link](/non-existing.md)
	- [](/testing/req.md)
	"""
)
{
	[Test]
	public void GeneratesHtml() =>
		Html.ShouldBeHtml(
			"""
			<p>Links:</p>
			<ul>
			<li><a href="/docs/testing/req">Special Requirements</a></li>
			</ul>
			<ul>
			<li><a href="/docs/testing/req">Special Requirements</a></li>
			</ul>
			"""
		);

	[Test]
	public void HasErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class NonExistingLinkShouldFail() : LinkTestBase(
	"""
	[Non Existing Link](/non-existing.md)
	- [Non Existing Link](/non-existing.md)
	This is another [Non Existing Link](/non-existing.md)
	% This is a commented [Non Existing Link](/non-existing.md)
	"""
)
{
	[Test]
	public void HasErrors() => Collector.Diagnostics.Should().HaveCount(3);
}

public class CursorProtocolLinkTest() : LinkTestBase(
	"""
	[Install with Cursor](cursor://anysphere.cursor-deeplink/mcp/install?name=elastic&config=eyJmb28iOiJiYXIifQ==)
	"""
)
{
	[Test]
	public void GeneratesHtml() => Html.Should().Contain("""href="cursor://""");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);

	[Test]
	public void EmitsNoCrossLinks() => Collector.CrossLinks.Should().HaveCount(0);
}

public class VscodeProtocolLinkTest() : LinkTestBase("""
	[Install VS Code Extension](vscode:extension/elastic.elasticsearch)
	""")
{
	[Test]
	public void GeneratesHtml() => Html.Should().Contain("""href="vscode:""");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);

	[Test]
	public void EmitsNoCrossLinks() => Collector.CrossLinks.Should().HaveCount(0);
}

public class VscodeInsidersProtocolLinkTest() : LinkTestBase(
	"""
	[Install with VS Code Insiders](vscode-insiders:mcp/install?%7B%22name%22%3A%22oblt-cli%22%7D)
	"""
)
{
	[Test]
	public void GeneratesHtml() => Html.Should().Contain("""href="vscode-insiders:""");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);

	[Test]
	public void EmitsNoCrossLinks() => Collector.CrossLinks.Should().HaveCount(0);
}
