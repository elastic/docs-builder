// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using AwesomeAssertions;
using Elastic.Markdown.Exporters;

namespace Elastic.Markdown.Tests.Exporters;

public class LlmMarkdownExporterTests
{
	[Fact]
	public async Task FinishExportAsync_InMemoryFileSystem_CreatesArchiveFromInMemoryFiles()
	{
		const string outputPath = "/repo/.artifacts/docs/html";
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			[$"{outputPath}/guide/page.md"] = new("# Page")
		});
		var outputFolder = fileSystem.DirectoryInfo.New(outputPath);
		var exporter = new LlmMarkdownExporter();

		var result = await exporter.FinishExportAsync(outputFolder, TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		fileSystem.File.Exists($"{outputPath}/llm.zip").Should().BeTrue();
		await using var zipStream = fileSystem.File.OpenRead($"{outputPath}/llm.zip");
		using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
		archive.Entries.Select(entry => entry.FullName).Should().BeEquivalentTo("llms.txt", "guide/page.md");
	}
}
