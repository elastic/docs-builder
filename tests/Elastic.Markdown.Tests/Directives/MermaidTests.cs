// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.CodeBlocks;
using FluentAssertions;
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
	public void RendersPreMermaidTag() => Html.Should().Contain("<pre class=\"mermaid\">");

	[Fact]
	public void ContainsDiagramContent() => Html.Should().Contain("flowchart LR");

	[Fact]
	public void PreservesArrows() => Html.Should().Contain("--&gt;");
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
	public void RendersPreMermaidTag() => Html.Should().Contain("<pre class=\"mermaid\">");

	[Fact]
	public void PreservesIndentation() => Html.Should().Contain("participant A as Alice");
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
	public void RendersPreMermaidTag() => Html.Should().Contain("<pre class=\"mermaid\">");

	[Fact]
	public void PreservesSpecialCharacters() => Html.Should().Contain("[*]");
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
	public void RendersPreMermaidTag() => Html.Should().Contain("<pre class=\"mermaid\">");

	[Fact]
	public void PreservesInheritanceArrow() => Html.Should().Contain("&lt;|--");
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
	public void RendersPreMermaidTag() => Html.Should().Contain("<pre class=\"mermaid\">");

	[Fact]
	public void PreservesErSyntax() => Html.Should().Contain("||--o{");
}
