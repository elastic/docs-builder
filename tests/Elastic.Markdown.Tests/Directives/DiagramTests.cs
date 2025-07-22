// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration.Diagram;
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
	public void RendersImageTag() => Html.Should().Contain("<img src=\"/images/generated-graphs/");

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
	public void CleanupUnusedDiagramsWithNoActiveFilesCleansAllFiles()
	{
		var fileSystem = new MockFileSystem();
		var registry = new DiagramRegistry(fileSystem);
		registry.RegisterDiagramForCaching("test-path.svg", "http://example.com/test", "/test");

		// Clear registry to simulate no active diagrams
		registry.Clear();

		var cleanedCount = registry.CleanupUnusedDiagrams(fileSystem.DirectoryInfo.New("/test"));

		cleanedCount.Should().Be(0); // No files to clean since directory doesn't exist
	}

	[Fact]
	public void CleanupUnusedDiagramsWithActiveAndUnusedFilesCleansOnlyUnused()
	{
		var fileSystem = new MockFileSystem();
		var registry = new DiagramRegistry(fileSystem);
		registry.Clear();
		registry.RegisterDiagramForCaching("images/generated-graphs/active-diagram.svg", "http://example.com/active", "/output");

		fileSystem.AddDirectory("/output/images/generated-graphs");
		fileSystem.AddFile("/output/images/generated-graphs/active-diagram.svg", "active content");
		fileSystem.AddFile("/output/images/generated-graphs/unused-diagram.svg", "unused content");

		var cleanedCount = registry.CleanupUnusedDiagrams(fileSystem.DirectoryInfo.New("/output"));

		cleanedCount.Should().Be(1);
		fileSystem.File.Exists("/output/images/generated-graphs/active-diagram.svg").Should().BeTrue();
		fileSystem.File.Exists("/output/images/generated-graphs/unused-diagram.svg").Should().BeFalse();
	}

	[Fact]
	public void CleanupUnusedDiagramsWithNonexistentDirectoryReturnsZero()
	{
		var fileSystem = new MockFileSystem();
		var registry = new DiagramRegistry(fileSystem);
		registry.Clear();

		var cleanedCount = registry.CleanupUnusedDiagrams(fileSystem.DirectoryInfo.New("/nonexistent"));

		cleanedCount.Should().Be(0);
	}

	[Fact]
	public void CleanupUnusedDiagramsRemovesEmptyDirectories()
	{
		var fileSystem = new MockFileSystem();
		var registry = new DiagramRegistry(fileSystem);
		registry.Clear();

		fileSystem.AddDirectory("/output/images/generated-graphs/subdir");
		fileSystem.AddFile("/output/images/generated-graphs/subdir/unused.svg", "content");

		var cleanedCount = registry.CleanupUnusedDiagrams(fileSystem.DirectoryInfo.New("/output"));

		cleanedCount.Should().Be(1);
		fileSystem.Directory.Exists("/output/images/generated-graphs/subdir").Should().BeFalse();
	}
}
