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

public class BrandingCopyTests(ITestOutputHelper output)
{
	[Fact]
	public async Task CopyBrandingResources_SeparateFileSystems_DoesNotThrow()
	{
		var logger = new TestLoggerFactory(output);

		var readFs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "docs/docset.yml",
				//language=yaml
				new MockFileData("""
project: test
toc:
- file: index.md
branding:
  icon: assets/logo.svg
""") },
			{ "docs/index.md", new MockFileData("# Hello") },
			{ "docs/assets/logo.svg", new MockFileData("<svg/>") }
		}, new MockFileSystemOptions
		{
			CurrentDirectory = Paths.WorkingDirectoryRoot.FullName
		});

		var writeFs = new MockFileSystem(new MockFileSystemOptions
		{
			CurrentDirectory = Paths.WorkingDirectoryRoot.FullName
		});

		await using var collector = new DiagnosticsCollector([]).StartAsync(TestContext.Current.CancellationToken);
		var configurationContext = TestHelpers.CreateConfigurationContext(readFs);
		var readScoped = FileSystemFactory.ScopeCurrentWorkingDirectory(readFs);
		var writeScoped = FileSystemFactory.ScopeCurrentWorkingDirectoryForWrite(writeFs);
		var context = new BuildContext(collector, readScoped, writeScoped, configurationContext, ExportOptions.Default);

		var linkResolver = new TestCrossLinkResolver();
		var set = new DocumentationSet(context, logger, linkResolver);
		var generator = new DocumentationGenerator(set, logger);

		await generator.GenerateAll(TestContext.Current.CancellationToken);
		await collector.StopAsync(TestContext.Current.CancellationToken);

		var outputStaticDir = Path.Join(set.OutputDirectory.FullName, "_static");
		writeFs.File.Exists(Path.Join(outputStaticDir, "logo.svg")).Should().BeTrue();
	}
}
