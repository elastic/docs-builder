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

public class BrandingCopyTests()
{
	[Test]
	public async Task CopyBrandingResources_SeparateFileSystems_DoesNotThrow()
	{
		var logger = new TestLoggerFactory();

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
branding:
  icon: assets/logo.svg
""")
				},
				{ "docs/index.md", new MockFileData("# Hello") },
				{ "docs/assets/logo.svg", new MockFileData("<svg/>") }
			},
			new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName }
		);

		await using var collector = new DiagnosticsCollector([]).StartAsync(TestContext.Current!.Execution.CancellationToken);
		var configurationContext = TestHelpers.CreateConfigurationContext(fs);
		var context = new BuildContext(collector, TestHelpers.CreateDocumentationFileSystem(fs), configurationContext);

		var linkResolver = new TestCrossLinkResolver();
		var set = new DocumentationSet(context, logger, linkResolver);
		var generator = new DocumentationGenerator(set, logger);

		await generator.GenerateAll(TestContext.Current!.Execution.CancellationToken);
		await collector.StopAsync(TestContext.Current!.Execution.CancellationToken);

		var outputStaticDir = Path.Join(set.OutputDirectory.FullName, "_static");
		fs.File.Exists(Path.Join(outputStaticDir, "logo.svg")).Should().BeTrue();
	}
}
