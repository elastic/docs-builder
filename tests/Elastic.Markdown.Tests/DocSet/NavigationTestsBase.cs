// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Configuration;
using FluentAssertions;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.DocSet;

public class NavigationTestsBase : IAsyncLifetime
{
	protected NavigationTestsBase(ITestOutputHelper output)
	{
		var logger = new TestLoggerFactory(output);
		ReadFileSystem = new FileSystem(); //use real IO to read docs.
		var writeFs = new MockFileSystem(new MockFileSystemOptions //use in memory mock fs to test generation
		{
			CurrentDirectory = Paths.Root.FullName
		});
		var collector = new TestDiagnosticsCollector(output);
		var context = new BuildContext(ReadFileSystem, writeFs)
		{
			Force = false,
			UrlPathPrefix = null,
			Collector = collector
		};

		Set = new DocumentationSet(context);

		Set.Files.Should().HaveCountGreaterThan(10);
		Generator = new DocumentationGenerator(Set, logger);

	}

	protected FileSystem ReadFileSystem { get; set; }
	protected DocumentationSet Set { get; }
	protected DocumentationGenerator Generator { get; }
	protected ConfigurationFile Configuration { get; set; } = default!;

	public async Task InitializeAsync()
	{
		await Generator.ResolveDirectoryTree(default);
		Configuration = Generator.DocumentationSet.Configuration;
	}

	public Task DisposeAsync() => Task.CompletedTask;
}
