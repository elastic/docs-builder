// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Myst.Directives.Storybook;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

public class StorybookTests(ITestOutputHelper output) : DirectiveTest<StorybookBlock>(output,
"""
:::{storybook}
:root: /storybook/my-lib
:id: components-button--primary
:height: 300
:title: Button / Primary story
:::
"""
)
{
	[Fact]
	public void SetsStoryRoot() => Block!.StoryRoot.Should().Be("/storybook/my-lib");

	[Fact]
	public void SetsStoryId() => Block!.StoryId.Should().Be("components-button--primary");

	[Fact]
	public void BuildsStoryUrl() => Block!.StoryUrl.Should().Be("/storybook/my-lib/iframe.html?id=components-button--primary&viewMode=story");

	[Fact]
	public void SetsHeight() => Block!.Height.Should().Be(300);

	[Fact]
	public void SetsTitle() => Block!.IframeTitle.Should().Be("Button / Primary story");

	[Fact]
	public void RendersIframe()
	{
		Html.Should().Contain("class=\"storybook-embed\"");
		Html.Should().Contain("src=\"/storybook/my-lib/iframe.html?id=components-button--primary&amp;viewMode=story\"");
		Html.Should().Contain("title=\"Button / Primary story\"");
		Html.Should().Contain("height:300px");
		Html.Should().Contain("loading=\"lazy\"");
	}
}

public class StorybookBodyTests(ITestOutputHelper output) : DirectiveTest<StorybookBlock>(output,
"""
:::{storybook}
:root: /storybook/my-lib
:id: components-button--primary
Supporting details for this story.
:::
"""
)
{
	[Fact]
	public void RendersBodyContent() =>
		Html.Should().Contain("Supporting details for this story.");
}

public class StorybookDefaultsTests(ITestOutputHelper output) : DirectiveTest<StorybookBlock>(output,
"""
:::{storybook}
:root: /storybook/my-lib
:id: components-button--primary
:::
"""
)
{
	[Fact]
	public void UsesDefaultHeight() => Block!.Height.Should().Be(400);

	[Fact]
	public void UsesDefaultTitle() => Block!.IframeTitle.Should().Be("Storybook story");
}

public class StorybookDocsetRootTests(ITestOutputHelper output) : DirectiveTest<StorybookBlock>(output,
"""
:::{storybook}
:id: components-button--primary
:::
"""
)
{
	protected override string? GetDocsetExtraYaml() =>
"""
storybook:
  root: /storybook/my-lib
""";

	[Fact]
	public void UsesDocsetRoot()
	{
		Block!.StoryRoot.Should().Be("/storybook/my-lib");
		Block!.StoryUrl.Should().Be("/storybook/my-lib/iframe.html?id=components-button--primary&viewMode=story");
		Collector.Diagnostics.Should().BeEmpty();
	}
}

public class StorybookDocsetServerRootTests(ITestOutputHelper output) : DirectiveTest<StorybookBlock>(output,
"""
:::{storybook}
:id: components-button--primary
:::
"""
)
{
	protected override string? GetDocsetExtraYaml() =>
"""
storybook:
  root: /storybook/my-lib
  server_root: http://localhost:6006
""";

	[Fact]
	public void CombinesDocsetRootAndServerRoot()
	{
		Block!.StoryRoot.Should().Be("http://localhost:6006/storybook/my-lib");
		Block!.StoryUrl.Should().Be("http://localhost:6006/storybook/my-lib/iframe.html?id=components-button--primary&viewMode=story");
		Collector.Diagnostics.Should().BeEmpty();
	}
}

public class StorybookServerRootWithLiteralRootTests(ITestOutputHelper output) : DirectiveTest<StorybookBlock>(output,
"""
:::{storybook}
:root: /
:id: components-button--primary
:::
"""
)
{
	protected override string? GetDocsetExtraYaml() =>
"""
storybook:
  server_root: http://localhost:6006
""";

	[Fact]
	public void UsesServerRootWithoutDoubleSlash()
	{
		Block!.StoryRoot.Should().Be("http://localhost:6006");
		Block!.StoryUrl.Should().Be("http://localhost:6006/iframe.html?id=components-button--primary&viewMode=story");
		Collector.Diagnostics.Should().BeEmpty();
	}
}

public class StorybookLiteralRootTests(ITestOutputHelper output) : DirectiveTest<StorybookBlock>(output,
"""
:::{storybook}
:root: /
:id: components-button--primary
:::
"""
)
{
	[Fact]
	public void UsesLiteralRootWithoutDoubleSlash()
	{
		Block!.StoryRoot.Should().Be("/");
		Block!.StoryUrl.Should().Be("/iframe.html?id=components-button--primary&viewMode=story");
		Collector.Diagnostics.Should().BeEmpty();
	}
}

