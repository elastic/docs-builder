// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.IO;
using FluentAssertions;

namespace Elastic.Markdown.Tests;

public class RootIndexValidationTests(ITestOutputHelper output)
{
	[Fact]
	public void InternalRegistry_MissingIndexMd_EmitsError()
	{
		var logger = new TestLoggerFactory(output);
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "docs/docset.yml", new MockFileData("""
				project: test
				registry: internal
				toc:
				- file: getting-started.md
				""") },
			{ "docs/getting-started.md", new MockFileData("# Getting started") }
		}, new MockFileSystemOptions
		{
			CurrentDirectory = Paths.WorkingDirectoryRoot.FullName
		});
		var collector = new TestDiagnosticsCollector(output);
		_ = collector.StartAsync(TestContext.Current.CancellationToken);
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var context = new BuildContext(collector, fileSystem, configurationContext);
		_ = new DocumentationSet(context, logger, new TestCrossLinkResolver());

		collector.Errors.Should().BeGreaterThan(0);
		collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("index.md"));
	}

	[Fact]
	public void InternalRegistry_WithIndexMd_NoError()
	{
		var logger = new TestLoggerFactory(output);
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "docs/docset.yml", new MockFileData("""
				project: test
				registry: internal
				toc:
				- file: index.md
				- file: getting-started.md
				""") },
			{ "docs/index.md", new MockFileData("# Home") },
			{ "docs/getting-started.md", new MockFileData("# Getting started") }
		}, new MockFileSystemOptions
		{
			CurrentDirectory = Paths.WorkingDirectoryRoot.FullName
		});
		var collector = new TestDiagnosticsCollector(output);
		_ = collector.StartAsync(TestContext.Current.CancellationToken);
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var context = new BuildContext(collector, fileSystem, configurationContext);
		_ = new DocumentationSet(context, logger, new TestCrossLinkResolver());

		collector.Diagnostics
			.Where(d => d.Severity == Severity.Error && d.Message.Contains("index.md"))
			.Should()
			.BeEmpty();
	}

	[Fact]
	public void PublicRegistry_MissingIndexMd_NoError()
	{
		var logger = new TestLoggerFactory(output);
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "docs/docset.yml", new MockFileData("""
				project: test
				toc:
				- file: getting-started.md
				""") },
			{ "docs/getting-started.md", new MockFileData("# Getting started") }
		}, new MockFileSystemOptions
		{
			CurrentDirectory = Paths.WorkingDirectoryRoot.FullName
		});
		var collector = new TestDiagnosticsCollector(output);
		_ = collector.StartAsync(TestContext.Current.CancellationToken);
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var context = new BuildContext(collector, fileSystem, configurationContext);
		_ = new DocumentationSet(context, logger, new TestCrossLinkResolver());

		collector.Diagnostics
			.Where(d => d.Severity == Severity.Error && d.Message.Contains("index.md"))
			.Should()
			.BeEmpty();
	}
}
