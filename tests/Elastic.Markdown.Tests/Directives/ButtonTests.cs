// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Testing;
using Elastic.Markdown.Myst.Directives.Button;
using Nullean.Snapshots.Html;

namespace Elastic.Markdown.Tests.Directives;

[InheritsTests]
public class ButtonBlockTests() : DirectiveTest<ButtonBlock>("""
:::{button}
[Get Started](/get-started)
:::
""")
{
	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void DefaultsToPrimaryType() => Block!.Type.Should().Be("primary");

	[Test]
	public void DefaultsToLeftAlign() => Block!.Align.Should().Be("left");

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="doc-button-wrapper doc-button-primary doc-button-left">
			<p>
				<a href="/get-started">Get Started</a>
			</p>
		</div>
		"""
		);
}

[InheritsTests]
public class ButtonSecondaryTests() : DirectiveTest<ButtonBlock>("""
:::{button}
:type: secondary
[Learn More](/learn-more)
:::
""")
{
	[Test]
	public void ParsesSecondaryType() => Block!.Type.Should().Be("secondary");

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="doc-button-wrapper doc-button-secondary doc-button-left">
			<p>
				<a href="/learn-more">Learn More</a>
			</p>
		</div>
		"""
		);
}

[InheritsTests]
public class ButtonNeutralTests() : DirectiveTest<ButtonBlock>(
	"""
:::{button}
:type: neutral
[Browse All Docs](https://www.elastic.co/docs)
:::
"""
)
{
	[Test]
	public void ParsesNeutralType() => Block!.Type.Should().Be("neutral");

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="doc-button-wrapper doc-button-neutral doc-button-left">
			<p>
				<a href="https://www.elastic.co/docs" target="_blank" rel="noopener noreferrer">Browse All Docs</a>
			</p>
		</div>
		"""
		);

	[Test]
	public void EmitsNoErrors() => Collector.Diagnostics.Should().BeEmpty();
}

[InheritsTests]
public class ButtonNeutralVariantAliasTests() : DirectiveTest<ButtonBlock>(
	"""
:::{button}
:variant: neutral
[Browse All Docs](https://www.elastic.co/docs)
:::
"""
)
{
	[Test]
	public void ParsesNeutralTypeFromVariantAlias() => Block!.Type.Should().Be("neutral");
}

[InheritsTests]
public class ButtonNeutralInGroupTests() : DirectiveTest<ButtonGroupBlock>(
	"""
::::{button-group}
:::{button}
[Get Started](/get-started)
:::
:::{button}
:type: neutral
[What's New](/whats-new)
:::
::::
"""
)
{
	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="doc-button-group doc-button-group-left">
			<span class="doc-button-item doc-button-primary">
				<p>
					<a href="/get-started">Get Started</a>
				</p>
			</span>
			<span class="doc-button-item doc-button-neutral">
				<p>
					<a href="/whats-new">What's New</a>
				</p>
			</span>
		</div>
		"""
		);
}

[InheritsTests]
public class ButtonAlignmentTests() : DirectiveTest<ButtonBlock>("""
:::{button}
:align: center
[Centered Button](/centered)
:::
""")
{
	[Test]
	public void ParsesCenterAlign() => Block!.Align.Should().Be("center");

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="doc-button-wrapper doc-button-primary doc-button-center">
			<p>
				<a href="/centered">Centered Button</a>
			</p>
		</div>
		"""
		);
}

[InheritsTests]
public class ButtonExternalTests() : DirectiveTest<ButtonBlock>("""
:::{button}
[GitHub](https://github.com/elastic)
:::
""")
{
	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="doc-button-wrapper doc-button-primary doc-button-left">
			<p>
				<a href="https://github.com/elastic" target="_blank" rel="noopener noreferrer">GitHub</a>
			</p>
		</div>
		"""
		);
}

[InheritsTests]
public class ButtonReferenceLinkTests() : DirectiveTest<ButtonBlock>(
	"""
:::{button}
[Open][kibana-url]
:::

[kibana-url]: <https://foo.example.com>
"""
)
{
	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="doc-button-wrapper doc-button-primary doc-button-left">
			<p>
				<a href="https://foo.example.com" target="_blank" rel="noopener noreferrer">Open</a>
			</p>
		</div>
		"""
		);

	[Test]
	public void EmitsNoErrors() => Collector.Diagnostics.Should().BeEmpty();
}

[InheritsTests]
public class ButtonInvalidTypeTests() : DirectiveTest<ButtonBlock>("""
:::{button}
:type: invalid
[Invalid Type](/test)
:::
""")
{
	[Test]
	public void EmitsWarningForInvalidType() =>
		Collector.Diagnostics.Should().ContainSingle(d => d.Message.Contains("Invalid button type"));

	[Test]
	public void FallsBackToPrimary() => Block!.Type.Should().Be("primary");
}

[InheritsTests]
public class ButtonGroupTests() : DirectiveTest<ButtonGroupBlock>(
	"""
::::{button-group}
:::{button}
:type: primary
[Primary](/primary)
:::
:::{button}
:type: secondary
[Secondary](/secondary)
:::
::::
"""
)
{
	[Test]
	public void ParsesButtonGroup() => Block.Should().NotBeNull();

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="doc-button-group doc-button-group-left">
			<span class="doc-button-item doc-button-primary">
				<p>
					<a href="/primary">Primary</a>
				</p>
			</span>
			<span class="doc-button-item doc-button-secondary">
				<p>
					<a href="/secondary">Secondary</a>
				</p>
			</span>
		</div>
		"""
		);
}

