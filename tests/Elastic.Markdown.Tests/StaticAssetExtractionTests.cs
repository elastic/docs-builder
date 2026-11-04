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

public class StaticAssetExtractionTests
{
	[Test]
	public async Task GenerateAll_ByDefault_CopiesEmbeddedStaticFilesToOutput()
	{
		var (fs, outputStaticDir) = await GenerateAsync(skipStaticAssetExtraction: false);

		var copied = fs.Directory.Exists(outputStaticDir) ? fs.Directory.GetFiles(outputStaticDir, "*", SearchOption.AllDirectories) : [];
		copied.Should().NotBeEmpty();
	}

	[Test]
	public async Task GenerateAll_SkipStaticAssetExtraction_LeavesOutputStaticEmpty()
	{
		var (fs, outputStaticDir) = await GenerateAsync(skipStaticAssetExtraction: true);

		var copied = fs.Directory.Exists(outputStaticDir) ? fs.Directory.GetFiles(outputStaticDir, "*", SearchOption.AllDirectories) : [];
		copied.Should().BeEmpty();
	}

	private static async Task<(MockFileSystem Fs, string OutputStaticDir)> GenerateAsync(bool skipStaticAssetExtraction)
	{
		var ctx = TestContext.Current!.Execution.CancellationToken;
		var fs = new MockFileSystem(
			new Dictionary<string, MockFileData>
			{
				{
					"docs/docset.yml",
					//language=yaml
					new MockFileData("""
project: test
toc:
- file: index.md
""")
				},
				{ "docs/index.md", new MockFileData("# Hello") }
			},
			new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName }
		);

		await using var collector = new DiagnosticsCollector([]).StartAsync(ctx);
		var configurationContext = TestHelpers.CreateConfigurationContext(fs);
		// The default case leaves the property unset on purpose, so a changed default is caught.
		var context = skipStaticAssetExtraction
			? new BuildContext(collector, TestHelpers.CreateDocumentationFileSystem(fs), configurationContext)
			{
				SkipStaticAssetExtraction = true
			}
			: new BuildContext(collector, TestHelpers.CreateDocumentationFileSystem(fs), configurationContext);

		var logger = new TestLoggerFactory();
		var set = new DocumentationSet(context, logger, new TestCrossLinkResolver());
		var generator = new DocumentationGenerator(set, logger);

		await generator.GenerateAll(ctx);
		await collector.StopAsync(ctx);

		return (fs, Path.Join(set.OutputDirectory.FullName, "_static"));
	}
}
