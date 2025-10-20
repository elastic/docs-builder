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
		var collector = new TestDiagnosticsCollector(output);
		_ = collector.StartAsync(TestContext.Current.CancellationToken);

		// Create DocumentationSetNavigation for each repository
		var documentationSets = new List<IDocumentationSetNavigation>();

		foreach (var repo in repositories)
		{
			var context = SiteNavigationTestFixture.CreateContext(fileSystem, repo.FullName, output, collector);

			// Read the docset file
			var docsetPath = fileSystem.File.Exists($"{repo.FullName}/docs/docset.yml")
				? $"{repo.FullName}/docs/docset.yml"
				: $"{repo.FullName}/docs/_docset.yml";

			var docset = DocumentationSetFile.LoadAndResolve(context.Collector, fileSystem.FileInfo.New(docsetPath), fileSystem);

			var navigation = new DocumentationSetNavigation<IDocumentationFile>(docset, context, GenericDocumentationFileFactory.Instance);
			documentationSets.Add(navigation);
		}

		documentationSets.Should().HaveCount(5);

		// Verify each documentation set has navigation items
		foreach (var docSet in documentationSets)
			docSet.NavigationItems.Should().NotBeEmpty();

		collector.Errors.Should().Be(0, "there should be no errors when loading observability documentation set");
		collector.Warnings.Should().Be(0, "there should be no warnings when loading observability documentation set");
	}

	[Fact]
	public void SiteNavigationIntegratesWithDocumentationSets()
	{
		// language=yaml
		var siteNavYaml = """
		                  toc:
		                    - toc: observability://
		                      path_prefix: /serverless/observability
		                    - toc: serverless-search://
		                      path_prefix: /serverless/search
		                    - toc: serverless-security://
		                      path_prefix: /serverless/security
		                  """;

		var siteNavFile = SiteNavigationFile.Deserialize(siteNavYaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		// Create DocumentationSetNavigation instances
		var documentationSets = new List<IDocumentationSetNavigation>();

		var observabilityContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability", output);
		var observabilityDocset = DocumentationSetFile.LoadAndResolve(observabilityContext.Collector, fileSystem.FileInfo.New("/checkouts/current/observability/docs/docset.yml"), fileSystem);
		documentationSets.Add(new DocumentationSetNavigation<IDocumentationFile>(observabilityDocset, observabilityContext, GenericDocumentationFileFactory.Instance));

		var searchContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/serverless-search", output);
		var searchDocset = DocumentationSetFile.LoadAndResolve(searchContext.Collector, fileSystem.FileInfo.New("/checkouts/current/serverless-search/docs/docset.yml"), fileSystem);
		documentationSets.Add(new DocumentationSetNavigation<IDocumentationFile>(searchDocset, searchContext, GenericDocumentationFileFactory.Instance));

		var securityContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/serverless-security", output);
		var securityDocset = DocumentationSetFile.LoadAndResolve(securityContext.Collector, fileSystem.FileInfo.New("/checkouts/current/serverless-security/docs/_docset.yml"), fileSystem);
		documentationSets.Add(new DocumentationSetNavigation<IDocumentationFile>(securityDocset, securityContext, GenericDocumentationFileFactory.Instance));

		// Create site navigation context (using any repository's filesystem)
		var siteContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability", output);

		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets);

		siteNavigation.Should().NotBeNull();
		siteNavigation.NavigationItems.Should().HaveCount(3);

		var observability = siteNavigation.NavigationItems.ElementAt(0);
		observability.Url.Should().Be("/serverless/observability");
		observability.NavigationTitle.Should().NotBeNullOrEmpty();

		var search = siteNavigation.NavigationItems.ElementAt(1);
		search.Url.Should().Be("/serverless/search");

		var security = siteNavigation.NavigationItems.ElementAt(2);
		security.Url.Should().Be("/serverless/security");
	}

	[Fact]
	public void SiteNavigationWithNestedTocs()
	{
		// language=yaml
		var siteNavYaml = """
		                  toc:
		                    - toc: platform://
		                      path_prefix: /platform
		                      children:
		                        - toc: platform://deployment-guide
		                          path_prefix: /platform/deployment
		                        - toc: platform://cloud-guide
		                          path_prefix: /platform/cloud
		                  """;

		var siteNavFile = SiteNavigationFile.Deserialize(siteNavYaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		// Create DocumentationSetNavigation for platform
		var platformContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/platform", output);
		var platformDocset = DocumentationSetFile.LoadAndResolve(platformContext.Collector, fileSystem.FileInfo.New("/checkouts/current/platform/docs/docset.yml"), fileSystem);
		var platformNav = new DocumentationSetNavigation<IDocumentationFile>(platformDocset, platformContext, GenericDocumentationFileFactory.Instance);

		var documentationSets = new List<IDocumentationSetNavigation> { platformNav };

		var siteContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/platform", output);

		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets);

		siteNavigation.NavigationItems.Should().HaveCount(1);

		var platform = siteNavigation.NavigationItems.First() as INodeNavigationItem<INavigationModel, INavigationItem>;
		platform.Should().NotBeNull();
		platform.Url.Should().Be("/platform");
		platform.NavigationItems.Should().HaveCount(2);

		var deployment = platform.NavigationItems.ElementAt(0);
		deployment.Url.Should().Be("/platform/deployment");

		var cloud = platform.NavigationItems.ElementAt(1);
		cloud.Url.Should().Be("/platform/cloud");
	}

	[Fact]
	public void SiteNavigationWithAllRepositories()
	{
		// language=yaml
		var siteNavYaml = """
		                  toc:
		                    - toc: observability://
		                      path_prefix: /serverless/observability
		                    - toc: serverless-search://
		                      path_prefix: /serverless/search
		                    - toc: serverless-security://
		                      path_prefix: /serverless/security
		                    - toc: platform://
		                      path_prefix: /platform
		                      children:
		                        - toc: platform://deployment-guide
		                          path_prefix: /platform/deployment
		                        - toc: platform://cloud-guide
		                          path_prefix: /platform/cloud
		                    - toc: elasticsearch-reference://
		                      path_prefix: /elasticsearch/reference
		                  """;

		var siteNavFile = SiteNavigationFile.Deserialize(siteNavYaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		// Create all DocumentationSetNavigation instances
		var checkoutDir = fileSystem.DirectoryInfo.New("/checkouts/current");
		var repositories = checkoutDir.GetDirectories();

		var documentationSets = new List<IDocumentationSetNavigation>();

		foreach (var repo in repositories)
		{
			var context = SiteNavigationTestFixture.CreateContext(fileSystem, repo.FullName, output);

			var docsetPath = fileSystem.File.Exists($"{repo.FullName}/docs/docset.yml")
				? $"{repo.FullName}/docs/docset.yml"
				: $"{repo.FullName}/docs/_docset.yml";

			var docset = DocumentationSetFile.LoadAndResolve(context.Collector, fileSystem.FileInfo.New(docsetPath), fileSystem);

			var navigation = new DocumentationSetNavigation<IDocumentationFile>(docset, context, GenericDocumentationFileFactory.Instance);
			documentationSets.Add(navigation);
		}

		var siteContext = SiteNavigationTestFixture.CreateContext(
			fileSystem, "/checkouts/current/observability", output);

		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets);

		siteNavigation.Should().NotBeNull();
		siteNavigation.NavigationItems.Should().HaveCount(5);

		// Verify top-level items
		var observability = siteNavigation.NavigationItems.ElementAt(0);
		observability.Url.Should().Be("/serverless/observability");

		var search = siteNavigation.NavigationItems.ElementAt(1);
		search.Url.Should().Be("/serverless/search");

		var security = siteNavigation.NavigationItems.ElementAt(2);
		security.Url.Should().Be("/serverless/security");

		var platform = siteNavigation.NavigationItems.ElementAt(3) as INodeNavigationItem<INavigationModel, INavigationItem>;
		platform.Should().NotBeNull();
		platform.Url.Should().Be("/platform");
		platform.NavigationItems.Should().HaveCount(2);

		var elasticsearch = siteNavigation.NavigationItems.ElementAt(4);
		elasticsearch.Url.Should().Be("/elasticsearch/reference");
	}

	[Fact]
	public void DocumentationSetNavigationHasCorrectStructure()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		// Test observability repository structure
		var observabilityContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability", output);
		var observabilityDocset = DocumentationSetFile.LoadAndResolve(observabilityContext.Collector, fileSystem.FileInfo.New("/checkouts/current/observability/docs/docset.yml"), fileSystem);
		var observabilityNav = new DocumentationSetNavigation<IDocumentationFile>(observabilityDocset, observabilityContext, GenericDocumentationFileFactory.Instance);

		observabilityNav.NavigationTitle.Should().Be("Serverless Observability");
		observabilityNav.NavigationItems.Should().HaveCount(3); // index.md, getting-started folder, monitoring folder

		var indexFile = observabilityNav.NavigationItems.ElementAt(0);
		indexFile.Should().BeOfType<FileNavigationLeaf<IDocumentationFile>>();
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
		var platformDocset = DocumentationSetFile.LoadAndResolve(platformContext.Collector, fileSystem.FileInfo.New("/checkouts/current/platform/docs/docset.yml"), fileSystem);
		var platformNav = new DocumentationSetNavigation<IDocumentationFile>(platformDocset, platformContext, GenericDocumentationFileFactory.Instance);

		platformNav.NavigationTitle.Should().Be("Platform");
		platformNav.NavigationItems.Should().HaveCount(3); // index.md, deployment-guide TOC, cloud-guide TOC

		var indexFile = platformNav.NavigationItems.ElementAt(0);
		indexFile.Should().BeOfType<FileNavigationLeaf<IDocumentationFile>>();
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
		var securityDocset = DocumentationSetFile.LoadAndResolve(securityContext.Collector, fileSystem.FileInfo.New("/checkouts/current/serverless-security/docs/_docset.yml"), fileSystem);
		var securityNav = new DocumentationSetNavigation<IDocumentationFile>(securityDocset, securityContext, GenericDocumentationFileFactory.Instance);

		securityNav.NavigationTitle.Should().Be("Serverless Security");
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

	[Fact]
	public void SiteNavigationAppliesPathPrefixToAllUrls()
	{
		// language=yaml
		var siteNavYaml = """
		                  toc:
		                    - toc: observability://
		                      path_prefix: /serverless/observability
		                  """;

		var siteNavFile = SiteNavigationFile.Deserialize(siteNavYaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		var observabilityContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability", output);
		var observabilityDocset = DocumentationSetFile.LoadAndResolve(observabilityContext.Collector, fileSystem.FileInfo.New("/checkouts/current/observability/docs/docset.yml"), fileSystem);
		var documentationSets = new List<IDocumentationSetNavigation> { new DocumentationSetNavigation<IDocumentationFile>(observabilityDocset, observabilityContext, GenericDocumentationFileFactory.Instance) };

		var siteContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability", output);
		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets);

		// Verify root URL has path prefix
		var root = siteNavigation.NavigationItems.First();
		root.Url.Should().StartWith("/serverless/observability");

		// Verify all nested items also have the path prefix
		if (root is INodeNavigationItem<INavigationModel, INavigationItem> nodeItem)
		{
			foreach (var item in nodeItem.NavigationItems)
				item.Url.Should().StartWith("/serverless/observability");
		}
	}

	[Fact]
	public void SiteNavigationWithNestedTocsAppliesCorrectPathPrefixes()
	{
		// language=yaml
		var siteNavYaml = """
		                  toc:
		                    - toc: platform://
		                      path_prefix: /platform
		                      children:
		                        - toc: platform://deployment-guide
		                          path_prefix: /platform/deployment
		                        - toc: platform://cloud-guide
		                          path_prefix: /platform/cloud
		                  """;

		var siteNavFile = SiteNavigationFile.Deserialize(siteNavYaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		var platformContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/platform", output);
		var platformDocset = DocumentationSetFile.LoadAndResolve(platformContext.Collector, fileSystem.FileInfo.New("/checkouts/current/platform/docs/docset.yml"), fileSystem);
		var documentationSets = new List<IDocumentationSetNavigation> { new DocumentationSetNavigation<IDocumentationFile>(platformDocset, platformContext, GenericDocumentationFileFactory.Instance) };

		var siteContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/platform", output);
		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets);

		var platform = siteNavigation.NavigationItems.First() as INodeNavigationItem<INavigationModel, INavigationItem>;
		platform.Should().NotBeNull();
		platform.Url.Should().Be("/platform");

		// Verify child TOCs have their specific path prefixes
		var deployment = platform.NavigationItems.ElementAt(0);
		deployment.Url.Should().StartWith("/platform/deployment");

		var cloud = platform.NavigationItems.ElementAt(1);
		cloud.Url.Should().StartWith("/platform/cloud");
	}

	[Fact]
	public void SiteNavigationRequiresPathPrefix()
	{
		// language=yaml - missing path_prefix
		var siteNavYaml = """
		                  toc:
		                    - toc: observability://
		                  """;

		var siteNavFile = SiteNavigationFile.Deserialize(siteNavYaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		var observabilityContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability", output);
		var observabilityDocset = DocumentationSetFile.LoadAndResolve(observabilityContext.Collector, fileSystem.FileInfo.New("/checkouts/current/observability/docs/docset.yml"), fileSystem);
		var documentationSets = new List<IDocumentationSetNavigation> { new DocumentationSetNavigation<IDocumentationFile>(observabilityDocset, observabilityContext, GenericDocumentationFileFactory.Instance) };

		var siteContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability", output);
		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets);

		// navigation will still be build
		siteNavigation.NavigationItems.Should().NotBeEmpty();

		var toc = siteNavigation.NavigationItems.First() as SiteTableOfContentsNavigation<IDocumentationFile>;
		toc.Should().NotBeNull();
		toc.PathPrefixProvider.PathPrefix.Should().Be("observability"); //constructed from toc URI as fallback
	}

	[Fact]
	public void ObservabilityDocumentationSetNavigationHasNoDiagnostics()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();
		var context = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability", output);

		var docsetPath = fileSystem.FileInfo.New("/checkouts/current/observability/docs/docset.yml");
		var docset = DocumentationSetFile.LoadAndResolve(context.Collector, docsetPath, fileSystem);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docset, context, GenericDocumentationFileFactory.Instance);

		// Assert navigation was created successfully
		navigation.Should().NotBeNull();
		navigation.NavigationItems.Should().NotBeEmpty();

		// Assert no diagnostic errors or warnings
		context.Collector.Errors.Should().Be(0, "there should be no errors when loading observability documentation set");
		context.Collector.Warnings.Should().Be(0, "there should be no warnings when loading observability documentation set");
	}

	[Fact]
	public void ServerlessSearchDocumentationSetNavigationHasNoDiagnostics()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();
		var context = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/serverless-search", output);

		var docsetPath = fileSystem.FileInfo.New("/checkouts/current/serverless-search/docs/docset.yml");
		var docset = DocumentationSetFile.LoadAndResolve(context.Collector, docsetPath, fileSystem);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docset, context, GenericDocumentationFileFactory.Instance);

		// Assert navigation was created successfully
		navigation.Should().NotBeNull();
		navigation.NavigationItems.Should().NotBeEmpty();

		// Assert no diagnostic errors or warnings
		context.Collector.Errors.Should().Be(0, "there should be no errors when loading serverless-search documentation set");
		context.Collector.Warnings.Should().Be(0, "there should be no warnings when loading serverless-search documentation set");
	}

	[Fact]
	public void ServerlessSecurityDocumentationSetNavigationHasNoDiagnostics()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();
		var context = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/serverless-security", output);

		var docsetPath = fileSystem.FileInfo.New("/checkouts/current/serverless-security/docs/_docset.yml");
		var docset = DocumentationSetFile.LoadAndResolve(context.Collector, docsetPath, fileSystem);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docset, context, GenericDocumentationFileFactory.Instance);

		// Assert navigation was created successfully
		navigation.Should().NotBeNull();
		navigation.NavigationItems.Should().NotBeEmpty();

		// Assert no diagnostic errors or warnings
		context.Collector.Errors.Should().Be(0, "there should be no errors when loading serverless-security documentation set");
		context.Collector.Warnings.Should().Be(0, "there should be no warnings when loading serverless-security documentation set");
	}

	[Fact]
	public void PlatformDocumentationSetNavigationHasNoDiagnostics()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();
		var context = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/platform", output);

		var docsetPath = fileSystem.FileInfo.New("/checkouts/current/platform/docs/docset.yml");
		var docset = DocumentationSetFile.LoadAndResolve(context.Collector, docsetPath, fileSystem);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docset, context, GenericDocumentationFileFactory.Instance);

		// Assert navigation was created successfully
		navigation.Should().NotBeNull();
		navigation.NavigationItems.Should().NotBeEmpty();

		// Assert no diagnostic errors or warnings
		context.Collector.Errors.Should().Be(0, "there should be no errors when loading platform documentation set");
		context.Collector.Warnings.Should().Be(0, "there should be no warnings when loading platform documentation set");
	}

	[Fact]
	public void ElasticsearchReferenceDocumentationSetNavigationHasNoDiagnostics()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();
		var context = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/elasticsearch-reference", output);

		var docsetPath = fileSystem.FileInfo.New("/checkouts/current/elasticsearch-reference/docs/docset.yml");
		var docset = DocumentationSetFile.LoadAndResolve(context.Collector, docsetPath, fileSystem);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docset, context, GenericDocumentationFileFactory.Instance);

		// Assert navigation was created successfully
		navigation.Should().NotBeNull();
		navigation.NavigationItems.Should().NotBeEmpty();

		// Assert no diagnostic errors or warnings
		context.Collector.Errors.Should().Be(0, "there should be no errors when loading elasticsearch-reference documentation set");
		context.Collector.Warnings.Should().Be(0, "there should be no warnings when loading elasticsearch-reference documentation set");
	}

	[Fact]
	public void AllDocumentationSetsHaveNoDiagnostics()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();
		var checkoutDir = fileSystem.DirectoryInfo.New("/checkouts/current");
		var repositories = checkoutDir.GetDirectories();

		repositories.Should().HaveCount(5);

		foreach (var repo in repositories)
		{
			var context = SiteNavigationTestFixture.CreateContext(fileSystem, repo.FullName, output);

			// Read the docset file
			var docsetPath = fileSystem.File.Exists($"{repo.FullName}/docs/docset.yml")
				? $"{repo.FullName}/docs/docset.yml"
				: $"{repo.FullName}/docs/_docset.yml";

			var docset = DocumentationSetFile.LoadAndResolve(context.Collector, fileSystem.FileInfo.New(docsetPath), fileSystem);

			var navigation = new DocumentationSetNavigation<IDocumentationFile>(docset, context, GenericDocumentationFileFactory.Instance);

			// Assert navigation was created successfully
			navigation.Should().NotBeNull($"navigation for {repo.Name} should be created");
			navigation.NavigationItems.Should().NotBeEmpty($"navigation for {repo.Name} should have items");

			// Assert no diagnostic errors or warnings
			context.Collector.Errors.Should().Be(0, $"there should be no errors when loading {repo.Name} documentation set");
			context.Collector.Warnings.Should().Be(0, $"there should be no warnings when loading {repo.Name} documentation set");
		}
	}

	[Fact]
	public void DocumentationSetNavigationWithNestedTocsHasNoDiagnostics()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();
		var context = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/platform", output);

		var docsetPath = fileSystem.FileInfo.New("/checkouts/current/platform/docs/docset.yml");
		var docset = DocumentationSetFile.LoadAndResolve(context.Collector, docsetPath, fileSystem);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docset, context, GenericDocumentationFileFactory.Instance);

		// Assert navigation was created successfully with nested TOCs
		navigation.Should().NotBeNull();
		navigation.NavigationItems.Should().HaveCount(3); // index.md, deployment-guide TOC, cloud-guide TOC

		var deploymentGuide = navigation.NavigationItems.ElementAt(1);
		deploymentGuide.Should().BeOfType<TableOfContentsNavigation>();

		var cloudGuide = navigation.NavigationItems.ElementAt(2);
		cloudGuide.Should().BeOfType<TableOfContentsNavigation>();

		// Assert no diagnostic errors or warnings
		context.Collector.Errors.Should().Be(0, "there should be no errors when loading platform documentation set with nested TOCs");
		context.Collector.Warnings.Should().Be(0, "there should be no warnings when loading platform documentation set with nested TOCs");
	}

	[Fact]
	public void DocumentationSetNavigationWithFoldersHasNoDiagnostics()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();
		var context = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability", output);

		var docsetPath = fileSystem.FileInfo.New("/checkouts/current/observability/docs/docset.yml");
		var docset = DocumentationSetFile.LoadAndResolve(context.Collector, docsetPath, fileSystem);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docset, context, GenericDocumentationFileFactory.Instance);

		// Assert navigation was created successfully with folders
		navigation.Should().NotBeNull();
		navigation.NavigationItems.Should().HaveCount(3); // index.md, getting-started folder, monitoring folder

		var gettingStarted = navigation.NavigationItems.ElementAt(1);
		gettingStarted.Should().BeOfType<FolderNavigation>();

		var monitoring = navigation.NavigationItems.ElementAt(2);
		monitoring.Should().BeOfType<FolderNavigation>();

		// Assert no diagnostic errors or warnings
		context.Collector.Errors.Should().Be(0, "there should be no errors when loading documentation set with folders");
		context.Collector.Warnings.Should().Be(0, "there should be no warnings when loading documentation set with folders");
	}
}
