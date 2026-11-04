// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Myst.Directives.Button;

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
	public void RendersPrimaryButtonClass() => Html.Should().Contain("doc-button-primary");

	[Test]
	public void RendersLinkHref() => Html.Should().Contain("href=\"/get-started\"");

	[Test]
	public void RendersButtonText() => Html.Should().Contain("Get Started");
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
	public void RendersSecondaryClass() => Html.Should().Contain("doc-button-secondary");
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
	public void RendersNeutralClass() => Html.Should().Contain("doc-button-neutral");

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
	public void RendersNeutralItemInsideGroup() => Html.Should().Contain("class=\"doc-button-item doc-button-neutral\"");

	[Test]
	public void RendersPrimaryAlongsideNeutral() => Html.Should().Contain("doc-button-primary");
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
	public void RendersWrapperWithAlignClass() => Html.Should().Contain("doc-button-wrapper doc-button-primary doc-button-center");
}

[InheritsTests]
public class ButtonExternalTests() : DirectiveTest<ButtonBlock>("""
:::{button}
[GitHub](https://github.com/elastic)
:::
""")
{
	[Test]
	public void RendersExternalAttributes() => Html.Should().Contain("target=\"_blank\"");

	[Test]
	public void RendersNoopenerNoreferrer() => Html.Should().Contain("rel=\"noopener noreferrer\"");
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
	public void RendersReferencedLinkHref() => Html.Should().Contain("href=\"https://foo.example.com\"");

	[Test]
	public void RendersButtonText() => Html.Should().Contain(">Open<");

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
	public void RendersButtonGroupContainer() => Html.Should().Contain("class=\"doc-button-group");

	[Test]
	public void ContainsBothButtons() => Html.Should().Contain("Primary").And.Contain("Secondary");

	[Test]
	public void RendersPrimaryButton() => Html.Should().Contain("doc-button-primary");

	[Test]
	public void RendersSecondaryButton() => Html.Should().Contain("doc-button-secondary");
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
	public void RendersGroupAlignClass() => Html.Should().Contain("doc-button-group-center");
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
	public void DoesNotRenderWrapperInGroup() => Html.Should().NotContain("doc-button-wrapper");

	[Test]
	public void RendersButtonItem() => Html.Should().Contain("doc-button-item");
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
	public void RendersLinkHref() => Html.Should().Contain("href=\"");
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
	public void RendersLinkHref() => Html.Should().Contain("href=\"cursor://");

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
	public void RendersLinkHref() => Html.Should().Contain("href=\"vscode:");

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
	public void RendersLinkHref() => Html.Should().Contain("href=\"vscode-insiders:");

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
