// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.Directives.Mermaid;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

public class MermaidBlockTests(ITestOutputHelper output) : DirectiveTest<MermaidBlock>(output,
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
	public void GeneratesRenderedSvg() => Block!.RenderedSvg.Should().StartWith("<svg");

	[Fact]
	public void RendersSvgInDiv() => Html.Should().Contain("<div class=\"mermaid\">");

	[Fact]
	public void RendersInlineSvg() => Html.Should().Contain("<svg");
}

public class MermaidBlockSequenceTests(ITestOutputHelper output) : DirectiveTest<MermaidBlock>(output,
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
	public void GeneratesRenderedSvg() => Block!.RenderedSvg.Should().StartWith("<svg");
}

public class MermaidBlockEmptyTests(ITestOutputHelper output) : DirectiveTest<MermaidBlock>(output,
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
