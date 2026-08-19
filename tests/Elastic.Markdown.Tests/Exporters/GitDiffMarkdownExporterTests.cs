// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using AwesomeAssertions;
using Elastic.Documentation.GitDiff;
using Elastic.Documentation.Serialization;
using Elastic.Markdown.Exporters.GitDiff;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Markdown.Tests.Exporters;

public class GitDiffMarkdownExporterTests
{
	[Fact]
	public async Task FinishExportAsync_WithoutExportAsync_DoesNotWriteChangedPagesFile()
	{
		const string outputPath = "/repo/.artifacts/docs/html";
		var fileSystem = new MockFileSystem();
		var outputFolder = fileSystem.DirectoryInfo.New(outputPath);
		var exporter = new GitDiffMarkdownExporter(NullLoggerFactory.Instance);

		var result = await exporter.FinishExportAsync(outputFolder, TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		fileSystem.File.Exists($"{outputPath}/{ChangedPagesExportFile.FileName}").Should().BeFalse();
	}

	[Fact]
	public void Serialize_WritesSnakeCaseJson()
	{
		var export = new ChangedPagesExport
		{
			Base = "origin/main",
			ConfigChanged = false,
			Pages =
			[
				new ChangedPageEntry
				{
					SourcePath = "guides/start.md",
					Url = "/preview/guides/start",
					Title = "Get started",
					Change = "modified",
					IncludedFrom = []
				}
			],
			Deleted = []
		};

		var json = ChangedPagesExportFile.Serialize(export);
		using var document = JsonDocument.Parse(json);

		document.RootElement.GetProperty("base").GetString().Should().Be("origin/main");
		document.RootElement.GetProperty("config_changed").GetBoolean().Should().BeFalse();
		document.RootElement.GetProperty("pages")[0].GetProperty("source_path").GetString().Should().Be("guides/start.md");
		document.RootElement.GetProperty("pages")[0].GetProperty("included_from").GetArrayLength().Should().Be(0);
	}
}
