// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Navigation;
using Elastic.Markdown.IO;
using Nullean.ScopedFileSystem;

namespace Elastic.Markdown.Tests.DocSet;

/// <summary>
/// Tests that the "Report a docs issue" URL and breadcrumb structured-data URLs
/// are built correctly and never contain a duplicated path prefix (e.g. /docs/docs/).
///
/// Regression guard for: https://github.com/elastic/docs-content/issues/5788
/// The bug was that HtmlWriter called UrlPath.JoinUrl(UrlPathPrefix, current.Url),
/// but current.Url already contains the prefix because navigation sets PathPrefix = UrlPathPrefix.
/// </summary>
public class ReportIssueUrlTests : IAsyncLifetime
{
	private static readonly Uri CanonicalBaseUrl = new("https://www.elastic.co/");
	private const string UrlPathPrefix = "docs";

	private DocumentationSet Set { get; }
	private DocumentationGenerator Generator { get; }

	public ReportIssueUrlTests(ITestOutputHelper output)
	{
		var loggerFactory = new TestLoggerFactory(output);
		var mockWriteFs = new MockFileSystem(new MockFileSystemOptions
		{
			CurrentDirectory = Paths.WorkingDirectoryRoot.FullName
		});
		var readFileSystem = FileSystemFactory.RealRead;
		var writeFileSystem = FileSystemFactory.ScopeCurrentWorkingDirectory(mockWriteFs);
		var collector = new TestDiagnosticsCollector(output);
		var configurationContext = TestHelpers.CreateConfigurationContext(readFileSystem);

		var context = new BuildContext(collector, readFileSystem, writeFileSystem, configurationContext, ExportOptions.Default)
		{
			Force = false,
			UrlPathPrefix = UrlPathPrefix,
			CanonicalBaseUrl = CanonicalBaseUrl
		};

		Set = new DocumentationSet(context, loggerFactory, new TestCrossLinkResolver());
		Generator = new DocumentationGenerator(Set, loggerFactory);
	}

	public async ValueTask InitializeAsync() => await Generator.ResolveDirectoryTree(default);

	public ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		return ValueTask.CompletedTask;
	}

	[Fact]
	public void NavigationItemUrls_WithUrlPathPrefix_AlreadyIncludeThePrefix()
	{
		// When UrlPathPrefix = "docs", the navigation PathPrefix is "/docs",
		// so every navigation item URL should already start with "/docs/".
		var navUrl = Set.Navigation.Url;

		navUrl.Should().StartWith($"/{UrlPathPrefix}");
	}

	[Fact]
	public void ReportIssueUrl_WithUrlPathPrefix_DoesNotDuplicatePrefix()
	{
		// Simulate exactly what HtmlWriter does to build the report link parameter:
		//   reportLinkParameter = new Uri(CanonicalBaseUrl, current.Url)
		// current.Url already contains "/docs/..." because navigation incorporates PathPrefix.
		// Prepending UrlPathPrefix a second time would produce "/docs/docs/…" (the old bug).
		var currentUrl = Set.Navigation.Url;
		var reportLinkParameter = new Uri(CanonicalBaseUrl, currentUrl);

		reportLinkParameter.AbsoluteUri.Should().NotContain($"/{UrlPathPrefix}/{UrlPathPrefix}/");
		reportLinkParameter.AbsoluteUri.Should().StartWith($"{CanonicalBaseUrl.AbsoluteUri.TrimEnd('/')}/{UrlPathPrefix}");
	}

	[Fact]
	public void BreadcrumbUrl_WithUrlPathPrefix_DoesNotDuplicatePrefix()
	{
		// Simulate what HtmlWriter does for breadcrumb structured data:
		//   Item = new Uri(CanonicalBaseUrl ?? localhost, parent.Url).ToString()
		// Same bug would have applied here if the old JoinUrl call had been used.
		INavigationTraversable traversable = Set;
		var nestedFile = Set.MarkdownFiles
			.First(f => traversable.GetParentsOfMarkdownFile(f).Length > 0);
		var parents = traversable.GetParentsOfMarkdownFile(nestedFile);

		parents.Should().NotBeEmpty();
		foreach (var parent in parents)
		{
			var breadcrumbUrl = new Uri(CanonicalBaseUrl, parent.Url).ToString();

			breadcrumbUrl.Should().NotContain($"/{UrlPathPrefix}/{UrlPathPrefix}/");
		}
	}
}
