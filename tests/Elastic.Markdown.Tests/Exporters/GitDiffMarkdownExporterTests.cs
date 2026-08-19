// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.GitDiff;
using Elastic.Documentation.Serialization;
using Elastic.Markdown.Exporters.GitDiff;
using Elastic.Markdown.IO;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Markdown.Tests.Exporters;

public class GitDiffMarkdownExporterTests(ITestOutputHelper output)
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

	[Fact]
	public async Task FinishExportAsync_WritesChangedPageFromCiFileList()
	{
		var export = await GenerateChangedPages(
			environment: new FakeEnvironmentVariables(new Dictionary<string, string?>
			{
				["MODIFIED_FILES"] = "docs/index.md",
				["DOCS_DIFF_BASE"] = "origin/main"
			}),
			gitCommand: static _ => throw new InvalidOperationException("git should not run when a CI file list is set")
		);

		export.Base.Should().Be("origin/main");
		export.Pages.Should().ContainSingle(p => p.SourcePath == "index.md" && p.Change == "modified");
		export.Pages[0].Url.Should().NotBeNullOrEmpty();
		export.Pages[0].Title.Should().NotBeNullOrEmpty();
	}

	[Fact]
	public async Task FinishExportAsync_MapsSnippetGitDiffOntoIncludingPage()
	{
		var export = await GenerateChangedPages(
			environment: new FakeEnvironmentVariables(new Dictionary<string, string?>
			{
				["DOCS_DIFF_BASE"] = "origin/main"
			}),
			gitCommand: static args => args[0] == "diff"
				? "M\u0000docs/_snippets/shared.md\u0000"
				: string.Empty
		);

		export.Pages.Should().ContainSingle(p => p.SourcePath == "index.md");
		export.Pages[0].IncludedFrom.Should().Contain("_snippets/shared.md");
		export.Pages[0].Change.Should().Be("modified");
	}

	private async Task<ChangedPagesExport> GenerateChangedPages(
		FakeEnvironmentVariables environment,
		Func<string[], string> gitCommand
	)
	{
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			["docs/docset.yml"] = new("""
				project: test
				toc:
				- file: index.md
				"""),
			["docs/index.md"] = new("""
				# Get started

				:::{include} _snippets/shared.md
				:::
				"""),
			["docs/_snippets/shared.md"] = new("shared text")
		}, new MockFileSystemOptions
		{
			CurrentDirectory = Paths.WorkingDirectoryRoot.FullName
		});

		var collector = new TestDiagnosticsCollector(output);
		_ = collector.StartAsync(TestContext.Current.CancellationToken);
		var context = new BuildContext(
			collector,
			TestHelpers.CreateDocumentationFileSystem(fileSystem),
			TestHelpers.CreateConfigurationContext(fileSystem),
			environment
		)
		{
			Force = true
		};
		var exporter = new GitDiffMarkdownExporter(NullLoggerFactory.Instance, gitCommand);
		var set = new DocumentationSet(context, NullLoggerFactory.Instance, new TestCrossLinkResolver());
		var generator = new DocumentationGenerator(set, NullLoggerFactory.Instance, markdownExporters: [exporter]);

		_ = await generator.GenerateAll(TestContext.Current.CancellationToken);
		_ = await exporter.FinishExportAsync(context.OutputDirectory, TestContext.Current.CancellationToken);

		var json = fileSystem.File.ReadAllText(
			Path.Join(context.OutputDirectory.FullName, ChangedPagesExportFile.FileName));
		return JsonSerializer.Deserialize(json, SourceGenerationContext.Default.ChangedPagesExport)
			?? throw new InvalidOperationException("changed-pages.json did not deserialize");
	}
}
