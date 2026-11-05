// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Testing;
using JetBrains.Annotations;
using Markdig.Syntax.Inlines;
using Nullean.Snapshots;
using Nullean.Snapshots.Html;

namespace Elastic.Markdown.Tests.Inline;

[InheritsTests]
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

[InheritsTests]
public class InlineLinkTests() : LinkTestBase("""
[Elasticsearch](/_static/img/observability.png)
""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
			<p>
				<a href="/docs/_static/img/observability.png">Elasticsearch</a>
			</p>
			""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
public class LinkToPageTests() : LinkTestBase("""
[Requirements](testing/req.md)
""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
			<p>
				<a href="/docs/testing/req">Requirements</a>
			</p>
			""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");

	[Test]
	public void EmitsCrossLink() => Collector.CrossLinks.Should().HaveCount(0);
}

[InheritsTests]
public class InsertPageTitleTests() : LinkTestBase("""
[](testing/req.md)
""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
			<p>
				<a href="/docs/testing/req">Special Requirements</a>
			</p>
			""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");

	[Test]
	public void EmitsCrossLink() => Collector.CrossLinks.Should().HaveCount(0);
}

[InheritsTests]
public class RepositoryLinksTest() : LinkTestBase("""
	[test][test]

	[test]: testing/req.md
	""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
			<p>
				<a href="/docs/testing/req">test</a>
			</p>
			""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");

	[Test]
	public void EmitsCrossLink() => Collector.CrossLinks.Should().HaveCount(0);
}

[InheritsTests]
public class CrossLinkReferenceTest() : LinkTestBase("""
	[test][test]

	[test]: kibana://index.md
	""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""<p><a href="https://docs-v3-preview.elastic.dev/elastic/kibana/tree/main/" target="_blank" rel="noopener noreferrer">test</a></p>""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");

	[Test]
	public void EmitsCrossLink()
	{
		Collector.CrossLinks.Should().HaveCount(1);
		Collector.CrossLinks.Should().Contain("kibana://index.md");
	}
}

[InheritsTests]
public class CrossLinkTest() : LinkTestBase("""

	Go to [test](kibana://index.md)
	""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>Go to
			<a href="https://docs-v3-preview.elastic.dev/elastic/kibana/tree/main/" target="_blank" rel="noopener noreferrer">test</a>
		</p>
		""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");

	[Test]
	public void EmitsCrossLink()
	{
		Collector.CrossLinks.Should().HaveCount(1);
		Collector.CrossLinks.Should().Contain("kibana://index.md");
	}
}

[InheritsTests]
public class CrossLinkEmptyTextTest() : LinkTestBase("""

	Go to [](kibana://index.md)
	""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>Go to
			<a href="https://docs-v3-preview.elastic.dev/elastic/kibana/tree/main/" target="_blank" rel="noopener noreferrer"></a>
		</p>
		""",
			DocsHtml.Body()
		);

	[Test]
	public void HasError() =>
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error {repo}/docs/index.md:4 Crosslink 'kibana://index.md' has empty link text. Please provide explicit link text."
			);

	[Test]
	public void EmitsCrossLink()
	{
		Collector.CrossLinks.Should().HaveCount(1);
		Collector.CrossLinks.Should().Contain("kibana://index.md");
	}
}

[InheritsTests]
public class CrossLinkEmptyTextNoTitleTest() : LinkTestBase("""

	Go to [](kibana://get-started/index.md)
	""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>Go to
			<a href="https://docs-v3-preview.elastic.dev/elastic/kibana/tree/main/get-started" target="_blank" rel="noopener noreferrer"></a>
		</p>
		""",
			DocsHtml.Body()
		);

	[Test]
	public void HasError() =>
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error {repo}/docs/index.md:4 Crosslink 'kibana://get-started/index.md' has empty link text. Please provide explicit link text."
			);

	[Test]
	public void EmitsCrossLink()
	{
		Collector.CrossLinks.Should().HaveCount(1);
		Collector.CrossLinks.Should().Contain("kibana://get-started/index.md");
	}
}

[InheritsTests]
public class LinkWithUnresolvedInterpolationError() : LinkTestBase(
	"""
	[global search field]({{this-variable-does-not-exist}}/introduction.html#kibana-navigation-search)
	"""
)
{
	[Test]
	public void HasErrors() =>
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error {repo}/docs/index.md:3 The url contains unresolved template expressions: '{{this-variable-does-not-exist}}/introduction.html#kibana-navigation-search'. Please check if there is an appropriate global or frontmatter subs variable."
			);
}

