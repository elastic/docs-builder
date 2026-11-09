// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Testing;
using Elastic.Markdown.IO;
using Elastic.Markdown.Tests.Inline;
using Nullean.Snapshots;

namespace Elastic.Markdown.Tests;

public class RedirectApiLinkTests
{
	[Test]
	public void RedirectToMissingApiPage_HintsAtTheRedirectsFile()
	{
		var collector = BuildDocSet(
			"""
			redirects:
			  'old-api.md': 'https://www.elastic.co/docs/api/doc/kibana/v8/operation/operation-gone'
			  'old-ok.md': 'https://www.elastic.co/docs/api/doc/kibana/operation/operation-exists'
			  'old-other.md': 'https://www.elastic.co/docs/solutions/search'
			"""
		);

		collector.HasOneApiHint("operation-gone");
		collector.Diagnostics.Should().ContainSingle(d => d.Severity == Severity.Hint && d.File.EndsWith("redirects.yml"));
		collector.Errors.Should().Be(0);
	}

	[Test]
	public void RedirectToUndeclaredCrossLinkRepository_ErrorsOnTheRedirectsFile()
	{
		var collector = BuildDocSet("""
			redirects:
			  'old.md': 'docs-release-notes://index.md'
			""");

		collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error {repo}/docs/redirects.yml Redirect old.md points to docs-release-notes://index.md but 'docs-release-notes' is not listed under 'cross_links' in docset.yml"
			);
	}

	[Test]
	public void RedirectWithDefaultTargetAndUndeclaredManyTarget_ErrorsOnTheRedirectsFile()
	{
		var collector = BuildDocSet(
			"""
			redirects:
			  'old.md':
			    to: 'index.md'
			    many:
			      - to: 'docs-release-notes://index.md'
			        anchors:
			          'item-a': 'yy'
			"""
		);

		collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error {repo}/docs/redirects.yml Redirect old.md points to docs-release-notes://index.md but 'docs-release-notes' is not listed under 'cross_links' in docset.yml"
			);
	}

	[Test]
	public void RedirectToDeclaredCrossLinkRepository_HasNoErrors()
	{
		var collector = BuildDocSet(
			"""
			redirects:
			  'old.md': 'docs-release-notes://index.md'
			""",
			crossLinks: "docs-release-notes"
		);

		collector.Errors.Should().Be(0);
	}

	[Test]
	public void RedirectWithDefaultTargetAndManyTarget_DoesNotRegisterManyAnchorRemaps()
	{
		var (collector, docSet) = Build(
			"""
			redirects:
			  'old.md':
			    to: 'index.md'
			    many:
			      - to: 'index.md'
			        anchors:
			          'item-a': 'does-not-exist'
			""",
			crossLinks: null
		);

		collector.Errors.Should().Be(0);
		docSet.MarkdownFiles.Single().AnchorRemapping.Should().BeNull();
	}

	private static TestDiagnosticsCollector BuildDocSet(string redirects, string? crossLinks = null) =>
		Build(redirects, crossLinks).Collector;

	private static (TestDiagnosticsCollector Collector, DocumentationSet DocSet) Build(string redirects, string? crossLinks)
	{
		var docSet = crossLinks is null
			? "project: test\ntoc:\n- file: index.md\n"
			: $"project: test\ncross_links:\n- {crossLinks}\ntoc:\n- file: index.md\n";
		var fileSystem = new MockFileSystem(
			new Dictionary<string, MockFileData>
			{
				{ "docs/docset.yml", new MockFileData(docSet) },
				{ "docs/index.md", new MockFileData("# Index") },
				{ "docs/redirects.yml", new MockFileData(redirects) }
			},
			new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName }
		);
		var collector = new TestDiagnosticsCollector();
		_ = collector.StartAsync(TestContext.Current!.Execution.CancellationToken);
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var context = new BuildContext(collector, TestHelpers.CreateDocumentationFileSystem(fileSystem), configurationContext);
		var documentationSet = new DocumentationSet(context, new TestLoggerFactory(), new TestCrossLinkResolver());
		return (collector, documentationSet);
	}
}
