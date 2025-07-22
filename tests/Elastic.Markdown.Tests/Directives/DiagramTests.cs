// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.Directives.Diagram;
using FluentAssertions;
using System.IO.Abstractions.TestingHelpers;

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
	public void RendersImageTag() => Html.Should().Contain("<img src=\"images/generated-graphs/");

	[Fact]
	public void GeneratesContentHash() => Block!.ContentHash.Should().NotBeNullOrEmpty();

	[Fact]
	public void GeneratesLocalSvgPath() => Block!.LocalSvgPath.Should().Contain("images/generated-graphs/");

	[Fact]
	public void LocalSvgPathContainsHash() => Block!.LocalSvgPath.Should().Contain(Block!.ContentHash!);

	[Fact]
	public void LocalSvgPathContainsDiagramType() => Block!.LocalSvgPath.Should().Contain("-diagram-mermaid-");

	[Fact]
	public void RendersLocalPathWithFallback() => Html.Should().Contain("onerror=\"this.src='https://kroki.io/mermaid/svg/");
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

public class DiagramRegistryTests
{
	[Fact]
	public void ClearResetsRegistry()
	{
		// Arrange
		DiagramRegistry.RegisterDiagram("test-path.svg");

		// Act
		DiagramRegistry.Clear();

		// Assert - registry should be empty, cleanup should not find any files to remove
		var fileSystem = new MockFileSystem();
		var cleanedCount = DiagramRegistry.CleanupUnusedDiagrams("/test", fileSystem);
		cleanedCount.Should().Be(0);
	}

	[Fact]
	public void CleanupRemovesUnusedFiles()
	{
		// Arrange
		DiagramRegistry.Clear();
		DiagramRegistry.RegisterDiagram("images/generated-graphs/active-diagram.svg");

		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			["/output/images/generated-graphs/active-diagram.svg"] = new MockFileData("<svg>active</svg>"),
			["/output/images/generated-graphs/unused-diagram.svg"] = new MockFileData("<svg>unused</svg>"),
			["/output/images/generated-graphs/another-unused.svg"] = new MockFileData("<svg>another</svg>")
		});

		// Act
		var cleanedCount = DiagramRegistry.CleanupUnusedDiagrams("/output", fileSystem);

		// Assert
		cleanedCount.Should().Be(2);
		fileSystem.File.Exists("/output/images/generated-graphs/active-diagram.svg").Should().BeTrue();
		fileSystem.File.Exists("/output/images/generated-graphs/unused-diagram.svg").Should().BeFalse();
		fileSystem.File.Exists("/output/images/generated-graphs/another-unused.svg").Should().BeFalse();
	}

	[Fact]
	public void CleanupHandlesMissingDirectory()
	{
		// Arrange
		DiagramRegistry.Clear();
		var fileSystem = new MockFileSystem();

		// Act & Assert - should not throw
		var cleanedCount = DiagramRegistry.CleanupUnusedDiagrams("/nonexistent", fileSystem);
		cleanedCount.Should().Be(0);
	}

	[Fact]
	public void CleanupRemovesEmptyDirectories()
	{
		// Arrange
		DiagramRegistry.Clear();
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			["/output/images/generated-graphs/unused.svg"] = new MockFileData("<svg>unused</svg>")
		});

		// Verify initial state
		fileSystem.Directory.Exists("/output/images/generated-graphs").Should().BeTrue();
		fileSystem.File.Exists("/output/images/generated-graphs/unused.svg").Should().BeTrue();

		// Act
		var cleanedCount = DiagramRegistry.CleanupUnusedDiagrams("/output", fileSystem);

		// Assert
		cleanedCount.Should().Be(1);
		fileSystem.File.Exists("/output/images/generated-graphs/unused.svg").Should().BeFalse();
		fileSystem.Directory.Exists("/output/images/generated-graphs").Should().BeFalse();
		// Note: /output/images may still exist if MockFileSystem creates it as a parent directory
	}
}
