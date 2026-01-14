// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Documentation.Navigation.Isolated;
using Elastic.Documentation.Navigation.Isolated.Node;
using Elastic.Documentation.Navigation.Tests.Isolation;
using FluentAssertions;

namespace Elastic.Documentation.Navigation.Tests.Assembler;

public class SiteNavigationTests(ITestOutputHelper output)
{
	private TestDocumentationSetContext CreateContext(MockFileSystem? fileSystem = null)
	{
		fileSystem ??= new MockFileSystem();
		var sourceDir = fileSystem.DirectoryInfo.New("/docs");
		var outputDir = fileSystem.DirectoryInfo.New("/output");
		var configPath = fileSystem.FileInfo.New("/docs/navigation.yml");

		return new TestDocumentationSetContext(fileSystem, sourceDir, outputDir, configPath, output);
	}

	[Fact]
	public void ConstructorCreatesSiteNavigation()
	{
		// language=yaml
		var yaml = """
		           toc:
		             - toc: observability://
		               path_prefix: /serverless/observability
		             - toc: serverless-search://
		               path_prefix: /serverless/search
		           """;

		var siteNavFile = SiteNavigationFile.Deserialize(yaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		// Create DocumentationSetNavigation instances for the referenced repos
		var observabilityContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability", output);
		var observabilityDocset = DocumentationSetFile.LoadAndResolve(observabilityContext.Collector, fileSystem.FileInfo.New("/checkouts/current/observability/docs/docset.yml"), fileSystem);
		var observabilityNav = new DocumentationSetNavigation<IDocumentationFile>(observabilityDocset, observabilityContext, GenericDocumentationFileFactory.Instance);

		var searchContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/serverless-search", output);
		var searchDocset = DocumentationSetFile.LoadAndResolve(searchContext.Collector, fileSystem.FileInfo.New("/checkouts/current/serverless-search/docs/docset.yml"), fileSystem);
		var searchNav = new DocumentationSetNavigation<IDocumentationFile>(searchDocset, searchContext, GenericDocumentationFileFactory.Instance);

		var documentationSets = new List<IDocumentationSetNavigation> { observabilityNav, searchNav };

		var siteContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability", output);
		var navigation = new SiteNavigation(siteNavFile, siteContext, documentationSets, sitePrefix: null);

		navigation.Should().NotBeNull();
		navigation.Url.Should().Be("/");
		navigation.NavigationTitle.Should().Be("Elastic Docs");
		navigation.NavigationItems.Should().HaveCount(2);
	}

	[Fact]
	public void SiteNavigationWithNestedChildren()
	{
		// language=yaml
		var yaml = """
		           toc:
		             - toc: platform://
		               path_prefix: /platform
		               children:
		                 - toc: platform://deployment-guide
		                   path_prefix: /platform/deployment
		                 - toc: platform://cloud-guide
		                   path_prefix: /platform/cloud
		           """;

		var siteNavFile = SiteNavigationFile.Deserialize(yaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		// Create DocumentationSetNavigation for platform
		var platformContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/platform", output);
		var platformDocset = DocumentationSetFile.LoadAndResolve(platformContext.Collector, fileSystem.FileInfo.New("/checkouts/current/platform/docs/docset.yml"), fileSystem);
		var platformNav = new DocumentationSetNavigation<IDocumentationFile>(platformDocset, platformContext, GenericDocumentationFileFactory.Instance);

		var documentationSets = new List<IDocumentationSetNavigation> { platformNav };

		var siteContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/platform", output);
		var navigation = new SiteNavigation(siteNavFile, siteContext, documentationSets, sitePrefix: null);

		navigation.NavigationItems.Should().HaveCount(1);

		var platform = navigation.NavigationItems.First();
		platform.Should().NotBeNull();
	}

	[Theory]
	[InlineData(null, "/")]
	[InlineData("", "/")]
	[InlineData("docs", "/docs")]
	[InlineData("/docs", "/docs")]
	[InlineData("docs/", "/docs")]
	[InlineData("/docs/", "/docs")]
	[InlineData("api/docs", "/api/docs")]
	[InlineData("/api/docs", "/api/docs")]
	[InlineData("api/docs/", "/api/docs")]
	[InlineData("/api/docs/", "/api/docs")]
	public void SitePrefixNormalizesSlashes(string? sitePrefix, string expectedRootUrl)
	{
		// language=yaml
		var yaml = """
		           toc:
		             - toc: observability://
		               path_prefix: observability
		           """;

		var siteNavFile = SiteNavigationFile.Deserialize(yaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		var observabilityContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability", output);
		var observabilityDocset = DocumentationSetFile.LoadAndResolve(observabilityContext.Collector, fileSystem.FileInfo.New("/checkouts/current/observability/docs/docset.yml"), fileSystem);
		var observabilityNav = new DocumentationSetNavigation<IDocumentationFile>(observabilityDocset, observabilityContext, GenericDocumentationFileFactory.Instance);

		var documentationSets = new List<IDocumentationSetNavigation> { observabilityNav };

		var siteContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability", output);
		var navigation = new SiteNavigation(siteNavFile, siteContext, documentationSets, sitePrefix);

		navigation.Should().NotBeNull();
		navigation.Url.Should().Be(expectedRootUrl, $"sitePrefix '{sitePrefix}' should normalize to '{expectedRootUrl}'");
	}

	[Theory]
	[InlineData(null, "/observability")]
	[InlineData("", "/observability")]
	[InlineData("docs", "/docs/observability")]
	[InlineData("/docs", "/docs/observability")]
	[InlineData("docs/", "/docs/observability")]
	[InlineData("/docs/", "/docs/observability")]
	[InlineData("api/docs", "/api/docs/observability")]
	[InlineData("/api/docs", "/api/docs/observability")]
	[InlineData("api/docs/", "/api/docs/observability")]
	[InlineData("/api/docs/", "/api/docs/observability")]
	public void SitePrefixAppliedToNavigationItemUrls(string? sitePrefix, string expectedObservabilityUrl)
	{
		// language=yaml
		var yaml = """
		           toc:
		             - toc: observability://
		               path_prefix: observability
		           """;

		var siteNavFile = SiteNavigationFile.Deserialize(yaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		var observabilityContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability", output);
		var observabilityDocset = DocumentationSetFile.LoadAndResolve(observabilityContext.Collector, fileSystem.FileInfo.New("/checkouts/current/observability/docs/docset.yml"), fileSystem);
		var observabilityNav = new DocumentationSetNavigation<IDocumentationFile>(observabilityDocset, observabilityContext, GenericDocumentationFileFactory.Instance);

		var documentationSets = new List<IDocumentationSetNavigation> { observabilityNav };

		var siteContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability", output);
		var navigation = new SiteNavigation(siteNavFile, siteContext, documentationSets, sitePrefix);

		navigation.NavigationItems.Should().HaveCount(1);
		var observabilityItem = navigation.NavigationItems.First();
		observabilityItem.Url.Should().Be(expectedObservabilityUrl, $"sitePrefix '{sitePrefix}' should result in URL '{expectedObservabilityUrl}'");
	}

	[Fact]
	public void NavigationNodeIdsAreUniqueAcrossDocsets()
	{
		// This test verifies that navigation node IDs are unique even when
		// multiple docsets have folders with the same relative path.
		// This is critical because IDs are used as HTML id attributes.

		// Create two docsets, each with a "getting-started" folder at the same relative path
		var fileSystem = new MockFileSystem();

		// Docset 1: product-a with getting-started folder
		var productADir = "/checkouts/current/product-a";
		// language=yaml
		var productADocset = """
		                     project: product-a
		                     toc:
		                       - file: index.md
		                       - folder: getting-started
		                         children:
		                           - file: index.md
		                           - file: quick-start.md
		                     """;
		fileSystem.AddFile($"{productADir}/docs/docset.yml", new MockFileData(productADocset));
		fileSystem.AddFile($"{productADir}/docs/index.md", new MockFileData("# Product A"));
		fileSystem.AddFile($"{productADir}/docs/getting-started/index.md", new MockFileData("# Getting Started A"));
		fileSystem.AddFile($"{productADir}/docs/getting-started/quick-start.md", new MockFileData("# Quick Start A"));

		// Docset 2: product-b with getting-started folder (same relative path!)
		var productBDir = "/checkouts/current/product-b";
		// language=yaml
		var productBDocset = """
		                     project: product-b
		                     toc:
		                       - file: index.md
		                       - folder: getting-started
		                         children:
		                           - file: index.md
		                           - file: tutorial.md
		                     """;
		fileSystem.AddFile($"{productBDir}/docs/docset.yml", new MockFileData(productBDocset));
		fileSystem.AddFile($"{productBDir}/docs/index.md", new MockFileData("# Product B"));
		fileSystem.AddFile($"{productBDir}/docs/getting-started/index.md", new MockFileData("# Getting Started B"));
		fileSystem.AddFile($"{productBDir}/docs/getting-started/tutorial.md", new MockFileData("# Tutorial B"));

		// Create navigation for both docsets
		var productAContext = SiteNavigationTestFixture.CreateContext(fileSystem, productADir, output);
		var productADocsetFile = DocumentationSetFile.LoadAndResolve(productAContext.Collector, fileSystem.FileInfo.New($"{productADir}/docs/docset.yml"), fileSystem);
		var productANav = new DocumentationSetNavigation<IDocumentationFile>(productADocsetFile, productAContext, GenericDocumentationFileFactory.Instance);

		var productBContext = SiteNavigationTestFixture.CreateContext(fileSystem, productBDir, output);
		var productBDocsetFile = DocumentationSetFile.LoadAndResolve(productBContext.Collector, fileSystem.FileInfo.New($"{productBDir}/docs/docset.yml"), fileSystem);
		var productBNav = new DocumentationSetNavigation<IDocumentationFile>(productBDocsetFile, productBContext, GenericDocumentationFileFactory.Instance);

		// Get the "getting-started" folders from each docset
		var productAGettingStarted = productANav.NavigationItems.First()
			.Should().BeOfType<FolderNavigation<IDocumentationFile>>().Subject;
		var productBGettingStarted = productBNav.NavigationItems.First()
			.Should().BeOfType<FolderNavigation<IDocumentationFile>>().Subject;

		// Both folders have the same relative path within their docsets
		productAGettingStarted.FolderPath.Should().Be("getting-started");
		productBGettingStarted.FolderPath.Should().Be("getting-started");

		// But they MUST have different IDs because they resolve to different URLs
		// Product A: /product-a/getting-started
		// Product B: /product-b/getting-started
		productAGettingStarted.Id.Should().NotBe(productBGettingStarted.Id,
			"folders with the same relative path but in different docsets should have different IDs " +
			"because they have different URLs");

		// Also verify in assembled navigation context
		// language=yaml
		var siteNavYaml = """
		                  toc:
		                    - toc: product-a://
		                      path_prefix: /product-a
		                    - toc: product-b://
		                      path_prefix: /product-b
		                  """;
		var siteNavFile = SiteNavigationFile.Deserialize(siteNavYaml);
		var documentationSets = new List<IDocumentationSetNavigation> { productANav, productBNav };
		var siteContext = SiteNavigationTestFixture.CreateContext(fileSystem, productADir, output);
		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets, sitePrefix: null);

		// Use production YieldAll() to collect all navigation items
		var allNodeItems = ((INavigationTraversable)siteNavigation).YieldAll()
			.OfType<INodeNavigationItem<INavigationModel, INavigationItem>>()
			.ToList();

		// Verify all IDs are unique
		var allIds = allNodeItems.Select(n => n.Id).ToList();
		var uniqueIds = allIds.Distinct().ToList();
		uniqueIds.Should().HaveCount(allIds.Count,
			$"all navigation node IDs should be unique in assembled navigation. " +
			$"Found duplicates: {string.Join(", ", allIds.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => $"ID '{g.Key}' appears {g.Count()} times"))}");
	}

	[Theory]
	[InlineData(null, "/observability", "/search")]
	[InlineData("docs", "/docs/observability", "/docs/search")]
	[InlineData("/docs", "/docs/observability", "/docs/search")]
	[InlineData("docs/", "/docs/observability", "/docs/search")]
	[InlineData("/docs/", "/docs/observability", "/docs/search")]
	public void SitePrefixAppliedToMultipleNavigationItems(string? sitePrefix, string expectedObsUrl, string expectedSearchUrl)
	{
		// language=yaml
		var yaml = """
		           toc:
		             - toc: observability://
		               path_prefix: observability
		             - toc: serverless-search://
		               path_prefix: search
		           """;

		var siteNavFile = SiteNavigationFile.Deserialize(yaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		var observabilityContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability", output);
		var observabilityDocset = DocumentationSetFile.LoadAndResolve(observabilityContext.Collector, fileSystem.FileInfo.New("/checkouts/current/observability/docs/docset.yml"), fileSystem);
		var observabilityNav = new DocumentationSetNavigation<IDocumentationFile>(observabilityDocset, observabilityContext, GenericDocumentationFileFactory.Instance);

		var searchContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/serverless-search", output);
		var searchDocset = DocumentationSetFile.LoadAndResolve(searchContext.Collector, fileSystem.FileInfo.New("/checkouts/current/serverless-search/docs/docset.yml"), fileSystem);
		var searchNav = new DocumentationSetNavigation<IDocumentationFile>(searchDocset, searchContext, GenericDocumentationFileFactory.Instance);

		var documentationSets = new List<IDocumentationSetNavigation> { observabilityNav, searchNav };

		var siteContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability", output);
		var navigation = new SiteNavigation(siteNavFile, siteContext, documentationSets, sitePrefix);

		navigation.NavigationItems.Should().HaveCount(2);

		var observabilityItem = navigation.NavigationItems.First();
		observabilityItem.Url.Should().Be(expectedObsUrl, $"observability URL should be '{expectedObsUrl}' with sitePrefix '{sitePrefix}'");

		var searchItem = navigation.NavigationItems.Skip(1).First();
		searchItem.Url.Should().Be(expectedSearchUrl, $"search URL should be '{expectedSearchUrl}' with sitePrefix '{sitePrefix}'");
	}
}
