// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Markdown.Exporters;

namespace Elastic.Markdown.Tests.Exporters;

public class LlmMarkdownExporterTests(ITestOutputHelper output)
{
	[Fact]
	public async Task FinishExportAsync_InMemoryFileSystem_CreatesArchiveFromInMemoryFiles()
	{
		const string outputPath = "/repo/.artifacts/docs/html";
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData> { [$"{outputPath}/guide/page.md"] = new("# Page") });
		var outputFolder = fileSystem.DirectoryInfo.New(outputPath);
		var exporter = new LlmMarkdownExporter();

		var result = await exporter.FinishExportAsync(outputFolder, TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		fileSystem.File.Exists($"{outputPath}/llm.zip").Should().BeTrue();
		await using var zipStream = fileSystem.File.OpenRead($"{outputPath}/llm.zip");
		using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
		archive.Entries.Select(entry => entry.FullName).Should().BeEquivalentTo("llms.txt", "guide/page.md");
	}

	[Fact]
	public void CreateDocumentationResources_PublicDocsBuild_ReturnsCalloutBeforeContent()
	{
		var context = CreateBuildContext(DocSetRegistry.Public, BuildType.Isolated);

		var resources = LlmMarkdownExporter.CreateDocumentationResources(context);

		resources.Should().Be(
			"""
			> ## Documentation resources
			>
			> Fetch the complete documentation index at: https://www.elastic.co/docs/llms.txt
			> Use this file to discover all available pages before exploring further.
			>
			> For targeted search and retrieval, use the free Elastic Docs MCP server at: https://www.elastic.co/docs/_mcp/
			> The server provides tools to search, discover related pages, and retrieve page content.

			"""
		);
	}

	[Fact]
	public void CreateDocumentationResources_InternalDocsBuild_ReturnsEmpty()
	{
		var context = CreateBuildContext(DocSetRegistry.Internal, BuildType.Isolated);

		var resources = LlmMarkdownExporter.CreateDocumentationResources(context);

		resources.Should().BeEmpty();
	}

	[Fact]
	public void CreateDocumentationResources_CodexBuild_ReturnsEmpty()
	{
		var context = CreateBuildContext(DocSetRegistry.Public, BuildType.Codex);

		var resources = LlmMarkdownExporter.CreateDocumentationResources(context);

		resources.Should().BeEmpty();
	}

	private BuildContext CreateBuildContext(DocSetRegistry registry, BuildType buildType)
	{
		var fileSystem = new MockFileSystem(
			new Dictionary<string, MockFileData>
			{
				["docs/docset.yml"] = new(
					$"""
					project: test
					registry: {registry.ToString().ToLowerInvariant()}
					toc:
					  - file: index.md
					"""
				),
				["docs/index.md"] = new("# Test")
			},
			new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName }
		);
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		return new BuildContext(
			new TestDiagnosticsCollector(output),
			TestHelpers.CreateDocumentationFileSystem(fileSystem),
			configurationContext,
			new PublicEnvironmentVariables()
		)
		{ BuildType = buildType, CanonicalBaseUrl = new Uri("https://www.elastic.co/"), UrlPathPrefix = "/docs" };
	}

	private sealed class PublicEnvironmentVariables : IEnvironmentVariables
	{
		public bool IsRunningOnCI => false;

		public string? GetEnvironmentVariable(string name) => null;
	}
}
