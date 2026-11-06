// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.IO;
using Elastic.Markdown.Tests.Inline;

namespace Elastic.Markdown.Tests;

public class RedirectApiLinkTests
{
	[Test]
	public void RedirectToMissingApiPage_WarnsOnce()
	{
		var collector = BuildDocSet(
			"""
			redirects:
			  'old-api.md': 'https://www.elastic.co/docs/api/doc/kibana/v8/operation/operation-gone'
			  'old-ok.md': 'https://www.elastic.co/docs/api/doc/kibana/operation/operation-exists'
			  'old-other.md': 'https://www.elastic.co/docs/solutions/search'
			"""
		);

		collector.HasOneApiWarning("operation-gone");
		collector.Diagnostics.Should().ContainSingle(d => d.Severity == Severity.Warning && d.Message.Contains("API link"));
		collector.Errors.Should().Be(0);
	}

	private static TestDiagnosticsCollector BuildDocSet(string redirects)
	{
		var fileSystem = new MockFileSystem(
			new Dictionary<string, MockFileData>
			{
				{ "docs/docset.yml", new MockFileData("project: test\ntoc:\n- file: index.md\n") },
				{ "docs/index.md", new MockFileData("# Index") },
				{ "docs/redirects.yml", new MockFileData(redirects) }
			},
			new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName }
		);
		var collector = new TestDiagnosticsCollector();
		_ = collector.StartAsync(TestContext.Current!.Execution.CancellationToken);
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var context = new BuildContext(collector, TestHelpers.CreateDocumentationFileSystem(fileSystem), configurationContext);
		_ = new DocumentationSet(context, new TestLoggerFactory(), new TestCrossLinkResolver());
		return collector;
	}
}
