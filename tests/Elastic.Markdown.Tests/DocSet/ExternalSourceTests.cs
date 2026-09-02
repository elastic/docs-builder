// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.IO;

namespace Elastic.Markdown.Tests.DocSet;

/// <summary>
/// End-to-end coverage for <c>source:</c>: a page that lives outside the documentation set root keeps the
/// docset-relative position declared by <c>file:</c> for its URL, output path and link reference.
/// </summary>
public class ExternalSourceTests(ITestOutputHelper output)
{
	private const string DocsetYaml =
		//language=yaml
		"""
		project: test
		toc:
		- file: index.md
		- file: feedback.md
		  source: ../packages/kbn-ui/feedback/feedback.md
		""";

	private static MockFileSystem CreateFileSystem(string docsetYaml, params (string path, string content)[] files)
	{
		var contents = new Dictionary<string, MockFileData> { { "docs/docset.yml", new MockFileData(docsetYaml) } };
		foreach (var (path, content) in files)
			contents[path] = new MockFileData(content);
		return new MockFileSystem(contents, new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName });
	}

	private (DocumentationSet Set, TestDiagnosticsCollector Collector, MockFileSystem FileSystem) Build(
		string docsetYaml,
		params (string path, string content)[] files
	)
	{
		var logger = new TestLoggerFactory(output);
		var fileSystem = CreateFileSystem(docsetYaml, files);
		var collector = new TestDiagnosticsCollector(output);
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var context = new BuildContext(collector, TestHelpers.CreateDocumentationFileSystem(fileSystem), configurationContext);
		return (new DocumentationSet(context, logger, new TestCrossLinkResolver()), collector, fileSystem);
	}

	[Fact]
	public void ExternalSource_IsRegisteredUnderItsVirtualPath()
	{
		var (set, collector, _) = Build(
			DocsetYaml,
			("docs/index.md", "# Home"),
			("packages/kbn-ui/feedback/feedback.md", "# Feedback\n\nSourced from the package tree.")
		);

		collector.Diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();

		var feedback = set.MarkdownFiles.Should().ContainSingle(f => f.RelativePath.EndsWith("feedback.md")).Subject;
		feedback.RelativePath.Should().Be("feedback.md", "the virtual path drives output and links, not the on-disk location");
		feedback.SourceFile.FullName.Should().EndWith(Path.Join("packages", "kbn-ui", "feedback", "feedback.md"));
		feedback.CrossLink.Should().EndWith("://feedback.md");
		feedback.FileName.Should().Be("feedback.md");
	}

	[Fact]
	public async Task ExternalSource_RendersToTheVirtualOutputPath()
	{
		var (set, collector, _) = Build(
			DocsetYaml,
			("docs/index.md", "# Home"),
			("packages/kbn-ui/feedback/feedback.md", "# Feedback\n\nSourced from the package tree.")
		);
		var generator = new DocumentationGenerator(set, new TestLoggerFactory(output));

		await generator.GenerateAll(TestContext.Current.CancellationToken);

		collector.Diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
		var outputFile = Path.Join(set.OutputDirectory.FullName, "feedback", "index.html");
		set.OutputDirectory.FileSystem.File.Exists(outputFile).Should().BeTrue();
		var html = await set.OutputDirectory.FileSystem.File.ReadAllTextAsync(outputFile, TestContext.Current.CancellationToken);
		html.Should().Contain("Sourced from the package tree.");
		html.Should().Contain(
			"/edit/test-e35fcb27-5f60-4e/packages/kbn-ui/feedback/feedback.md",
			"the edit link points at the file actually read"
		);

		var indexPath = Path.Join(set.OutputDirectory.FullName, "index.html");
		var indexHtml = await set.OutputDirectory.FileSystem.File.ReadAllTextAsync(indexPath, TestContext.Current.CancellationToken);
		indexHtml.Should().Contain("/edit/test-e35fcb27-5f60-4e/docs/index.md", "ordinary pages still edit through the docset directory");
	}

	[Fact]
	public void ExternalSource_NavigationUsesTheVirtualUrl()
	{
		var (set, _, _) = Build(DocsetYaml, ("docs/index.md", "# Home"), ("packages/kbn-ui/feedback/feedback.md", "# Feedback"));

		set.Navigation.NavigationItems.Should().ContainSingle().Which.Url.Should().Be("/feedback");
	}

	[Fact]
	public async Task ExternalSource_ResolvesRelativeLinksFromItsVirtualPosition()
	{
		var docsetYaml =
			//language=yaml
			"""
			project: test
			toc:
			- file: index.md
			- folder: guides
			  children:
			  - file: index.md
			  - file: feedback.md
			    source: ../packages/kbn-ui/feedback.md
			""";

		var (set, collector, _) = Build(
			docsetYaml,
			("docs/index.md", "# Home"),
			("docs/guides/index.md", "# Guides"),
			("packages/kbn-ui/feedback.md", "# Feedback\n\nBack to the [guides](./index.md).")
		);
		var generator = new DocumentationGenerator(set, new TestLoggerFactory(output));

		await generator.GenerateAll(TestContext.Current.CancellationToken);

		collector.Diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
		var outputFile = Path.Join(set.OutputDirectory.FullName, "guides", "feedback", "index.html");
		var html = await set.OutputDirectory.FileSystem.File.ReadAllTextAsync(outputFile, TestContext.Current.CancellationToken);
		html.Should().Contain(
			"""Back to the <a href="/guides" preload="mousedown">guides</a>""",
			"'./index.md' is the docset sibling, not a neighbour of the file on disk"
		);
	}

	[Fact]
	public void MissingExternalSource_EmitsErrorNamingTheSource()
	{
		var (_, collector, _) = Build(DocsetYaml, ("docs/index.md", "# Home"));

		collector
			.Diagnostics
			.Should()
			.Contain(
				d => d.Severity == Severity.Error && d.Message.Contains("'source: ../packages/kbn-ui/feedback/feedback.md' does not exist")
			);
	}

	[Fact]
	public void ExternalSourceOnAPositionTakenByARealFile_EmitsError()
	{
		var (_, collector, _) = Build(
			DocsetYaml,
			("docs/index.md", "# Home"),
			("docs/feedback.md", "# Feedback on disk"),
			("packages/kbn-ui/feedback/feedback.md", "# Feedback")
		);

		collector
			.Diagnostics
			.Should()
			.Contain(d => d.Severity == Severity.Error && d.Message.Contains("'file: feedback.md' already exists on disk"));
	}

	[Fact]
	public void TwoEntriesSourcingTheSamePosition_EmitsError()
	{
		var docsetYaml =
			//language=yaml
			"""
			project: test
			toc:
			- file: index.md
			- file: feedback.md
			  source: ../packages/kbn-ui/feedback.md
			- hidden: feedback.md
			  source: ../packages/kbn-es/feedback.md
			""";

		var (_, collector, _) = Build(
			docsetYaml,
			("docs/index.md", "# Home"),
			("packages/kbn-ui/feedback.md", "# UI feedback"),
			("packages/kbn-es/feedback.md", "# ES feedback")
		);

		collector
			.Diagnostics
			.Should()
			.Contain(d => d.Severity == Severity.Error && d.Message.Contains("is already sourced by another entry"));
	}

	[Fact]
	public void ExternalSourceAboveTheCheckout_EmitsError()
	{
		var docsetYaml =
			//language=yaml
			"""
			project: test
			toc:
			- file: index.md
			- file: feedback.md
			  source: ../../outside-the-repo/feedback.md
			""";

		var (_, collector, _) = Build(docsetYaml, ("docs/index.md", "# Home"));

		collector
			.Diagnostics
			.Should()
			.Contain(d => d.Severity == Severity.Error && d.Message.Contains("resolves outside the repository checkout"));
	}
}
