// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;
using FluentAssertions;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.SiteMap;

public class NavigationTestsBase : IAsyncLifetime
{
	protected NavigationTestsBase(ITestOutputHelper output)
	{
		var logger = new TestLoggerFactory(output);
		var readFs = new FileSystem(); //use real IO to read docs.
		var writeFs = new MockFileSystem(new MockFileSystemOptions //use in memory mock fs to test generation
		{
			CurrentDirectory = Paths.Root.FullName
		});
		var context = new BuildContext
		{
			Force = false,
			UrlPathPrefix = null,
			ReadFileSystem = readFs,
			WriteFileSystem = writeFs,
			Collector = new DiagnosticsCollector(logger, [])
		};

		var set = new DocumentationSet(context);

		set.Files.Should().HaveCountGreaterThan(10);
		Generator = new DocumentationGenerator(set, context, logger);

	}

	public DocumentationGenerator Generator { get; }

	public ConfigurationFile Configuration { get; set; } = default!;

	public async Task InitializeAsync()
	{
		await Generator.GenerateAll(default);
		Configuration = Generator.DocumentationSet.Configuration;
	}

	public Task DisposeAsync() => Task.CompletedTask;
}
