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

public class PageTitleTests(ITestOutputHelper output)
{
	[Fact]
	public async Task GenerateAll_MetaTitle_OverridesAutomaticProductTitle()
	{
		var html = await Generate(
			BuildType.Assembler,
			"""
			---
			meta_title: Elasticsearch query language
			products:
			  - id: elasticsearch
			---

			# Query DSL
			"""
		);

		html.Should().Contain("<title>Elasticsearch query language | Elastic Docs</title>");
		html.Should().Contain("<meta property=\"og:title\" content=\"Elasticsearch query language | Elastic Docs\"");
		html.Should().Contain("<meta data-pagefind-meta=\"title[content]\" content=\"Elasticsearch query language | Elastic Docs\"");
		html.Should().Contain("<h1>Query DSL</h1>");
	}

	[Fact]
	public async Task GenerateAll_DocsetProductMissingFromH1_AppendsInferredProductName()
	{
		var html = await Generate(BuildType.Assembler, "# Query DSL", docsetProduct: "elasticsearch");

		html.Should().Contain("<title>Query DSL - Elasticsearch | Elastic Docs</title>");
		html.Should().Contain("<meta property=\"og:title\" content=\"Query DSL - Elasticsearch | Elastic Docs\"");
		html.Should().Contain("<meta data-pagefind-meta=\"title[content]\" content=\"Query DSL - Elasticsearch | Elastic Docs\"");
		html.Should().Contain("<h1>Query DSL</h1>");
	}

	[Fact]
	public async Task GenerateAll_ProductAlreadyInH1_DoesNotAppendProductName()
	{
		var html = await Generate(
			BuildType.Assembler,
			"""
			---
			products:
			  - id: elasticsearch
			---

			# Elasticsearch query DSL
			"""
		);

		html.Should().Contain("<title>Elasticsearch query DSL | Elastic Docs</title>");
	}

	[Fact]
	public async Task GenerateAll_MultipleProducts_DoesNotChooseAProductName()
	{
		var html = await Generate(
			BuildType.Assembler,
			"""
			---
			products:
			  - id: elasticsearch
			  - id: kibana
			---

			# Query languages
			"""
		);

		html.Should().Contain("<title>Query languages | Elastic Docs</title>");
	}

	[Fact]
	public async Task GenerateAll_BrandedAssemblerBuild_KeepsExistingSuffix()
	{
		var html = await Generate(
			BuildType.Assembler,
			"""
			---
			products:
			  - id: elasticsearch
			---

			# Query DSL
			""",
			branded: true
		);

		html.Should().Contain("<title>Query DSL - Elasticsearch | Query DSL</title>");
		html.Should().NotContain("| Elastic Docs</title>");
	}

	[Fact]
	public async Task GenerateAll_IsolatedBuild_KeepsExistingSuffix()
	{
		var html = await Generate(
			BuildType.Isolated,
			"""
			---
			products:
			  - id: elasticsearch
			---

			# Query DSL
			"""
		);

		html.Should().Contain("<title>Query DSL - Elasticsearch | Query DSL</title>");
		html.Should().NotContain("| Elastic Docs</title>");
	}

	[Fact]
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

		html.Should().Contain("<title>Real page title | Elastic Docs</title>");
	}

	[Fact]
	public async Task GenerateAll_FormattedH1_PreservesVisibleFormatting()
	{
		var html = await Generate(BuildType.Assembler, markdown: "# Install `ecctl` *quickly*");

		html.Should().Contain("<title>Install ecctl quickly | Elastic Docs</title>");
		html.Should().Contain("<h1>Install <code>ecctl</code> <em>quickly</em></h1>");
	}

	[Fact]
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
		await using var collector = new DiagnosticsCollector([]).StartAsync(TestContext.Current.CancellationToken);
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var context = new BuildContext(collector, TestHelpers.CreateDocumentationFileSystem(fileSystem), configurationContext);
		var set = new DocumentationSet(context, new TestLoggerFactory(output), new TestCrossLinkResolver());
		var generator = new DocumentationGenerator(set, new TestLoggerFactory(output));

		var result = generator.MarkdownStringRenderer.RenderPreservingFirstHeadingWithMetadata(
			markdown,
			fileSystem.FileInfo.New("docs/index.md")
		);

		result.Title.Should().Be("Search APIs");
		result.MetaTitle.Should().Be("Search Elasticsearch API");
	}

	private async Task<string> Generate(BuildType buildType, string markdown, bool branded = false, string? docsetProduct = null)
	{
		var branding = branded ? """
			branding:
			  icon: assets/logo.svg
			""" : string.Empty;
		var products = docsetProduct is null
			? string.Empty
			: $"""
				products:
				  - id: {docsetProduct}
				""";
		var fileSystem = new MockFileSystem(
			new Dictionary<string, MockFileData>
			{
				["docs/docset.yml"] = new(
					$"""
					project: test
					{products}
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
		await using var collector = new DiagnosticsCollector([]).StartAsync(TestContext.Current.CancellationToken);
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var context = new BuildContext(collector, TestHelpers.CreateDocumentationFileSystem(fileSystem), configurationContext)
		{
			BuildType = buildType
		};
		var set = new DocumentationSet(context, new TestLoggerFactory(output), new TestCrossLinkResolver());
		var generator = new DocumentationGenerator(set, new TestLoggerFactory(output));

		await generator.GenerateAll(TestContext.Current.CancellationToken);
		await collector.StopAsync(TestContext.Current.CancellationToken);

		return fileSystem.File.ReadAllText(Path.Join(set.OutputDirectory.FullName, "index.html"));
	}
}
