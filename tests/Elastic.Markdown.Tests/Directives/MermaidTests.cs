// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Myst.CodeBlocks;
using Markdig.Syntax;

namespace Elastic.Markdown.Tests.Directives;

public class MermaidFlowchartTests(ITestOutputHelper output) : DirectiveTest(output,
"""
```mermaid
flowchart LR
A[Start] --> B[Process]
B --> C[End]
```
"""
)
{
	private EnhancedCodeBlock? Block => Document.Descendants<EnhancedCodeBlock>().FirstOrDefault();

	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void HasMermaidLanguage() => Block!.Language.Should().Be("mermaid");

	[Fact]
	public void RendersMermaidContainer() => Html.Should().Contain("<div class=\"mermaid-container\">");

	[Fact]
	public void RendersInlineSvg() => Html.Should().Contain("<svg");

	[Fact]
	public void ContainsNodeLabels()
	{
		Html.Should().Contain("Start");
		Html.Should().Contain("Process");
		Html.Should().Contain("End");
	}
}

public class MermaidSequenceTests(ITestOutputHelper output) : DirectiveTest(output,
"""
```mermaid
sequenceDiagram
    participant A as Alice
    participant B as Bob
    A->>B: Hello Bob, how are you?
    B-->>A: Great!
```
"""
)
{
	private EnhancedCodeBlock? Block => Document.Descendants<EnhancedCodeBlock>().FirstOrDefault();

	[Fact]
	public void ParsesSequenceDiagram() => Block.Should().NotBeNull();

	[Fact]
	public void RendersMermaidContainer() => Html.Should().Contain("<div class=\"mermaid-container\">");

	[Fact]
	public void RendersInlineSvg() => Html.Should().Contain("<svg");

	[Fact]
	public void ContainsParticipantLabels()
	{
		Html.Should().Contain("Alice");
		Html.Should().Contain("Bob");
	}
}

public class MermaidStateDiagramTests(ITestOutputHelper output) : DirectiveTest(output,
"""
```mermaid
stateDiagram-v2
    [*] --> Idle
    Idle --> Processing: start
    Processing --> Complete: done
    Complete --> [*]
```
"""
)
{
	private EnhancedCodeBlock? Block => Document.Descendants<EnhancedCodeBlock>().FirstOrDefault();

	[Fact]
	public void ParsesStateDiagram() => Block.Should().NotBeNull();

	[Fact]
	public void RendersMermaidContainer() => Html.Should().Contain("<div class=\"mermaid-container\">");

	[Fact]
	public void RendersInlineSvg() => Html.Should().Contain("<svg");

	[Fact]
	public void ContainsStateLabels()
	{
		Html.Should().Contain("Idle");
		Html.Should().Contain("Processing");
		Html.Should().Contain("Complete");
	}
}

public class MermaidClassDiagramTests(ITestOutputHelper output) : DirectiveTest(output,
"""
```mermaid
classDiagram
    Animal <|-- Duck
    Animal <|-- Fish
    Animal : +int age
```
"""
)
{
	private EnhancedCodeBlock? Block => Document.Descendants<EnhancedCodeBlock>().FirstOrDefault();

	[Fact]
	public void ParsesClassDiagram() => Block.Should().NotBeNull();

	[Fact]
	public void RendersMermaidContainer() => Html.Should().Contain("<div class=\"mermaid-container\">");

	[Fact]
	public void RendersInlineSvg() => Html.Should().Contain("<svg");

	[Fact]
	public void ContainsClassLabels()
	{
		Html.Should().Contain("Animal");
		Html.Should().Contain("Duck");
		Html.Should().Contain("Fish");
	}
}

public class MermaidErDiagramTests(ITestOutputHelper output) : DirectiveTest(output,
"""
```mermaid
erDiagram
    CUSTOMER ||--o{ ORDER : places
    ORDER ||--|{ LINE_ITEM : contains
```
"""
)
{
	private EnhancedCodeBlock? Block => Document.Descendants<EnhancedCodeBlock>().FirstOrDefault();

	[Fact]
	public void ParsesErDiagram() => Block.Should().NotBeNull();

	[Fact]
	public void RendersMermaidContainer() => Html.Should().Contain("<div class=\"mermaid-container\">");

	[Fact]
	public void RendersInlineSvg() => Html.Should().Contain("<svg");

	[Fact]
	public void ContainsEntityLabels()
	{
		Html.Should().Contain("CUSTOMER");
		Html.Should().Contain("ORDER");
		Html.Should().Contain("LINE_ITEM");
	}
}

// classDef/style directives are banned by strict styling — diagram should warn and fall back to raw source.
public class MermaidStyledFlowchartTests(ITestOutputHelper output) : DirectiveTest(output,
"""
```mermaid
flowchart LR
A[Start] --> B[Process]
classDef elasticBlue fill:#0B64DD,stroke:#333,stroke-width:2px,color:#fff
class A elasticBlue
style B fill:#0A52B3,color:#fff
```
"""
)
{
	[Fact]
	public void EmitsWarning() => Collector.Diagnostics.Should().NotBeEmpty();

	[Fact]
	public void FallsBackToRawSource() => Html.Should().Contain("<pre class=\"mermaid-error\">");

	[Fact]
	public void DoesNotRenderSvg() => Html.Should().NotContain("<svg");
}

// Allowlisted semantic classes render correctly with site palette colors baked into SVG.
public class MermaidStrictClassTests(ITestOutputHelper output) : DirectiveTest(output,
"""
```mermaid
flowchart LR
A[Start]:::warning --> B[End]
```
"""
)
{
	[Fact]
	public void RendersMermaidContainer() => Html.Should().Contain("<div class=\"mermaid-container\">");

	[Fact]
	public void RendersInlineSvg() => Html.Should().Contain("<svg");

	[Fact]
	public void EmitsNoDiagnostics() => Collector.Diagnostics.Should().BeEmpty();

	[Fact]
	public void SvgContainsWarningFillColor() => Html.Should().Contain("#fdf3d8");
}

// DataPalette: pie chart SVG should use our theme palette, not the Tableau CB10 default.
public class MermaidPieDataPaletteTests(ITestOutputHelper output) : DirectiveTest(output,
"""
```mermaid
pie
"Blue" : 40
"Red" : 30
"Green" : 30
```
"""
)
{
	[Fact]
	public void RendersInlineSvg() => Html.Should().Contain("<svg");

	[Fact]
	public void EmitsNoDiagnostics() => Collector.Diagnostics.Should().BeEmpty();

	[Fact]
	public void UsesThemePalette() => Html.Should().Contain("#3788ff"); // blue-elastic-70

	[Fact]
	public void DoesNotUseTableauDefault() => Html.Should().NotContain("#4e79a7"); // Tableau Blue
}
