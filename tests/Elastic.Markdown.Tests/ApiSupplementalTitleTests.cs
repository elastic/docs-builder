// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Testing;
using Elastic.Markdown.IO;
using Nullean.Snapshots;

namespace Elastic.Markdown.Tests;

public class ApiSupplementalTitleTests
{
	[Test]
	public async Task Parse_SupplementalFileWithoutTitle_IsQuietWhileOrdinaryPageStillWarns()
	{
		var fileSystem = new MockFileSystem(
			new Dictionary<string, MockFileData>
			{
				{
					"docs/docset.yml",
					new MockFileData(
						"""
				project: test
				api:
				  elasticsearch:
				    - spec: elasticsearch-openapi.json
				      product: elasticsearch
				toc:
				- file: index.md
				- file: untitled.md
				"""
					)
				},
				{ "docs/elasticsearch-openapi.json", new MockFileData("{}") },
				{ "docs/index.md", new MockFileData("# Index") },
				{ "docs/untitled.md", new MockFileData("Body without a heading.") },
				{ "docs/api/elasticsearch/op-search.md", new MockFileData("## Description\n\nSearch.") },
				{ "docs/api/elasticsearch/tag-documents.md", new MockFileData("## Description\n\nDocuments.") }
			},
			new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName }
		);
		var collector = new TestDiagnosticsCollector();
		_ = collector.StartAsync(TestContext.Current!.Execution.CancellationToken);
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var context = new BuildContext(collector, TestHelpers.CreateDocumentationFileSystem(fileSystem), configurationContext);
		var set = new DocumentationSet(context, new TestLoggerFactory(), new TestCrossLinkResolver());

		foreach (var file in set.MarkdownFiles)
			_ = await file.ParseFullAsync(set.TryFindDocumentByRelativePath, TestContext.Current!.Execution.CancellationToken);

		collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot("warning {repo}/docs/untitled.md Document has no title, using file name as title.");
	}
}
