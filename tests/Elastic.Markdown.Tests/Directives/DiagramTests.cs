// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.Directives.Diagram;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

public class DiagramBlockTests(ITestOutputHelper output) : DirectiveTest<DiagramBlock>(output,
"""
::::{diagram} mermaid
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
	public void ExtractsDiagramType() => Block!.DiagramType.Should().Be("mermaid");

	[Fact]
	public void ExtractsContent() => Block!.Content.Should().Contain("flowchart LR");

	[Fact]
	public void GeneratesEncodedUrl() => Block!.EncodedUrl.Should().StartWith("https://kroki.io/mermaid/svg/");

	[Fact]
	public void RendersImageTag() => Html.Should().Contain("<img src=\"https://kroki.io/mermaid/svg/");
}

public class DiagramBlockD2Tests(ITestOutputHelper output) : DirectiveTest<DiagramBlock>(output,
"""
::::{diagram} d2
x -> y
y -> z
::::
"""
)
{
	[Fact]
	public void ParsesD2Block() => Block.Should().NotBeNull();

	[Fact]
	public void ExtractsD2Type() => Block!.DiagramType.Should().Be("d2");

	[Fact]
	public void ExtractsD2Content() => Block!.Content.Should().Contain("x -> y");

	[Fact]
	public void GeneratesD2EncodedUrl() => Block!.EncodedUrl.Should().StartWith("https://kroki.io/d2/svg/");
}

public class DiagramBlockDefaultTests(ITestOutputHelper output) : DirectiveTest<DiagramBlock>(output,
"""
::::{diagram}
graph TD
  A --> B
::::
"""
)
{
	[Fact]
	public void DefaultsToMermaid() => Block!.DiagramType.Should().Be("mermaid");

	[Fact]
	public void ExtractsContentWithoutType() => Block!.Content.Should().Contain("graph TD");
}

public class DiagramBlockEmptyTests(ITestOutputHelper output) : DirectiveTest<DiagramBlock>(output,
"""
::::{diagram}
::::
"""
)
{
	[Fact]
	public void EmptyContentGeneratesError() =>
		Collector.Diagnostics.Should().ContainSingle(d => d.Message.Contains("Diagram directive requires content"));
}
