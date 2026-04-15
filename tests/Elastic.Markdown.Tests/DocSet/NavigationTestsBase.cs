// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Markdown.IO;
using Microsoft.Extensions.Logging;
using Nullean.ScopedFileSystem;

namespace Elastic.Markdown.Tests.DocSet;

public class NavigationTestsBase : IAsyncLifetime
{
	protected NavigationTestsBase(ITestOutputHelper output)
	{
		LoggerFactory = new TestLoggerFactory(output);
		var mockWriteFs = new MockFileSystem(new MockFileSystemOptions //use in memory mock fs to test generation
		{
			CurrentDirectory = Paths.WorkingDirectoryRoot.FullName
		});
		ReadFileSystem = FileSystemFactory.RealRead;
		WriteFileSystem = FileSystemFactory.ScopeCurrentWorkingDirectory(mockWriteFs);
		var collector = new TestDiagnosticsCollector(output);
		var configurationContext = TestHelpers.CreateConfigurationContext(ReadFileSystem);
		var context = new BuildContext(collector, ReadFileSystem, WriteFileSystem, configurationContext, ExportOptions.Default)
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

	protected ScopedFileSystem ReadFileSystem { get; set; }
	protected ScopedFileSystem WriteFileSystem { get; set; }
	protected DocumentationSet Set { get; }
	protected DocumentationGenerator Generator { get; }
	protected ConfigurationFile? Configuration { get; set; }

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
