// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.IO;
using Nullean.ScopedFileSystem;

namespace Elastic.Markdown.Tests;

public class MissingTocFileTests(ITestOutputHelper output)
{
	[Fact]
	public void TocReferencesMissingFile_DoesNotThrow_AndEmitsClearError()
	{
		var logger = new TestLoggerFactory(output);
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "docs/docset.yml", new MockFileData("""
				project: test
				toc:
				- file: missing.md
				""") },
			// A markdown file that exists on disk but is not referenced by the TOC.
			// It keeps the documentation set non-empty so construction reaches the
			// navigation traversal (VisitNavigation + BuildNavigationLookups) that
			// previously dereferenced the null Index sentinel and crashed.
			{ "docs/present.md", new MockFileData("# Present") }
		}, new MockFileSystemOptions
		{
			CurrentDirectory = Paths.WorkingDirectoryRoot.FullName
		});
		var collector = new TestDiagnosticsCollector(output);
		_ = collector.StartAsync(TestContext.Current.CancellationToken);
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var context = new BuildContext(collector, FileSystemFactory.ScopeCurrentWorkingDirectory(fileSystem), configurationContext);

		var act = () => _ = new DocumentationSet(context, logger, new TestCrossLinkResolver());

		act.Should().NotThrow("a missing toc file must surface a validation error, not crash the build");

		collector.Errors.Should().BeGreaterThan(0);
		collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("missing.md") &&
			d.Message.Contains("does not exist"));
	}
}