[InheritsTests]
public class ButtonGroupAlignmentTests() : DirectiveTest<ButtonGroupBlock>(
	"""
::::{button-group}
:align: center
:::{button}
[Centered](/centered)
:::
::::
"""
)
{
	[Test]
	public void ParsesGroupAlignment() => Block!.Align.Should().Be("center");

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="doc-button-group doc-button-group-center">
			<span class="doc-button-item doc-button-primary">
				<p>
					<a href="/centered">Centered</a>
				</p>
			</span>
		</div>
		"""
		);
}

[InheritsTests]
public class ButtonInGroupTests() : DirectiveTest<ButtonBlock>("""
::::{button-group}
:::{button}
[In Group](/in-group)
:::
::::
""")
{
	[Test]
	public void DetectsButtonIsInGroup() => Block!.IsInGroup.Should().BeTrue();

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="doc-button-group doc-button-group-left">
			<span class="doc-button-item doc-button-primary">
				<p>
					<a href="/in-group">In Group</a>
				</p>
			</span>
		</div>
		"""
		);
}

[InheritsTests]
public class ButtonCrossLinkTests() : DirectiveTest<ButtonBlock>("""
:::{button}
[Kibana Docs](kibana://api/index.md)
:::
""")
{
	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="doc-button-wrapper doc-button-primary doc-button-left">
			<p>
				<a href="kibana://api/index.md">Kibana Docs</a>
			</p>
		</div>
		"""
		);
}

[InheritsTests]
public class ButtonCursorProtocolTests() : DirectiveTest<ButtonBlock>(
	"""
:::{button}
[Install with Cursor](cursor://anysphere.cursor-deeplink/mcp/install?name=elastic&config=eyJmb28iOiJiYXIifQ==)
:::
"""
)
{
	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="doc-button-wrapper doc-button-primary doc-button-left">
			<p>
				<a href="cursor://anysphere.cursor-deeplink/mcp/install?name=elastic&amp;config=eyJmb28iOiJiYXIifQ==">Install with Cursor</a>
			</p>
		</div>
		"""
		);

	[Test]
	public void EmitsNoErrors() => Collector.Diagnostics.Should().BeEmpty();
}

[InheritsTests]
public class ButtonVscodeProtocolTests() : DirectiveTest<ButtonBlock>(
	"""
:::{button}
[Install with VS Code](vscode:extension/elastic.elasticsearch)
:::
"""
)
{
	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="doc-button-wrapper doc-button-primary doc-button-left">
			<p>
				<a href="vscode:extension/elastic.elasticsearch">Install with VS Code</a>
			</p>
		</div>
		"""
		);

	[Test]
	public void EmitsNoErrors() => Collector.Diagnostics.Should().BeEmpty();
}

[InheritsTests]
public class ButtonVscodeInsidersProtocolTests() : DirectiveTest<ButtonBlock>(
	"""
:::{button}
[Install with VS Code Insiders](vscode-insiders:mcp/install?%7B%22name%22%3A%22oblt-cli%22%7D)
:::
"""
)
{
	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="doc-button-wrapper doc-button-primary doc-button-left">
			<p>
				<a href="vscode-insiders:mcp/install?%7B%22name%22%3A%22oblt-cli%22%7D">Install with VS Code Insiders</a>
			</p>
		</div>
		"""
		);

	[Test]
	public void EmitsNoErrors() => Collector.Diagnostics.Should().BeEmpty();
}

[InheritsTests]
public class ButtonEmptyTests() : DirectiveTest<ButtonBlock>("""
:::{button}
:::
""")
{
	[Test]
	public void EmitsErrorForEmptyContent() => Collector.Diagnostics.Should().ContainSingle(d => d.Message.Contains("requires a link"));
}

[InheritsTests]
public class ButtonPlainTextTests() : DirectiveTest<ButtonBlock>("""
:::{button}
Just some text without a link
:::
""")
{
	[Test]
	public void EmitsErrorForPlainText() =>
		Collector.Diagnostics.Should().ContainSingle(d => d.Message.Contains("must contain only a single Markdown link"));
}

[InheritsTests]
public class ButtonMultipleLinksTests() : DirectiveTest<ButtonBlock>("""
:::{button}
[Link One](/one) and [Link Two](/two)
:::
""")
{
	[Test]
	public void EmitsErrorForMultipleLinks() =>
		Collector.Diagnostics.Should().ContainSingle(d => d.Message.Contains("must contain only a single Markdown link"));
}

[InheritsTests]
public class ButtonNestedDirectiveTests() : DirectiveTest<ButtonBlock>("""
:::{button}
::::{note}
This is nested
::::
:::
""")
{
	[Test]
	public void EmitsErrorForNestedDirective() =>
		Collector.Diagnostics.Should().ContainSingle(d => d.Message.Contains("cannot contain nested directives"));
}
