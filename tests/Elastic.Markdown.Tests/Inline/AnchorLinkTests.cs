// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using JetBrains.Annotations;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Tests.Inline;

[InheritsTests]
public abstract class AnchorLinkTestBase([LanguageInjection("markdown")] string content) : InlineTest<LinkInline>(
	$"""
## Hello world

A paragraph

{content}

"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion =
			"""
# Special Requirements

## Sub Requirements

To follow this tutorial you will need to install the following components:

## New Requirements [#new-reqs]


:::{dropdown} Nested heading

##### Heading inside dropdown [#heading-inside-dropdown]

:::

These are new requirements
""";
		fileSystem.AddFile(@"docs/testing/req.md", inclusion);
		fileSystem.AddFile(@"docs/_static/img/observability.png", new MockFileData(""));
	}
}

[InheritsTests]
public class InPageAnchorTests() : AnchorLinkTestBase("""
[Hello](#hello-world)
""")
{
	[Test]
	public void GeneratesHtml() => Html.ShouldContainHtml("""<p><a href="#hello-world">Hello</a></p>""");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

[InheritsTests]
public class ExternalPageAnchorTests() : AnchorLinkTestBase("""
[Sub Requirements](testing/req.md#sub-requirements)
""")
{
	[Test]
	public void GeneratesHtml() =>
		// language=html
		Html.ShouldContainHtml(
			"""<p><a href="/docs/testing/req#sub-requirements" hx-select-oob="#content-container,#toc-nav" preload="mousedown">Sub Requirements</a></p>"""
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

[InheritsTests]
public class ExternalPageCustomAnchorTests() : AnchorLinkTestBase("""
[Sub Requirements](testing/req.md#new-reqs)
""")
{
	[Test]
	public void GeneratesHtml() =>
		// language=html
		Html.ShouldContainHtml(
			"""<p><a href="/docs/testing/req#new-reqs" hx-get="/docs/testing/req#new-reqs" hx-select-oob="#content-container,#toc-nav" hx-swap="none" hx-push-url="true" hx-indicator="#htmx-indicator" preload="mousedown">Sub Requirements</a></p>"""
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

[InheritsTests]
public class ExternalPageAnchorAutoTitleTests() : AnchorLinkTestBase("""
[](testing/req.md#sub-requirements)
""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.ShouldContainHtml(
			"""<p><a href="/docs/testing/req#sub-requirements" hx-get="/docs/testing/req#sub-requirements" hx-select-oob="#content-container,#toc-nav" hx-swap="none" hx-push-url="true" hx-indicator="#htmx-indicator" preload="mousedown">Special Requirements &gt; Sub Requirements</a></p>"""
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

[InheritsTests]
public class InPageBadAnchorTests() : AnchorLinkTestBase("""
[Hello](#hello-world2)
""")
{
	[Test]
	public void GeneratesHtml() => Html.ShouldContainHtml("""<p><a href="#hello-world2">Hello</a></p>""");

	[Test]
	public void HasError() =>
		Collector.Diagnostics.Should().HaveCount(1).And.Contain(d => d.Message.Contains("`hello-world2` does not exist"));
}

[InheritsTests]
public class ExternalPageBadAnchorTests() : AnchorLinkTestBase("""
[Sub Requirements](testing/req.md#sub-requirements2)
""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.ShouldContainHtml(
			"""<p><a href="/docs/testing/req#sub-requirements2" hx-get="/docs/testing/req#sub-requirements2" hx-select-oob="#content-container,#toc-nav" hx-swap="none" hx-push-url="true" hx-indicator="#htmx-indicator" preload="mousedown">Sub Requirements</a></p>"""
		);

	[Test]
	public void HasError() =>
		Collector.Diagnostics.Should().HaveCount(1).And.Contain(d => d.Message.Contains("`sub-requirements2` does not exist"));
}

[InheritsTests]
public class NestedHeadingTest() : AnchorLinkTestBase("""
	[Heading inside dropdown](testing/req.md#heading-inside-dropdown)
	""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.ShouldContainHtml(
			"""<a href="/docs/testing/req#heading-inside-dropdown" hx-get="/docs/testing/req#heading-inside-dropdown" hx-select-oob="#content-container,#toc-nav" hx-swap="none" hx-push-url="true" hx-indicator="#htmx-indicator" preload="mousedown">Heading inside dropdown</a>"""
		);
	[Test]
	public void HasError() => Collector.Diagnostics.Should().HaveCount(0);
}

[InheritsTests]
public class MissingMdExtensionTests() : AnchorLinkTestBase("""
[Link](testing/req)
""")
{
	[Test]
	public void HasMdExtensionHintError() =>
		Collector.Diagnostics.Should().HaveCount(1).And.Contain(d => d.Message.Contains("Did you forget to add the .md extension?"));
}

[InheritsTests]
public class MissingMdExtensionWithAnchorTests() : AnchorLinkTestBase("""
[Link](testing/req#sub-requirements)
""")
{
	[Test]
	public void HasMdExtensionHintError() =>
		Collector.Diagnostics.Should().HaveCount(1).And.Contain(d => d.Message.Contains("Did you forget to add the .md extension?"));
}

[InheritsTests]
public class MissingFileNoMdHintTests() : AnchorLinkTestBase("""
[Link](testing/nonexistent)
""")
{
	[Test]
	public void HasGenericNotFoundError() =>
		Collector
			.Diagnostics
			.Should()
			.HaveCount(1)
			.And
			.Contain(d => d.Message.Contains("does not exist"))
			.And
			.NotContain(d => d.Message.Contains("Did you forget to add the .md extension?"));
}
