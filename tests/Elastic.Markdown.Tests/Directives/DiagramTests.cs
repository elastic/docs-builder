// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Diagram;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
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
	public void GeneratesLocalSvgUrl() => Block!.LocalSvgUrl.Should().Contain("images/generated-graphs/");

	[Fact]
	public void LocalSvgPathContainsHash() => Block!.LocalSvgUrl.Should().Contain(Block!.ContentHash!);

	[Fact]
	public void LocalSvgPathContainsDiagramType() => Block!.LocalSvgUrl.Should().Contain("-diagram-mermaid-");

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
	private MockFileSystem FileSystem { get; }

	private BuildContext Context { get; }

	private DiagramRegistry Registry { get; }

	public DiagramRegistryTests(ITestOutputHelper output)
	{
		var collector = new DiagnosticsCollector([]);
		var versionsConfig = new VersionsConfiguration
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>()
		};
		FileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "docs/index.md", new MockFileData($"# {nameof(DiagramRegistryTests)}") }
		}, new MockFileSystemOptions
		{
			CurrentDirectory = Paths.WorkingDirectoryRoot.FullName
		});
		var root = FileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs/"));
		FileSystem.GenerateDocSetYaml(root);
		Context = new BuildContext(collector, FileSystem, versionsConfig);
		Registry = new DiagramRegistry(new TestLoggerFactory(output), Context);
	}

	[Fact]
	public void CleanupUnusedDiagramsWithActiveAndUnusedFilesCleansOnlyUnused()
	{
		var localOutput = FileSystem.DirectoryInfo.New(Path.Combine(Context.DocumentationOutputDirectory.FullName, "output"));
		var file = FileSystem.FileInfo.New(Path.Combine(localOutput.FullName, "images", "generated-graphs", "active-diagram.svg"));
		Registry.RegisterDiagramForCaching(file, "http://example.com/active");

		FileSystem.AddDirectory(Path.Combine(localOutput.FullName, "images/generated-graphs"));
		FileSystem.AddFile(Path.Combine(localOutput.FullName, "images/generated-graphs/active-diagram.svg"), "active content");
		FileSystem.AddFile(Path.Combine(localOutput.FullName, "images/generated-graphs/unused-diagram.svg"), "unused content");

		var cleanedCount = Registry.CleanupUnusedDiagrams();

		cleanedCount.Should().Be(1);
		FileSystem.File.Exists(Path.Combine(localOutput.FullName, "images/generated-graphs/active-diagram.svg")).Should().BeTrue();
		FileSystem.File.Exists(Path.Combine(localOutput.FullName, "images/generated-graphs/unused-diagram.svg")).Should().BeFalse();
	}

	[Fact]
	public void CleanupUnusedDiagramsWithNonexistentDirectoryReturnsZero()
	{
		var cleanedCount = Registry.CleanupUnusedDiagrams();
		cleanedCount.Should().Be(0);
	}

	[Fact]
	public void CleanupUnusedDiagramsRemovesEmptyDirectories()
	{
		var localOutput = FileSystem.DirectoryInfo.New(Path.Combine(Context.DocumentationOutputDirectory.FullName, "output"));
		var file = FileSystem.FileInfo.New(Path.Combine(localOutput.FullName, "images", "generated-graphs", "unused.svg"));

		FileSystem.AddDirectory(file.Directory!.FullName);
		FileSystem.AddFile(file.FullName, "content");

		var cleanedCount = Registry.CleanupUnusedDiagrams();

		cleanedCount.Should().Be(1);
		FileSystem.Directory.Exists(file.Directory.FullName).Should().BeFalse();
	}
}
