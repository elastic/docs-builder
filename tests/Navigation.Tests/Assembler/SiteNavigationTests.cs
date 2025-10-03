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
		var observabilityDocset = DocumentationSetFile.Deserialize(fileSystem.File.ReadAllText("/checkouts/current/observability/docs/docset.yml"));
		var observabilityNav = new DocumentationSetNavigation(observabilityDocset, observabilityContext, TestDocumentationFileFactory.Instance);

		var searchContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/serverless-search", output);
		var searchDocset = DocumentationSetFile.Deserialize(fileSystem.File.ReadAllText("/checkouts/current/serverless-search/docs/docset.yml"));
		var searchNav = new DocumentationSetNavigation(searchDocset, searchContext, TestDocumentationFileFactory.Instance);

		var documentationSets = new List<DocumentationSetNavigation> { observabilityNav, searchNav };

		var siteContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability", output);
		var navigation = new SiteNavigation(siteNavFile, siteContext, documentationSets);

		navigation.Should().NotBeNull();
		navigation.Url.Should().Be("/");
		navigation.NavigationTitle.Should().Be("Site Navigation");
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
		var platformDocset = DocumentationSetFile.Deserialize(fileSystem.File.ReadAllText("/checkouts/current/platform/docs/docset.yml"));
		var platformNav = new DocumentationSetNavigation(platformDocset, platformContext, TestDocumentationFileFactory.Instance);

		var documentationSets = new List<DocumentationSetNavigation> { platformNav };

		var siteContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/platform", output);
		var navigation = new SiteNavigation(siteNavFile, siteContext, documentationSets);

		navigation.NavigationItems.Should().HaveCount(1);

		var platform = navigation.NavigationItems.First();
		platform.Should().NotBeNull();
	}
}
