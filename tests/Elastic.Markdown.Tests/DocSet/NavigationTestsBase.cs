// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Markdown.IO;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Tests.DocSet;

public class NavigationTestsBase : IAsyncLifetime
{
	protected NavigationTestsBase(ITestOutputHelper output)
	{
		LoggerFactory = new TestLoggerFactory(output);
		ReadFileSystem = new FileSystem(); //use real IO to read docs.
		WriteFileSystem = new MockFileSystem(new MockFileSystemOptions //use in memory mock fs to test generation
		{
			CurrentDirectory = Paths.WorkingDirectoryRoot.FullName
		});
		var collector = new TestDiagnosticsCollector(output);
		var context = new BuildContext(collector, ReadFileSystem, WriteFileSystem)
		{
			Force = false,
			UrlPathPrefix = null
		};

		var linkResolver = new TestCrossLinkResolver();
		Set = new DocumentationSet(context, LoggerFactory, linkResolver);

		Set.Files.Should().HaveCountGreaterThan(10);
		Generator = new DocumentationGenerator(Set, LoggerFactory);
	}

	protected ILoggerFactory LoggerFactory { get; }

	protected FileSystem ReadFileSystem { get; set; }
	protected IFileSystem WriteFileSystem { get; set; }
	protected DocumentationSet Set { get; }
	protected DocumentationGenerator Generator { get; }
	protected ConfigurationFile Configuration { get; set; } = default!;

	public async ValueTask InitializeAsync()
	{
		await Generator.ResolveDirectoryTree(default);
		Configuration = Generator.DocumentationSet.Configuration;
	}

	public ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		return ValueTask.CompletedTask;
	}
}
