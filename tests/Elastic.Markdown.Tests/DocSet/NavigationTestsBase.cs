// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.FileSystems;
using Elastic.Markdown.IO;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Tests.DocSet;

public class NavigationTestsBase : IAsyncLifetime
{
	protected NavigationTestsBase(ITestOutputHelper output)
	{
		LoggerFactory = new TestLoggerFactory(output);
		var mockWriteFs = new MockFileSystem(new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName });
		var docsTestsPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs-tests");
		var invocation = new System.IO.Abstractions.FileSystem().DirectoryInfo.New(docsTestsPath);
		FileSystem = DocumentationFileSystem.Resolve(invocation, new DocumentationScopeOptions { InnerWrite = mockWriteFs });
		var collector = new TestDiagnosticsCollector(output);
		var configurationContext = TestHelpers.CreateConfigurationContext(FileSystem.Read);
		var context = new BuildContext(collector, FileSystem, configurationContext) { Force = false, UrlPathPrefix = null };

		var linkResolver = new TestCrossLinkResolver();
		Set = new DocumentationSet(context, LoggerFactory, linkResolver);

		Set.Files.Should().HaveCountGreaterThan(10);
		Generator = new DocumentationGenerator(Set, LoggerFactory);
	}

	protected ILoggerFactory LoggerFactory { get; }

	protected DocumentationFileSystem FileSystem { get; }
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
