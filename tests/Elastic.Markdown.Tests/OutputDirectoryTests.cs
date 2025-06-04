// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Site;
using Elastic.Markdown.IO;
using FluentAssertions;

namespace Elastic.Markdown.Tests;

public class OutputDirectoryTests(ITestOutputHelper output)
{
	[Fact]
	public async Task CreatesDefaultOutputDirectory()
	{
		var logger = new TestLoggerFactory(output);
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "docs/docset.yml",
				//language=yaml
				new MockFileData("""
project: test
toc:
- file: index.md
""") },
			{ "docs/index.md", new MockFileData("test") }
		}, new MockFileSystemOptions
		{
			CurrentDirectory = Paths.WorkingDirectoryRoot.FullName
		});
		await using var collector = new DiagnosticsCollector([]).StartAsync(TestContext.Current.CancellationToken);
		var context = new BuildContext(collector, fileSystem);
		var linkResolver = new TestCrossLinkResolver();
		var set = new DocumentationSet(context, logger, linkResolver);
		var generator = new DocumentationGenerator(set, logger);

		await generator.GenerateAll(TestContext.Current.CancellationToken);
		await collector.StopAsync(TestContext.Current.CancellationToken);

		fileSystem.Directory.Exists(".artifacts").Should().BeTrue();

	}
}
