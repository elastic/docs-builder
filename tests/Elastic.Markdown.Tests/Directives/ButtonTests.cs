// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.Directives.Button;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

public class ButtonBlockTests(ITestOutputHelper output) : DirectiveTest<ButtonBlock>(output,
"""
:::{button} Get Started
:link: /get-started
:::
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void ExtractsText() => Block!.Text.Should().Be("Get Started");

	[Fact]
	public void ExtractsLink() => Block!.Link.Should().Be("/get-started");

	[Fact]
	public void DefaultsToPrimaryType() => Block!.Type.Should().Be("primary");

	[Fact]
	public void DefaultsToLeftAlign() => Block!.Align.Should().Be("left");

	[Fact]
	public void DefaultsToNotExternal() => Block!.External.Should().BeFalse();

	[Fact]
	public void RendersButtonElement() => Html.Should().Contain("bg-blue-elastic");

	[Fact]
	public void RendersLinkHref() => Html.Should().Contain("href=\"/get-started\"");

	[Fact]
	public void RendersButtonText() => Html.Should().Contain("Get Started");
}

public class ButtonSecondaryTests(ITestOutputHelper output) : DirectiveTest<ButtonBlock>(output,
"""
:::{button} Learn More
:link: /learn-more
:type: secondary
:::
"""
)
{
	[Fact]
	public void ParsesSecondaryType() => Block!.Type.Should().Be("secondary");

	[Fact]
	public void RendersSecondaryClass() => Html.Should().Contain("border-blue-elastic");
}

public class ButtonAlignmentTests(ITestOutputHelper output) : DirectiveTest<ButtonBlock>(output,
"""
:::{button} Centered Button
:link: /centered
:align: center
:::
"""
)
{
	[Fact]
	public void ParsesCenterAlign() => Block!.Align.Should().Be("center");

	[Fact]
	public void RendersWrapperWithAlignClass() => Html.Should().Contain("doc-button-wrapper doc-button-center");
}

public class ButtonExternalTests(ITestOutputHelper output) : DirectiveTest<ButtonBlock>(output,
"""
:::{button} GitHub
:link: https://github.com/elastic
:external:
:::
"""
)
{
	[Fact]
	public void ParsesExternalFlag() => Block!.External.Should().BeTrue();

	[Fact]
	public void RendersExternalAttributes() => Html.Should().Contain("target=\"_blank\"");

	[Fact]
	public void RendersNoopenerNoreferrer() => Html.Should().Contain("rel=\"noopener noreferrer\"");
}

public class ButtonAutoExternalTests(ITestOutputHelper output) : DirectiveTest<ButtonBlock>(output,
"""
:::{button} GitHub
:link: https://github.com/elastic
:::
"""
)
{
	[Fact]
	public void AutoDetectsExternalLink() => Block!.External.Should().BeTrue();
}

public class ButtonMissingLinkTests(ITestOutputHelper output) : DirectiveTest<ButtonBlock>(output,
"""
:::{button} No Link
:::
"""
)
{
	[Fact]
	public void EmitsErrorForMissingLink() =>
		Collector.Diagnostics.Should().ContainSingle(d => d.Message.Contains("requires a :link: property"));
}

public class ButtonMissingTextTests(ITestOutputHelper output) : DirectiveTest<ButtonBlock>(output,
"""
:::{button}
:link: /somewhere
:::
"""
)
{
	[Fact]
	public void EmitsErrorForMissingText() =>
		Collector.Diagnostics.Should().ContainSingle(d => d.Message.Contains("requires text as an argument"));
}

public class ButtonInvalidTypeTests(ITestOutputHelper output) : DirectiveTest<ButtonBlock>(output,
"""
:::{button} Invalid Type
:link: /test
:type: invalid
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
:::{button} Primary
:link: /primary
:type: primary
:::
:::{button} Secondary
:link: /secondary
:type: secondary
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
	public void RendersPrimaryButton() => Html.Should().Contain("bg-blue-elastic");

	[Fact]
	public void RendersSecondaryButton() => Html.Should().Contain("border-blue-elastic");
}

public class ButtonGroupAlignmentTests(ITestOutputHelper output) : DirectiveTest<ButtonGroupBlock>(output,
"""
::::{button-group}
:align: center
:::{button} Centered
:link: /centered
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
:::{button} In Group
:link: /in-group
:::
::::
"""
)
{
	[Fact]
	public void DetectsButtonIsInGroup() => Block!.IsInGroup.Should().BeTrue();

	[Fact]
	public void DoesNotRenderWrapperInGroup() => Html.Should().NotContain("doc-button-wrapper");
}

public class ButtonCrossLinkTests(ITestOutputHelper output) : DirectiveTest<ButtonBlock>(output,
"""
:::{button} Kibana Docs
:link: kibana://api/index.md
:::
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void ExtractsLink() => Block!.Link.Should().Be("kibana://api/index.md");

	[Fact]
	public void CrossLinksAreNotExternal() => Block!.External.Should().BeFalse();
}

