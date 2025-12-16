// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.Directives.Button;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

public class ButtonBlockTests(ITestOutputHelper output) : DirectiveTest<ButtonBlock>(output,
"""
:::{button}
[Get Started](/get-started)
:::
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void DefaultsToPrimaryType() => Block!.Type.Should().Be("primary");

	[Fact]
	public void DefaultsToLeftAlign() => Block!.Align.Should().Be("left");

	[Fact]
	public void RendersPrimaryButtonClass() => Html.Should().Contain("doc-button-primary");

	[Fact]
	public void RendersLinkHref() => Html.Should().Contain("href=\"/get-started\"");

	[Fact]
	public void RendersButtonText() => Html.Should().Contain("Get Started");
}

public class ButtonSecondaryTests(ITestOutputHelper output) : DirectiveTest<ButtonBlock>(output,
"""
:::{button}
:type: secondary
[Learn More](/learn-more)
:::
"""
)
{
	[Fact]
	public void ParsesSecondaryType() => Block!.Type.Should().Be("secondary");

	[Fact]
	public void RendersSecondaryClass() => Html.Should().Contain("doc-button-secondary");
}

public class ButtonAlignmentTests(ITestOutputHelper output) : DirectiveTest<ButtonBlock>(output,
"""
:::{button}
:align: center
[Centered Button](/centered)
:::
"""
)
{
	[Fact]
	public void ParsesCenterAlign() => Block!.Align.Should().Be("center");

	[Fact]
	public void RendersWrapperWithAlignClass() => Html.Should().Contain("doc-button-wrapper doc-button-primary doc-button-center");
}

public class ButtonExternalTests(ITestOutputHelper output) : DirectiveTest<ButtonBlock>(output,
"""
:::{button}
[GitHub](https://github.com/elastic)
:::
"""
)
{
	[Fact]
	public void RendersExternalAttributes() => Html.Should().Contain("target=\"_blank\"");

	[Fact]
	public void RendersNoopenerNoreferrer() => Html.Should().Contain("rel=\"noopener noreferrer\"");
}

public class ButtonInvalidTypeTests(ITestOutputHelper output) : DirectiveTest<ButtonBlock>(output,
"""
:::{button}
:type: invalid
[Invalid Type](/test)
:::
"""
)
{
	[Fact]
	public void EmitsWarningForInvalidType() =>
		Collector.Diagnostics.Should().ContainSingle(d => d.Message.Contains("Invalid button type"));

	[Fact]
	public void FallsBackToPrimary() => Block!.Type.Should().Be("primary");
}

public class ButtonGroupTests(ITestOutputHelper output) : DirectiveTest<ButtonGroupBlock>(output,
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
	[Fact]
	public void ParsesButtonGroup() => Block.Should().NotBeNull();

	[Fact]
	public void RendersButtonGroupContainer() => Html.Should().Contain("class=\"doc-button-group");

	[Fact]
	public void ContainsBothButtons() =>
		Html.Should().Contain("Primary").And.Contain("Secondary");

	[Fact]
	public void RendersPrimaryButton() => Html.Should().Contain("doc-button-primary");

	[Fact]
	public void RendersSecondaryButton() => Html.Should().Contain("doc-button-secondary");
}

public class ButtonGroupAlignmentTests(ITestOutputHelper output) : DirectiveTest<ButtonGroupBlock>(output,
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
	[Fact]
	public void ParsesGroupAlignment() => Block!.Align.Should().Be("center");

	[Fact]
	public void RendersGroupAlignClass() => Html.Should().Contain("doc-button-group-center");
}

public class ButtonInGroupTests(ITestOutputHelper output) : DirectiveTest<ButtonBlock>(output,
"""
::::{button-group}
:::{button}
[In Group](/in-group)
:::
::::
"""
)
{
	[Fact]
	public void DetectsButtonIsInGroup() => Block!.IsInGroup.Should().BeTrue();

	[Fact]
	public void DoesNotRenderWrapperInGroup() => Html.Should().NotContain("doc-button-wrapper");

	[Fact]
	public void RendersButtonItem() => Html.Should().Contain("doc-button-item");
}

public class ButtonCrossLinkTests(ITestOutputHelper output) : DirectiveTest<ButtonBlock>(output,
"""
:::{button}
[Kibana Docs](kibana://api/index.md)
:::
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void RendersLinkHref() => Html.Should().Contain("href=\"");
}

public class ButtonEmptyTests(ITestOutputHelper output) : DirectiveTest<ButtonBlock>(output,
"""
:::{button}
:::
"""
)
{
	[Fact]
	public void EmitsErrorForEmptyContent() =>
		Collector.Diagnostics.Should().ContainSingle(d => d.Message.Contains("requires a link"));
}

public class ButtonPlainTextTests(ITestOutputHelper output) : DirectiveTest<ButtonBlock>(output,
"""
:::{button}
Just some text without a link
:::
"""
)
{
	[Fact]
	public void EmitsErrorForPlainText() =>
		Collector.Diagnostics.Should().ContainSingle(d => d.Message.Contains("must contain only a single Markdown link"));
}

public class ButtonMultipleLinksTests(ITestOutputHelper output) : DirectiveTest<ButtonBlock>(output,
"""
:::{button}
[Link One](/one) and [Link Two](/two)
:::
"""
)
{
	[Fact]
	public void EmitsErrorForMultipleLinks() =>
		Collector.Diagnostics.Should().ContainSingle(d => d.Message.Contains("must contain only a single Markdown link"));
}

public class ButtonNestedDirectiveTests(ITestOutputHelper output) : DirectiveTest<ButtonBlock>(output,
"""
:::{button}
::::{note}
This is nested
::::
:::
"""
)
{
	[Fact]
	public void EmitsErrorForNestedDirective() =>
		Collector.Diagnostics.Should().ContainSingle(d => d.Message.Contains("cannot contain nested directives"));
}
