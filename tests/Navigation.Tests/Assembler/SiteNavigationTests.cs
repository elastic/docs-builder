// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Documentation.Navigation.Isolated;
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
		navigation.NavigationTitle.Should().Be("Serverless Observability");
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