public class StorybookDirectiveRootUsesDocsetServerRootTests(ITestOutputHelper output) : DirectiveTest<StorybookBlock>(output,
"""
:::{storybook}
:root: /storybook/other-lib
:id: components-button--primary
:::
"""
)
{
	protected override string? GetDocsetExtraYaml() =>
"""
storybook:
  server_root: http://localhost:6006
""";

	[Fact]
	public void CombinesDirectiveRootAndServerRoot()
	{
		Block!.StoryRoot.Should().Be("http://localhost:6006/storybook/other-lib");
		Block!.StoryUrl.Should().Be("http://localhost:6006/storybook/other-lib/iframe.html?id=components-button--primary&viewMode=story");
		Collector.Diagnostics.Should().BeEmpty();
	}
}

public class StorybookAllowListedAbsoluteUrlTests(ITestOutputHelper output) : DirectiveTest<StorybookBlock>(output,
"""
:::{storybook}
:root: http://localhost:6006/storybook/my-lib
:id: components-button--primary
:::
"""
)
{
	protected override string? GetDocsetExtraYaml() =>
"""
storybook:
  allowed_roots:
    - http://localhost:6006/storybook/my-lib
""";

	[Fact]
	public void AcceptsAllowListedAbsoluteUrl()
	{
		Block!.StoryRoot.Should().Be("http://localhost:6006/storybook/my-lib");
		Block!.StoryUrl.Should().Be("http://localhost:6006/storybook/my-lib/iframe.html?id=components-button--primary&viewMode=story");
		Collector.Diagnostics.Should().BeEmpty();
	}
}

public class StorybookInvalidHeightTests(ITestOutputHelper output) : DirectiveTest<StorybookBlock>(output,
"""
:::{storybook}
:root: /storybook/my-lib
:id: components-button--primary
:height: tall
:::
"""
)
{
	[Fact]
	public void WarnsAndFallsBackToDefaultHeight()
	{
		Block!.Height.Should().Be(400);
		Collector.Diagnostics.Should().ContainSingle(d =>
			d.Severity == Severity.Warning
			&& d.Message.Contains(":height: must be a positive integer"));
		Html.Should().Contain("height:400px");
	}
}

public class StorybookMissingRootTests(ITestOutputHelper output) : DirectiveTest<StorybookBlock>(output,
"""
:::{storybook}
:::
"""
)
{
	[Fact]
	public void EmitsError() =>
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("requires a :root: property or docset.yml storybook.root"));
}

public class StorybookMissingIdTests(ITestOutputHelper output) : DirectiveTest<StorybookBlock>(output,
"""
:::{storybook}
:root: /storybook/my-lib
:::
"""
)
{
	[Fact]
	public void EmitsError() =>
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("requires an :id: property"));
}

public class StorybookExternalUrlTests(ITestOutputHelper output) : DirectiveTest<StorybookBlock>(output,
"""
:::{storybook}
:root: https://evil.example/storybook/my-lib
:id: components-button--primary
:::
"""
)
{
	[Fact]
	public void EmitsError() =>
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("storybook.allowed_roots"));
}

public class StorybookProtocolRelativeUrlTests(ITestOutputHelper output) : DirectiveTest<StorybookBlock>(output,
"""
:::{storybook}
:root: //evil.example/storybook/my-lib
:id: components-button--primary
:::
"""
)
{
	[Fact]
	public void EmitsError() =>
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("storybook.allowed_roots"));
}

public class StorybookRootWithIframePathTests(ITestOutputHelper output) : DirectiveTest<StorybookBlock>(output,
"""
:::{storybook}
:root: /storybook/my-lib/iframe.html
:id: components-button--primary
:::
"""
)
{
	[Fact]
	public void EmitsError() =>
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("should point to the Storybook root"));
}

public class StorybookRootWithQueryTests(ITestOutputHelper output) : DirectiveTest<StorybookBlock>(output,
"""
:::{storybook}
:root: /storybook/my-lib?foo=bar
:id: components-button--primary
:::
"""
)
{
	[Fact]
	public void EmitsError() =>
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("must not contain query string"));
}

public class StorybookPositionalArgumentWarningTests(ITestOutputHelper output) : DirectiveTest<StorybookBlock>(output,
"""
:::{storybook} /storybook/ignored
:root: /storybook/my-lib
:id: components-button--primary
:::
"""
)
{
	[Fact]
	public void EmitsWarning() =>
		Collector.Diagnostics.Should().ContainSingle(d =>
			d.Severity == Severity.Warning
			&& d.Message.Contains("ignores positional arguments"));
}