[InheritsTests]
public class ExternalLinksWithInterpolationSuccess() : LinkTestBase("""
	[link to app]({{some-url-with-a-version}})
	""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""<p><a href="https://github.com/elastic/fake-repo/tree/v1.17.0" target="_blank" rel="noopener noreferrer">link to app</a></p>""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoWarningsOrErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
public class InternalLinksWithInterpolationWarning() : LinkTestBase("""
	[link to app]({{some-url-path-prefix}}/hello-world)
	""")
{
	[Test]
	public void HasWarnings() =>
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error {repo}/docs/index.md:3 Link is resolved to '/something/hello-world'. Only external links are allowed to be resolved from template expressions."
			);
}

[InheritsTests]
public class NonExistingLinks() : LinkTestBase("""
	[Non Existing Link](/non-existing.md)
	""")
{
	[Test]
	public void EmitsDiagnostics() =>
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error {repo}/docs/index.md:3 `/non-existing.md` does not exist. If it was recently removed add a redirect. resolved to `{repo}/docs/non-existing.md`"
			);
}

[InheritsTests]
public class CommentedNonExistingLinks() : LinkTestBase("""
	% [Non Existing Link](/non-existing.md)
	""")
{
	[Test]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().BeNullOrWhiteSpace();

	[Test]
	public void HasErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
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
		Html.Should().MatchInlineHtmlSnapshot(
			"""
			<p>Links:</p>
			<ul>
			<li><a href="/docs/testing/req">Special Requirements</a></li>
			</ul>
			<ul>
			<li><a href="/docs/testing/req">Special Requirements</a></li>
			</ul>
			""",
			DocsHtml.Body()
		);

	[Test]
	public void HasErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
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
	public void HasErrors() =>
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"""
		error {repo}/docs/index.md:3 `/non-existing.md` does not exist. If it was recently removed add a redirect. resolved to `{repo}/docs/non-existing.md`
		error {repo}/docs/index.md:4 `/non-existing.md` does not exist. If it was recently removed add a redirect. resolved to `{repo}/docs/non-existing.md`
		error {repo}/docs/index.md:5 `/non-existing.md` does not exist. If it was recently removed add a redirect. resolved to `{repo}/docs/non-existing.md`
		"""
			);
}

[InheritsTests]
public class CursorProtocolLinkTest() : LinkTestBase(
	"""
	[Install with Cursor](cursor://anysphere.cursor-deeplink/mcp/install?name=elastic&config=eyJmb28iOiJiYXIifQ==)
	"""
)
{
	[Test]
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>
			<a href="cursor://anysphere.cursor-deeplink/mcp/install?name=elastic&amp;config=eyJmb28iOiJiYXIifQ==">Install with Cursor</a>
		</p>
		""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");

	[Test]
	public void EmitsNoCrossLinks() => Collector.CrossLinks.Should().HaveCount(0);
}

[InheritsTests]
public class VscodeProtocolLinkTest() : LinkTestBase("""
	[Install VS Code Extension](vscode:extension/elastic.elasticsearch)
	""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>
			<a href="vscode:extension/elastic.elasticsearch">Install VS Code Extension</a>
		</p>
		""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");

	[Test]
	public void EmitsNoCrossLinks() => Collector.CrossLinks.Should().HaveCount(0);
}

[InheritsTests]
public class VscodeInsidersProtocolLinkTest() : LinkTestBase(
	"""
	[Install with VS Code Insiders](vscode-insiders:mcp/install?%7B%22name%22%3A%22oblt-cli%22%7D)
	"""
)
{
	[Test]
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>
			<a href="vscode-insiders:mcp/install?%7B%22name%22%3A%22oblt-cli%22%7D">Install with VS Code Insiders</a>
		</p>
		""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");

	[Test]
	public void EmitsNoCrossLinks() => Collector.CrossLinks.Should().HaveCount(0);
}

[InheritsTests]
public class EncodedSpaceLinkToExistingFile() : LinkTestBase("""
	[doc](testing/Contacting%20Support.png)
	""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		base.AddToFileSystem(fileSystem);
		fileSystem.AddFile(@"docs/testing/Contacting Support.png", new MockFileData(""));
	}

	[Test]
	public void ExplainsEncodedSpacesAndSuggestsWorkarounds()
	{
		var error = Collector.Diagnostics.Should().ContainSingle(d => d.Severity == Severity.Error).Subject;
		error.Message.Should().Contain("not URL-decoded").And.Contain("raw.githubusercontent.com").And.Contain("rename the file");
	}
}

[InheritsTests]
public class LiteralSpaceLinkToExistingFile() : LinkTestBase("""
	[doc](<testing/Contacting Support.png>)
	""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		base.AddToFileSystem(fileSystem);
		fileSystem.AddFile(@"docs/testing/Contacting Support.png", new MockFileData(""));
	}

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
public class LinkToUnpublishedFileType() : LinkTestBase("""
	[doc](testing/support.pdf)
	""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		base.AddToFileSystem(fileSystem);
		fileSystem.AddFile(@"docs/testing/support.pdf", new MockFileData(""));
	}

	[Test]
	public void WarnsThatFileIsNotPublished()
	{
		Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
		var warning = Collector.Diagnostics.Should().ContainSingle(d => d.Severity == Severity.Warning).Subject;
		warning.Message.Should().Contain("not published").And.Contain("raw.githubusercontent.com");
	}
}
