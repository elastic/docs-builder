// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Markdown.IO;

namespace Elastic.Markdown.Tests;

public class RedirectCrossLinkValidationTests(ITestOutputHelper output)
{
	[Fact]
	public void ValidateRedirectsExists_KibanaDocsPrefix_EmitsError()
	{
		var collector = CreateSet("""
			redirects:
			  'old-page.md': 'kibana://docs/reference/advanced-settings.md'
			""");

		collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("kibana://docs/reference/advanced-settings.md") &&
			d.Message.Contains("docs/reference/advanced-settings.md") &&
			d.Message.Contains("kibana"));
	}

	[Fact]
	public void ValidateRedirectsExists_ValidKibanaKey_NoError()
	{
		var collector = CreateSet("""
			redirects:
			  'old-page.md': 'kibana://get-started/index.md'
			""");

		RedirectErrors(collector).Should().BeEmpty();
	}

	[Fact]
	public void ValidateRedirectsExists_UnknownHost_EmitsError()
	{
		var collector = CreateSet("""
			redirects:
			  'old-page.md': 'not-a-repo://foo.md'
			""");

		collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("not-a-repo"));
	}

	[Fact]
	public void ValidateRedirectsExists_HttpsTarget_NoError()
	{
		var collector = CreateSet("""
			redirects:
			  'old-page.md': 'https://www.elastic.co/docs/get-started'
			""");

		RedirectErrors(collector).Should().BeEmpty();
	}

	[Fact]
	public void ValidateRedirectsExists_ManyWithBadCrossRepo_EmitsError()
	{
		var collector = CreateSet("""
			redirects:
			  'old-page.md':
			    many:
			      - to: 'kibana://get-started/index.md'
			        anchors:
			          'keep': 'keep'
			      - to: 'kibana://docs/reference/advanced-settings.md'
			        anchors:
			          'keep': 'keep'
			""");

		collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("kibana://docs/reference/advanced-settings.md"));
	}

	[Fact]
	public void ValidateRedirectsExists_NoopResolver_NoError()
	{
		var collector = CreateSet("""
			redirects:
			  'old-page.md': 'kibana://docs/reference/advanced-settings.md'
			""", NoopCrossLinkResolver.Instance);

		RedirectErrors(collector).Should().BeEmpty();
	}

	private TestDiagnosticsCollector CreateSet(string redirectYaml, ICrossLinkResolver? resolver = null)
	{
		var logger = new TestLoggerFactory(output);
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{
				"docs/docset.yml", new MockFileData("""
					project: test
					toc:
					- file: index.md
					""")
			},
			{ "docs/redirects.yml", new MockFileData(redirectYaml) },
			{ "docs/index.md", new MockFileData("# Home") }
		}, new MockFileSystemOptions
		{
			CurrentDirectory = Paths.WorkingDirectoryRoot.FullName
		});
		var collector = new TestDiagnosticsCollector(output);
		_ = collector.StartAsync(TestContext.Current.CancellationToken);
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var context = new BuildContext(collector, TestHelpers.CreateDocumentationFileSystem(fileSystem), configurationContext);
		_ = new DocumentationSet(context, logger, resolver ?? new TestCrossLinkResolver());
		return collector;
	}

	private static IEnumerable<Diagnostic> RedirectErrors(TestDiagnosticsCollector collector) =>
		collector.Diagnostics.Where(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("Redirect ", StringComparison.Ordinal));
}
