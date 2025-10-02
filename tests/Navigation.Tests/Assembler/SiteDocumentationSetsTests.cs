// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Documentation.Navigation.Isolated;
using FluentAssertions;

namespace Elastic.Documentation.Navigation.Tests.Assembler;

public class SiteDocumentationSetsTests(ITestOutputHelper output)
{
	[Fact]
	public void CreatesDocumentationSetNavigationsFromCheckoutFolders()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		// Discover all repositories in /checkouts/current
		var checkoutDir = fileSystem.DirectoryInfo.New("/checkouts/current");
		var repositories = checkoutDir.GetDirectories();

		repositories.Should().HaveCount(5);
		repositories.Select(r => r.Name).Should().Contain(
		[
			"observability",
			"serverless-search",
			"serverless-security",
			"platform",
			"elasticsearch-reference"
		]);

		// Create DocumentationSetNavigation for each repository
		var documentationSets = new List<DocumentationSetNavigation>();

		foreach (var repo in repositories)
		{
			var context = SiteNavigationTestFixture.CreateContext(fileSystem, repo.FullName, output);

			// Read the docset file
			var docsetPath = fileSystem.File.Exists($"{repo.FullName}/docs/docset.yml")
				? $"{repo.FullName}/docs/docset.yml"
				: $"{repo.FullName}/docs/_docset.yml";

			var docsetYaml = fileSystem.File.ReadAllText(docsetPath);
			var docset = DocumentationSetFile.Deserialize(docsetYaml);

			var navigation = new DocumentationSetNavigation(docset, context);
			documentationSets.Add(navigation);
		}

		documentationSets.Should().HaveCount(5);

