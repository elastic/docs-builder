// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using AwesomeAssertions;
using Elastic.Documentation.Testing;
using Elastic.Markdown.Myst.CodeBlocks;
using Markdig.Syntax;
using Nullean.Snapshots;
using Nullean.Snapshots.Html;

namespace Elastic.Markdown.Tests.Directives;

internal static class MermaidSnapshots
{
	/// <summary>The diagram file name carries a hash of the rendered SVG, which changes with the renderer, not with the markup.</summary>
	public static SnapshotOptions Options => new SnapshotOptions().Scrub(new Regex(@"index-[0-9a-f]{8}\.svg"), "index-{hash}.svg");
}

public class MermaidFlowchartTests() : DirectiveTest("""
```mermaid
flowchart LR
A[Start] --> B[Process]
B --> C[End]
```
""")
{
	private EnhancedCodeBlock? Block => Document.Descendants<EnhancedCodeBlock>().FirstOrDefault();

	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void HasMermaidLanguage() => Block!.Language.Should().Be("mermaid");

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			options: MermaidSnapshots.Options,
			expected: """
			<div class="mermaid-container">
				<div class="mermaid-viewport">
					<div class="mermaid-rendered" data-src="/index-{hash}.svg">
						<img src="/index-{hash}.svg" alt="Mermaid diagram">
					</div>
				</div>
			</div>
			"""
		);

	[Test]
	public void EmitsExternalSvgFile() => ReadMermaidSvgs().Should().NotBeEmpty();

	[Test]
	public void SvgContainsNodeLabels()
	{
		var svg = ReadMermaidSvgs()[0];
		svg.Should().Contain("Start");
		svg.Should().Contain("Process");
		svg.Should().Contain("End");
	}
}

public class MermaidSequenceTests() : DirectiveTest(
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

	[Test]
	public void ParsesSequenceDiagram() => Block.Should().NotBeNull();

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			options: MermaidSnapshots.Options,
			expected: """
			<div class="mermaid-container">
				<div class="mermaid-viewport">
					<div class="mermaid-rendered" data-src="/index-{hash}.svg">
						<img src="/index-{hash}.svg" alt="Mermaid diagram">
					</div>
				</div>
			</div>
			"""
		);

	[Test]
	public void SvgContainsParticipantLabels()
	{
		var svg = ReadMermaidSvgs()[0];
		svg.Should().Contain("Alice");
		svg.Should().Contain("Bob");
	}
}

public class MermaidStateDiagramTests() : DirectiveTest(
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

	[Test]
	public void ParsesStateDiagram() => Block.Should().NotBeNull();

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			options: MermaidSnapshots.Options,
			expected: """
			<div class="mermaid-container">
				<div class="mermaid-viewport">
					<div class="mermaid-rendered" data-src="/index-{hash}.svg">
						<img src="/index-{hash}.svg" alt="Mermaid diagram">
					</div>
				</div>
			</div>
			"""
		);

	[Test]
	public void SvgContainsStateLabels()
	{
		var svg = ReadMermaidSvgs()[0];
		svg.Should().Contain("Idle");
		svg.Should().Contain("Processing");
		svg.Should().Contain("Complete");
	}
}

public class MermaidClassDiagramTests() : DirectiveTest(
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

	[Test]
	public void ParsesClassDiagram() => Block.Should().NotBeNull();

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			options: MermaidSnapshots.Options,
			expected: """
			<div class="mermaid-container">
				<div class="mermaid-viewport">
					<div class="mermaid-rendered" data-src="/index-{hash}.svg">
						<img src="/index-{hash}.svg" alt="Mermaid diagram">
					</div>
				</div>
			</div>
			"""
		);

	[Test]
	public void SvgContainsClassLabels()
	{
		var svg = ReadMermaidSvgs()[0];
		svg.Should().Contain("Animal");
		svg.Should().Contain("Duck");
		svg.Should().Contain("Fish");
	}
}

public class MermaidErDiagramTests() : DirectiveTest(
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

	[Test]
	public void ParsesErDiagram() => Block.Should().NotBeNull();

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			options: MermaidSnapshots.Options,
			expected: """
			<div class="mermaid-container">
				<div class="mermaid-viewport">
					<div class="mermaid-rendered" data-src="/index-{hash}.svg">
						<img src="/index-{hash}.svg" alt="Mermaid diagram">
					</div>
				</div>
			</div>
			"""
		);

	[Test]
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
public class MermaidStyledFlowchartTests() : DirectiveTest(
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
	[Test]
	public void EmitsHints() => Collector.Diagnostics.Should().NotBeEmpty();

	[Test]
	public void EmitsSvgFile() => ReadMermaidSvgs().Should().NotBeEmpty();

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			options: MermaidSnapshots.Options,
			expected: """
			<div class="mermaid-container">
				<div class="mermaid-viewport">
					<div class="mermaid-rendered" data-src="/index-{hash}.svg">
						<img src="/index-{hash}.svg" alt="Mermaid diagram">
					</div>
				</div>
			</div>
			"""
		);
}

// Allowlisted semantic classes render correctly with site palette colors baked into SVG.
public class MermaidStrictClassTests() : DirectiveTest("""
```mermaid
flowchart LR
A[Start]:::warning --> B[End]
```
""")
{
	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			options: MermaidSnapshots.Options,
			expected: """
			<div class="mermaid-container">
				<div class="mermaid-viewport">
					<div class="mermaid-rendered" data-src="/index-{hash}.svg">
						<img src="/index-{hash}.svg" alt="Mermaid diagram">
					</div>
				</div>
			</div>
			"""
		);

	[Test]
	public void EmitsNoDiagnostics() => Collector.Diagnostics.Should().BeEmpty();

	[Test]
	public void SvgContainsWarningFillColor() => ReadMermaidSvgs()[0].Should().Contain("#fdf3d8");
}

// DataPalette: pie chart SVG should use our theme palette, not the Tableau CB10 default.
public class MermaidPieDataPaletteTests() : DirectiveTest("""
```mermaid
pie
"Blue" : 40
"Red" : 30
"Green" : 30
```
""")
{
	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			options: MermaidSnapshots.Options,
			expected: """
			<div class="mermaid-container">
				<div class="mermaid-viewport">
					<div class="mermaid-rendered" data-src="/index-{hash}.svg">
						<img src="/index-{hash}.svg" alt="Mermaid diagram">
					</div>
				</div>
			</div>
			"""
		);

	[Test]
	public void EmitsNoDiagnostics() => Collector.Diagnostics.Should().BeEmpty();

	[Test]
	public void UsesThemePalette() => ReadMermaidSvgs()[0].Should().Contain("#3788ff"); // blue-elastic-70

	[Test]
	public void DoesNotUseTableauDefault() => ReadMermaidSvgs()[0].Should().NotContain("#4e79a7"); // Tableau Blue
}
