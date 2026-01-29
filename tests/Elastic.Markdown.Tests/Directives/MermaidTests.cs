// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.Directives.Mermaid;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

public class MermaidFlowchartTests(ITestOutputHelper output) : DirectiveTest<MermaidBlock>(output,
"""
::::{mermaid}
flowchart LR
  A[Start] --> B[Process]
  B --> C[End]
::::
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void ExtractsContent() => Block!.Content.Should().Contain("flowchart LR");

	[Fact]
	public void GeneratesRenderedHtml() => Block!.RenderedHtml.Should().StartWith("<svg");

	[Fact]
	public void RendersSvgInDiv() => Html.Should().Contain("<div class=\"mermaid\">");

	[Fact]
	public void RendersInlineSvg() => Html.Should().Contain("<svg");

	[Fact]
	public void SvgHasNoGoogleFontsImport() => Block!.RenderedHtml.Should().NotContain("fonts.googleapis.com");

	[Fact]
	public void IsNotClientSide() => Block!.IsClientSide.Should().BeFalse();
}

public class MermaidSequenceTests(ITestOutputHelper output) : DirectiveTest<MermaidBlock>(output,
"""
::::{mermaid}
sequenceDiagram
    participant A as Alice
    participant B as Bob
    A->>B: Hello!
::::
"""
)
{
	[Fact]
	public void ParsesSequenceDiagram() => Block.Should().NotBeNull();

	[Fact]
	public void ExtractsSequenceContent() => Block!.Content.Should().Contain("sequenceDiagram");

	[Fact]
	public void GeneratesRenderedHtml() => Block!.RenderedHtml.Should().StartWith("<svg");
}

public class MermaidStateDiagramTests(ITestOutputHelper output) : DirectiveTest<MermaidBlock>(output,
"""
::::{mermaid}
stateDiagram-v2
    [*] --> Idle
    Idle --> Processing: start
    Processing --> Complete: done
    Complete --> [*]
::::
"""
)
{
	[Fact]
	public void ParsesStateDiagram() => Block.Should().NotBeNull();

	[Fact]
	public void ExtractsStateContent() => Block!.Content.Should().Contain("stateDiagram-v2");

	[Fact]
	public void GeneratesRenderedHtml() => Block!.RenderedHtml.Should().StartWith("<svg");

	[Fact]
	public void ContainsStateNodes() => Block!.RenderedHtml.Should().Contain("Idle");
}

public class MermaidClassDiagramTests(ITestOutputHelper output) : DirectiveTest<MermaidBlock>(output,
"""
::::{mermaid}
classDiagram
    Animal <|-- Duck
    Animal: +int age
    Duck: +quack()
::::
"""
)
{
	[Fact]
	public void ParsesClassDiagram() => Block.Should().NotBeNull();

	[Fact]
	public void ExtractsClassContent() => Block!.Content.Should().Contain("classDiagram");

	[Fact]
	public void GeneratesRenderedHtml() => Block!.RenderedHtml.Should().StartWith("<svg");

	[Fact]
	public void ContainsClassNames() => Block!.RenderedHtml.Should().Contain("Animal");
}

public class MermaidErDiagramTests(ITestOutputHelper output) : DirectiveTest<MermaidBlock>(output,
"""
::::{mermaid}
erDiagram
    CUSTOMER ||--o{ ORDER : places
    ORDER ||--|{ LINE_ITEM : contains
::::
"""
)
{
	[Fact]
	public void ParsesErDiagram() => Block.Should().NotBeNull();

	[Fact]
	public void ExtractsErContent() => Block!.Content.Should().Contain("erDiagram");

	[Fact]
	public void GeneratesRenderedHtml() => Block!.RenderedHtml.Should().StartWith("<svg");

	[Fact]
	public void ContainsEntityNames() => Block!.RenderedHtml.Should().Contain("CUSTOMER");
}

public class MermaidEmptyContentTests(ITestOutputHelper output) : DirectiveTest<MermaidBlock>(output,
"""
::::{mermaid}
::::
"""
)
{
	[Fact]
	public void EmptyContentGeneratesError() =>
		Collector.Diagnostics.Should().ContainSingle(d => d.Message.Contains("Mermaid directive requires content"));
}

public class MermaidInvalidSyntaxTests(ITestOutputHelper output) : DirectiveTest<MermaidBlock>(output,
"""
::::{mermaid}
invalid syntax here
::::
"""
)
{
	[Fact]
	public void InvalidSyntaxGeneratesError() =>
		Collector.Diagnostics.Should().ContainSingle(d => d.Message.Contains("Failed to render Mermaid diagram"));

	[Fact]
	public void RenderedHtmlIsNull() => Block!.RenderedHtml.Should().BeNull();
}