		// Verify each documentation set has navigation items
		foreach (var docSet in documentationSets)
			docSet.NavigationItems.Should().NotBeEmpty();
	}

	[Fact]
	public void SiteNavigationIntegratesWithDocumentationSets()
	{
		// language=yaml
		var siteNavYaml = """
		                  toc:
		                    - toc: observability://
		                    - toc: serverless-search://
		                    - toc: serverless-security://
		                  """;

		var siteNavFile = SiteNavigationFile.Deserialize(siteNavYaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		// Create DocumentationSetNavigation instances
		var documentationSets = new List<DocumentationSetNavigation>();

		var observabilityContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability", output);
		var observabilityDocset = DocumentationSetFile.Deserialize(fileSystem.File.ReadAllText("/checkouts/current/observability/docs/docset.yml"));
		documentationSets.Add(new DocumentationSetNavigation(observabilityDocset, observabilityContext));

		var searchContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/serverless-search", output);
		var searchDocset = DocumentationSetFile.Deserialize(fileSystem.File.ReadAllText("/checkouts/current/serverless-search/docs/docset.yml"));
		documentationSets.Add(new DocumentationSetNavigation(searchDocset, searchContext));

		var securityContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/serverless-security", output);
		var securityDocset = DocumentationSetFile.Deserialize(fileSystem.File.ReadAllText("/checkouts/current/serverless-security/docs/_docset.yml"));
		documentationSets.Add(new DocumentationSetNavigation(securityDocset, securityContext));

		// Create site navigation context (using any repository's filesystem)
		var siteContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability", output);

		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets);

		siteNavigation.Should().NotBeNull();
		siteNavigation.NavigationItems.Should().HaveCount(3);

		var observability = siteNavigation.NavigationItems.ElementAt(0);
		observability.Url.Should().Be("/");
		observability.NavigationTitle.Should().NotBeNullOrEmpty();

		var search = siteNavigation.NavigationItems.ElementAt(1);
		search.Url.Should().Be("/");

		var security = siteNavigation.NavigationItems.ElementAt(2);
		security.Url.Should().Be("/");
	}

	[Fact]
	public void SiteNavigationWithNestedTocs()
	{
		// language=yaml
		var siteNavYaml = """
		                  toc:
		                    - toc: platform://
		                      children:
		                        - toc: platform:///deployment-guide
		                        - toc: platform:///cloud-guide
		                  """;

		var siteNavFile = SiteNavigationFile.Deserialize(siteNavYaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		// Create DocumentationSetNavigation for platform
		var platformContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/platform", output);
		var platformDocset = DocumentationSetFile.Deserialize(fileSystem.File.ReadAllText("/checkouts/current/platform/docs/docset.yml"));
		var platformNav = new DocumentationSetNavigation(platformDocset, platformContext);

		var documentationSets = new List<DocumentationSetNavigation> { platformNav };

		var siteContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/platform", output);

		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets);

		siteNavigation.NavigationItems.Should().HaveCount(1);

		var platform = siteNavigation.NavigationItems.First() as INodeNavigationItem<INavigationModel, INavigationItem>;
		platform.Should().NotBeNull();
		platform.Url.Should().Be("/"); // Root DocumentationSetNavigation URL
		// 2 toc's + its index
		platform.NavigationItems.Should().HaveCount(3);

		// verify TOC children starting from index 1
		var deployment = platform.NavigationItems.ElementAt(1);
		deployment.Url.Should().Be("/deployment-guide");

		var cloud = platform.NavigationItems.ElementAt(2);
		cloud.Url.Should().Be("/cloud-guide");
	}

	[Fact]
	public void SiteNavigationWithAllRepositories()
	{
		// language=yaml
		var siteNavYaml = """
		                  toc:
		                    - toc: observability://
		                    - toc: serverless-search://
		                    - toc: serverless-security://
		                    - toc: platform://
		                      children:
		                        - toc: platform://deployment-guide
		                        - toc: platform://cloud-guide
		                    - toc: elasticsearch-reference://
		                  """;

		var siteNavFile = SiteNavigationFile.Deserialize(siteNavYaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		// Create all DocumentationSetNavigation instances
		var checkoutDir = fileSystem.DirectoryInfo.New("/checkouts/current");
		var repositories = checkoutDir.GetDirectories();

		var documentationSets = new List<DocumentationSetNavigation>();

		foreach (var repo in repositories)
		{
			var context = SiteNavigationTestFixture.CreateContext(fileSystem, repo.FullName, output);

			var docsetPath = fileSystem.File.Exists($"{repo.FullName}/docs/docset.yml")
				? $"{repo.FullName}/docs/docset.yml"
				: $"{repo.FullName}/docs/_docset.yml";

			var docsetYaml = fileSystem.File.ReadAllText(docsetPath);
			var docset = DocumentationSetFile.Deserialize(docsetYaml);

			var navigation = new DocumentationSetNavigation(docset, context);
			documentationSets.Add(navigation);
		}

		var siteContext = SiteNavigationTestFixture.CreateContext(
			fileSystem, "/checkouts/current/observability", output);

		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets);

		siteNavigation.Should().NotBeNull();
		siteNavigation.NavigationItems.Should().HaveCount(5);

		// Verify top-level items
		var observability = siteNavigation.NavigationItems.ElementAt(0);
		observability.Url.Should().Be("/");

		var search = siteNavigation.NavigationItems.ElementAt(1);
		search.Url.Should().Be("/");

		var security = siteNavigation.NavigationItems.ElementAt(2);
		security.Url.Should().Be("/");

		var platform = siteNavigation.NavigationItems.ElementAt(3) as INodeNavigationItem<INavigationModel, INavigationItem>;
		platform.Should().NotBeNull();
		platform.Url.Should().Be("/");
		// 2 toc's + its index
		platform.NavigationItems.Should().HaveCount(3);

		var elasticsearch = siteNavigation.NavigationItems.ElementAt(4);
		elasticsearch.Url.Should().Be("/");
	}

	[Fact]
	public void DocumentationSetNavigationHasCorrectStructure()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		// Test observability repository structure
		var observabilityContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability", output);
		var observabilityDocset = DocumentationSetFile.Deserialize(fileSystem.File.ReadAllText("/checkouts/current/observability/docs/docset.yml"));
		var observabilityNav = new DocumentationSetNavigation(observabilityDocset, observabilityContext);

		observabilityNav.NavigationTitle.Should().Be("serverless-observability");
		observabilityNav.NavigationItems.Should().HaveCount(3); // index.md, getting-started folder, monitoring folder

		var indexFile = observabilityNav.NavigationItems.ElementAt(0);
		indexFile.Should().BeOfType<FileNavigationLeaf>();
		indexFile.Url.Should().Be("/");

		var gettingStarted = observabilityNav.NavigationItems.ElementAt(1);
		gettingStarted.Should().BeOfType<FolderNavigation>();
		var gettingStartedFolder = (FolderNavigation)gettingStarted;
		gettingStartedFolder.NavigationItems.Should().HaveCount(2); // quick-start.md, installation.md

		var monitoring = observabilityNav.NavigationItems.ElementAt(2);
		monitoring.Should().BeOfType<FolderNavigation>();
		var monitoringFolder = (FolderNavigation)monitoring;
		monitoringFolder.NavigationItems.Should().HaveCount(4); // index.md, logs.md, metrics.md, traces.md
	}

	[Fact]
	public void DocumentationSetWithNestedTocs()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		// Test platform repository with nested TOCs
		var platformContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/platform", output);
		var platformDocset = DocumentationSetFile.Deserialize(fileSystem.File.ReadAllText("/checkouts/current/platform/docs/docset.yml"));
		var platformNav = new DocumentationSetNavigation(platformDocset, platformContext);

		platformNav.NavigationTitle.Should().Be("platform");
		platformNav.NavigationItems.Should().HaveCount(3); // index.md, deployment-guide TOC, cloud-guide TOC

		var indexFile = platformNav.NavigationItems.ElementAt(0);
		indexFile.Should().BeOfType<FileNavigationLeaf>();
		indexFile.Url.Should().Be("/");

		var deploymentGuide = platformNav.NavigationItems.ElementAt(1);
		deploymentGuide.Should().BeOfType<TableOfContentsNavigation>();
		deploymentGuide.Url.Should().Be("/deployment-guide");
		var deploymentToc = (TableOfContentsNavigation)deploymentGuide;
		deploymentToc.NavigationItems.Should().HaveCount(2); // index.md, self-managed folder

		var cloudGuide = platformNav.NavigationItems.ElementAt(2);
		cloudGuide.Should().BeOfType<TableOfContentsNavigation>();
		cloudGuide.Url.Should().Be("/cloud-guide");
		var cloudToc = (TableOfContentsNavigation)cloudGuide;
		cloudToc.NavigationItems.Should().HaveCount(3); // index.md, aws folder, azure folder
	}

	[Fact]
	public void DocumentationSetWithUnderscoreDocset()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		// Test serverless-security repository with _docset.yml
		var securityContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/serverless-security", output);
		var securityDocset = DocumentationSetFile.Deserialize(fileSystem.File.ReadAllText("/checkouts/current/serverless-security/docs/_docset.yml"));
		var securityNav = new DocumentationSetNavigation(securityDocset, securityContext);

		securityNav.NavigationTitle.Should().Be("serverless-security");
		securityNav.NavigationItems.Should().HaveCount(3); // index.md, authentication folder, authorization folder

		var authentication = securityNav.NavigationItems.ElementAt(1);
		authentication.Should().BeOfType<FolderNavigation>();
		var authenticationFolder = (FolderNavigation)authentication;
		authenticationFolder.NavigationItems.Should().HaveCount(3); // index.md, api-keys.md, oauth.md

		var authorization = securityNav.NavigationItems.ElementAt(2);
		authorization.Should().BeOfType<FolderNavigation>();
		var authorizationFolder = (FolderNavigation)authorization;
		authorizationFolder.NavigationItems.Should().HaveCount(2); // index.md, rbac.md
	}
}
