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
	public void RendersImgElement() => Html.Should().Contain("<img");

	[Fact]
	public void EmitsExternalSvgFile() => ReadMermaidSvgs().Should().NotBeEmpty();

	[Fact]
	public void SvgContainsNodeLabels()
	{
		var svg = ReadMermaidSvgs()[0];
		svg.Should().Contain("Start");
		svg.Should().Contain("Process");
		svg.Should().Contain("End");
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
	public void RendersImgElement() => Html.Should().Contain("<img");

	[Fact]
	public void SvgContainsParticipantLabels()
	{
		var svg = ReadMermaidSvgs()[0];
		svg.Should().Contain("Alice");
		svg.Should().Contain("Bob");
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
	public void RendersImgElement() => Html.Should().Contain("<img");

	[Fact]
	public void SvgContainsStateLabels()
	{
		var svg = ReadMermaidSvgs()[0];
		svg.Should().Contain("Idle");
		svg.Should().Contain("Processing");
		svg.Should().Contain("Complete");
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
	public void RendersImgElement() => Html.Should().Contain("<img");

	[Fact]
	public void SvgContainsClassLabels()
	{
		var svg = ReadMermaidSvgs()[0];
		svg.Should().Contain("Animal");
		svg.Should().Contain("Duck");
		svg.Should().Contain("Fish");
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
	public void RendersImgElement() => Html.Should().Contain("<img");

	[Fact]
	public void SvgContainsEntityLabels()
	{
		var svg = ReadMermaidSvgs()[0];
		svg.Should().Contain("CUSTOMER");
		svg.Should().Contain("ORDER");
		svg.Should().Contain("LINE_ITEM");
	}
}

// classDef/style directives are stripped by strict styling (Strip mode) — diagram still renders as SVG,
// each stripped item fires OnStripped as a hint.
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
	public void EmitsHints() => Collector.Diagnostics.Should().NotBeEmpty();

	[Fact]
	public void EmitsSvgFile() => ReadMermaidSvgs().Should().NotBeEmpty();

	[Fact]
	public void DoesNotFallBackToRawSource() => Html.Should().NotContain("<pre class=\"mermaid-error\">");
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
	public void RendersImgElement() => Html.Should().Contain("<img");

	[Fact]
	public void EmitsNoDiagnostics() => Collector.Diagnostics.Should().BeEmpty();

	[Fact]
	public void SvgContainsWarningFillColor() => ReadMermaidSvgs()[0].Should().Contain("#fdf3d8");
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
	public void RendersImgElement() => Html.Should().Contain("<img");

	[Fact]
	public void EmitsNoDiagnostics() => Collector.Diagnostics.Should().BeEmpty();

	[Fact]
	public void UsesThemePalette() => ReadMermaidSvgs()[0].Should().Contain("#3788ff"); // blue-elastic-70

	[Fact]
	public void DoesNotUseTableauDefault() => ReadMermaidSvgs()[0].Should().NotContain("#4e79a7"); // Tableau Blue
}
