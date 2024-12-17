// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests;

public class OutputDirectoryTests(ITestOutputHelper output)
{
	[Fact]
	public async Task CreatesDefaultOutputDirectory()
	{
		var logger = new TestLoggerFactory(output);
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "docs/source/index.md", new MockFileData("test") }
		}, new MockFileSystemOptions
		{
			CurrentDirectory = Paths.Root.FullName
		});
		var context = new BuildContext(fileSystem)
		{
			Collector = new DiagnosticsCollector(logger, [])
		};
		var set = new DocumentationSet(context);
		var generator = new DocumentationGenerator(set, logger);

		await generator.GenerateAll(default);

		fileSystem.Directory.Exists(".artifacts").Should().BeTrue();

	}
}
