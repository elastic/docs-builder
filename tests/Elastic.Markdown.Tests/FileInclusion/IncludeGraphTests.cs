// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Markdown.IO;
using Nullean.ScopedFileSystem;

namespace Elastic.Markdown.Tests.FileInclusion;

public class IncludeGraphTests(ITestOutputHelper output)
{
	[Fact]
	public async Task ResolvesDirectAndTransitiveDependents()
	{
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			["docs/docset.yml"] = new("""
				project: test
				toc:
				  - file: index.md
				  - file: page-a.md
				  - file: page-b.md
				  - file: page-c.md
				"""),
			["docs/index.md"] = new("# Home"),
			["docs/page-a.md"] = new("""
				# Page A

				:::{include} _snippets/foo.md
				:::
				"""),
			["docs/page-b.md"] = new("""
				# Page B

				:::{include} _snippets/bar.md
				:::
				"""),
			["docs/page-c.md"] = new("# Page C with no includes"),
			["docs/_snippets/foo.md"] = new("foo content"),
			["docs/_snippets/bar.md"] = new("""
				:::{include} /_snippets/foo.md
				:::
				""")
		}, new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName });

		var graph = await BuildGraph(fileSystem);

		var fooDependents = graph.ResolvePageDependents("_snippets/foo.md");
		fooDependents.Should().BeEquivalentTo("page-a.md", "page-b.md");

		var barDependents = graph.ResolvePageDependents("_snippets/bar.md");
		barDependents.Should().BeEquivalentTo("page-b.md");

		graph.HasConsumers("_snippets/foo.md").Should().BeTrue();
		graph.HasConsumers("_snippets/bar.md").Should().BeTrue();
	}

	[Fact]
	public async Task UnreferencedSnippetReportsNoConsumers()
	{
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			["docs/docset.yml"] = new("""
				project: test
				toc:
				  - file: index.md
				"""),
			["docs/index.md"] = new("# Home"),
			["docs/_snippets/orphan.md"] = new("nobody includes me")
		}, new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName });

		var graph = await BuildGraph(fileSystem);

		graph.HasConsumers("_snippets/orphan.md").Should().BeFalse();
		graph.ResolvePageDependents("_snippets/orphan.md").Should().BeEmpty();
	}

	[Fact]
	public async Task ResolvesCsvIncludeDependents()
	{
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			["docs/docset.yml"] = new("""
				project: test
				toc:
				  - file: index.md
				  - file: data-page.md
				"""),
			["docs/index.md"] = new("# Home"),
			["docs/data-page.md"] = new("""
				# Data Page

				:::{csv-include} data/values.csv
				:::
				"""),
			["docs/data/values.csv"] = new("a,b\n1,2")
		}, new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName });

		var graph = await BuildGraph(fileSystem);

		graph.ResolvePageDependents("data/values.csv").Should().BeEquivalentTo("data-page.md");
	}

	private async Task<IncludeGraph> BuildGraph(MockFileSystem fileSystem)
	{
		var collector = new TestDiagnosticsCollector(output);
		_ = collector.StartAsync(TestContext.Current.CancellationToken);
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var context = new BuildContext(collector, FileSystemFactory.ScopeCurrentWorkingDirectory(fileSystem), configurationContext);
		var set = new DocumentationSet(context, new TestLoggerFactory(output), new TestCrossLinkResolver());
		return await IncludeGraph.BuildAsync(set, TestContext.Current.CancellationToken);
	}
}
