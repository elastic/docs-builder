// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Testing;
using Elastic.Markdown.IO;
using Nullean.Snapshots;

namespace Elastic.Markdown.Tests;

public class MissingTocFileTests()
{
	[Test]
	public void TocReferencesMissingFile_DoesNotThrow_AndEmitsClearError()
	{
		var logger = new TestLoggerFactory();
		var fileSystem = new MockFileSystem(
			new Dictionary<string, MockFileData>
			{
				{
					"docs/docset.yml",
					new MockFileData("""
				project: test
				toc:
				- file: missing.md
				""")
				},
				// A markdown file that exists on disk but is not referenced by the TOC.
				// It keeps the documentation set non-empty so construction reaches the
				// navigation traversal (VisitNavigation + BuildNavigationLookups) that
				// previously dereferenced the null Index sentinel and crashed.
				{
					"docs/present.md",
					new MockFileData("# Present")
				}
			},
			new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName }
		);
		var collector = new TestDiagnosticsCollector();
		_ = collector.StartAsync(TestContext.Current!.Execution.CancellationToken);
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var context = new BuildContext(collector, TestHelpers.CreateDocumentationFileSystem(fileSystem), configurationContext);

		var act = () => _ = new DocumentationSet(context, logger, new TestCrossLinkResolver());

		act.Should().NotThrow("a missing toc file must surface a validation error, not crash the build");

		collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"""
			error {repo}/docs/docset.yml Table of contents references 'missing.md' but the file does not exist on disk.
			error {repo}/docs/docset.yml Documentation set 'test' ({repo}/docs/docset.yml) table of contents has items defined but none could be created
			"""
			);
	}
}
