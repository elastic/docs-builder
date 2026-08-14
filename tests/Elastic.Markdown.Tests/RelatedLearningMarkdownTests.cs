// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst.RelatedLearning;
using Markdig.Syntax;

namespace Elastic.Markdown.Tests;

public class RelatedLearningMappedPageTests(ITestOutputHelper output)
	: RelatedLearningPageTest(
		output,
		"docs/manage-data/data-store/index-basics.md",
		"""
		# Index basics

		Index documents into Elasticsearch.
		""",
		repositoryName: "docs-content")
{
	[Fact]
	public void InjectsHeadingAndLinks()
	{
		Document.Descendants<HeadingBlock>()
			.Should()
			.Contain(h => h.GetData("anchor") as string == RelatedLearningBlock.Anchor);
		Document.Descendants<RelatedLearningBlock>().Should().ContainSingle();

		Html.Should().Contain("id=\"related-learning-heading\"");
		Html.Should().Contain("class=\"related-learning\"");
		Html.Should().Contain("href=\"https://www.elastic.co/training/index-basics\"");
		Html.Should().Contain("target=\"_blank\"");
		Html.Should().Contain(">Index Basics</a>");
	}

	[Fact]
	public void AddsHeadingToOnThisPage()
	{
		File.PageTableOfContent.Should().ContainKey("related-learning-heading");
		File.PageTableOfContent["related-learning-heading"].Heading.Should().Be("Related learning");
		File.PageTableOfContent["related-learning-heading"].Level.Should().Be(2);
	}
}

public class RelatedLearningUnmappedPageTests(ITestOutputHelper output)
	: RelatedLearningPageTest(
		output,
		"docs/getting-started/index.md",
		"""
		# Getting started

		No matching catalog entry.
		""",
		repositoryName: "docs-content")
{
	[Fact]
	public void DoesNotInjectSection()
	{
		Document.Descendants<RelatedLearningBlock>().Should().BeEmpty();
		File.PageTableOfContent.Should().NotContainKey("related-learning-heading");
		Html.Should().NotContain("class=\"related-learning\"");
		Html.Should().NotContain("id=\"related-learning-heading\"");
	}
}

public class RelatedLearningWrongRepositoryTests(ITestOutputHelper output)
	: RelatedLearningPageTest(
		output,
		"docs/manage-data/data-store/index-basics.md",
		"""
		# Index basics

		Same path, different repository.
		""",
		repositoryName: "docs-builder")
{
	[Fact]
	public void DoesNotInjectSection()
	{
		Document.Descendants<RelatedLearningBlock>().Should().BeEmpty();
		File.PageTableOfContent.Should().NotContainKey("related-learning-heading");
		Html.Should().NotContain("class=\"related-learning\"");
	}
}

public abstract class RelatedLearningPageTest : IAsyncLifetime
{
	protected MarkdownFile File { get; }
	protected string Html { get; private set; }
	protected MarkdownDocument Document { get; private set; }
	private TestDiagnosticsCollector Collector { get; }
	private DocumentationSet Set { get; }

	protected RelatedLearningPageTest(ITestOutputHelper output, string relativePath, string content, string repositoryName)
	{
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ relativePath, new MockFileData(content) }
		}, new MockFileSystemOptions
		{
			CurrentDirectory = Paths.WorkingDirectoryRoot.FullName
		});

		var root = fileSystem.DirectoryInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs/"));
		fileSystem.GenerateDocSetYaml(root);
		Collector = new TestDiagnosticsCollector(output);
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var git = new GitCheckoutInformation
		{
			Branch = "main",
			Remote = $"elastic/{repositoryName}",
			Ref = "test",
			RepositoryName = repositoryName
		};
		var context = new BuildContext(Collector, TestHelpers.CreateDocumentationFileSystem(fileSystem, root, git), configurationContext);
		Set = new DocumentationSet(context, new TestLoggerFactory(output), new TestCrossLinkResolver());
		File = Set.TryFindDocument(fileSystem.FileInfo.New(relativePath)) as MarkdownFile
			?? throw new NullReferenceException();
		Html = default!;
		Document = default!;
	}

	public async ValueTask InitializeAsync()
	{
		_ = Collector.StartAsync(TestContext.Current.CancellationToken);
		await Set.ResolveDirectoryTree(TestContext.Current.CancellationToken);
		Document = await File.ParseFullAsync(Set.TryFindDocumentByRelativePath, TestContext.Current.CancellationToken);
		Html = MarkdownFile.CreateHtml(Document);
		await Collector.StopAsync(TestContext.Current.CancellationToken);
	}

	public ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		return ValueTask.CompletedTask;
	}
}
