// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.IO;

namespace Elastic.Markdown.Tests;

public class PageTitleTests()
{
	[Test]
	public async Task GenerateAll_MetaTitle_OverridesPageTitle()
	{
		var html = await Generate(
			BuildType.Assembler,
			"""
			---
			meta_title: Elasticsearch query language
			---

			# Query DSL
			"""
		);

		html.Should().Contain("<title>Elasticsearch query language | Query DSL</title>");
		html.Should().Contain("<meta property=\"og:title\" content=\"Elasticsearch query language | Query DSL\"");
		html.Should().Contain("<h1>Query DSL</h1>");
	}

	[Test]
	public async Task GenerateAll_WithoutMetaTitle_UsesH1()
	{
		var html = await Generate(BuildType.Assembler, "# Query DSL");

		html.Should().Contain("<title>Query DSL | Query DSL</title>");
	}

	[Test]
	public async Task GenerateAll_BlankMetaTitle_UsesH1()
	{
		var html = await Generate(
			BuildType.Assembler,
			"""
			---
			meta_title: " "
			---

			# Query DSL
			"""
		);

		html.Should().Contain("<title>Query DSL | Query DSL</title>");
	}

	[Test]
	public async Task GenerateAll_HeadingLikeContentBeforeH1_UsesParsedH1()
	{
		var html = await Generate(
			BuildType.Assembler,
			markdown: """
				---
				description: |
				  # Internal note
				---

				```text
				# Code example
				```

				# Real page title
				"""
		);

		html.Should().Contain("<title>Real page title | Real page title</title>");
	}

	[Test]
	public async Task GenerateAll_FormattedH1_PreservesVisibleFormatting()
	{
		var html = await Generate(BuildType.Assembler, markdown: "# Install `ecctl` *quickly*");

		html.Should().Contain("<title>Install ecctl quickly | Install ecctl quickly</title>");
		html.Should().Contain("<h1>Install <code>ecctl</code> <em>quickly</em></h1>");
	}

	[Test]
	public async Task RenderPreservingFirstHeadingWithMetadata_NormalizesMetaTitle()
	{
		const string markdown =
			"""
				---
				meta_title: "Search {{product}} *API*"
				sub:
				  product: Elasticsearch
				---

				# Search APIs
				""";
		var fileSystem = new MockFileSystem(
			new Dictionary<string, MockFileData> { ["docs/docset.yml"] = new("project: test"), ["docs/index.md"] = new(markdown) },
			new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName }
		);
		await using var collector = new DiagnosticsCollector([]).StartAsync(TestContext.Current!.Execution.CancellationToken);
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var context = new BuildContext(collector, TestHelpers.CreateDocumentationFileSystem(fileSystem), configurationContext);
		var set = new DocumentationSet(context, new TestLoggerFactory(), new TestCrossLinkResolver());
		var generator = new DocumentationGenerator(set, new TestLoggerFactory());

		var result = generator.MarkdownStringRenderer.RenderPreservingFirstHeadingWithMetadata(
			markdown,
			fileSystem.FileInfo.New("docs/index.md")
		);

		result.Title.Should().Be("Search APIs");
		result.MetaTitle.Should().Be("Search Elasticsearch API");
	}

	private async Task<string> Generate(BuildType buildType, string markdown, bool branded = false)
	{
		var branding = branded ? """
			branding:
			  icon: assets/logo.svg
			""" : string.Empty;
		var fileSystem = new MockFileSystem(
			new Dictionary<string, MockFileData>
			{
				["docs/docset.yml"] = new(
					$"""
					project: test
					toc:
					- file: index.md
					{branding}
					"""
				),
				["docs/index.md"] = new(markdown),
				["docs/assets/logo.svg"] = new("<svg/>")
			},
			new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName }
		);
		await using var collector = new DiagnosticsCollector([]).StartAsync(TestContext.Current!.Execution.CancellationToken);
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var context = new BuildContext(collector, TestHelpers.CreateDocumentationFileSystem(fileSystem), configurationContext)
		{
			BuildType = buildType
		};
		var set = new DocumentationSet(context, new TestLoggerFactory(), new TestCrossLinkResolver());
		var generator = new DocumentationGenerator(set, new TestLoggerFactory());

		await generator.GenerateAll(TestContext.Current!.Execution.CancellationToken);
		await collector.StopAsync(TestContext.Current!.Execution.CancellationToken);

		return fileSystem.File.ReadAllText(Path.Join(set.OutputDirectory.FullName, "index.html"));
	}
}
