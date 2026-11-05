// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Documentation.Navigation.Isolated;
using Elastic.Documentation.Navigation.Isolated.Leaf;
using Elastic.Documentation.Navigation.Isolated.Node;

namespace Elastic.Documentation.Navigation.Tests.Assembler;

public class SiteDocumentationSetsTests()
{
	[Test]
	public void CreatesDocumentationSetNavigationsFromCheckoutFolders()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		// Discover all repositories in /checkouts/current
		var checkoutDir = fileSystem.DirectoryInfo.New("/checkouts/current");
		var repositories = checkoutDir.GetDirectories();

		repositories.Should().HaveCount(5);
		repositories
			.Select(r => r.Name)
			.Should()
			.Contain(["observability", "serverless-search", "serverless-security", "platform", "elasticsearch-reference"]);
		var collector = new TestDiagnosticsCollector();
		_ = collector.StartAsync(TestContext.Current!.Execution.CancellationToken);

		// Create DocumentationSetNavigation for each repository
		var documentationSets = new List<DocumentationSetNavigation<IDocumentationFile>>();

		foreach (var repo in repositories)
		{
			var context = SiteNavigationTestFixture.CreateContext(fileSystem, repo.FullName, collector);

			// Read the docset file
			var docsetPath = fileSystem.File.Exists($"{repo.FullName}/docs/docset.yml")
				? $"{repo.FullName}/docs/docset.yml"
				: $"{repo.FullName}/docs/_docset.yml";

			var docset = DocumentationSetFile.LoadAndResolve(
				context.Collector,
				fileSystem.FileInfo.New(docsetPath),
				new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
			);

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

	[Test]
	public void SiteNavigationIntegratesWithDocumentationSets()
	{
		// language=yaml
		var siteNavYaml =
			"""
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

		var observabilityContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability");
		var observabilityDocset = DocumentationSetFile.LoadAndResolve(
			observabilityContext.Collector,
			fileSystem.FileInfo.New("/checkouts/current/observability/docs/docset.yml"),
			new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
		);
		documentationSets.Add(
			new DocumentationSetNavigation<IDocumentationFile>(
				observabilityDocset,
				observabilityContext,
				GenericDocumentationFileFactory.Instance
			)
		);

		var searchContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/serverless-search");
		var searchDocset = DocumentationSetFile.LoadAndResolve(
			searchContext.Collector,
			fileSystem.FileInfo.New("/checkouts/current/serverless-search/docs/docset.yml"),
			new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
		);
		documentationSets.Add(
			new DocumentationSetNavigation<IDocumentationFile>(searchDocset, searchContext, GenericDocumentationFileFactory.Instance)
		);

		var securityContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/serverless-security");
		var securityDocset = DocumentationSetFile.LoadAndResolve(
			securityContext.Collector,
			fileSystem.FileInfo.New("/checkouts/current/serverless-security/docs/_docset.yml"),
			new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
		);
		documentationSets.Add(
			new DocumentationSetNavigation<IDocumentationFile>(securityDocset, securityContext, GenericDocumentationFileFactory.Instance)
		);

		// Create site navigation context (using any repository's filesystem)
		var siteContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability");

		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets, sitePrefix: null);

		NavigationTreeDump
			.ToText(siteNavigation)
			.Should()
			.MatchInlineSnapshot(
				"""
			SiteNavigation  /  "Docs"
			  index: FileNavigationLeaf  /serverless/observability  "Serverless Observability"
			  DocumentationSetNavigation  /serverless/observability  "Serverless Observability" [island]
			    index: FileNavigationLeaf  /serverless/observability  "Serverless Observability"
			    FolderNavigation  /serverless/observability/getting-started/quick-start  "Quick Start"
			      index: FileNavigationLeaf  /serverless/observability/getting-started/quick-start  "Quick Start"
			      FileNavigationLeaf  /serverless/observability/getting-started/installation  "Installation"
			    FolderNavigation  /serverless/observability/monitoring  "Monitoring"
			      index: FileNavigationLeaf  /serverless/observability/monitoring  "Monitoring"
			      FileNavigationLeaf  /serverless/observability/monitoring/logs  "Logs"
			      FileNavigationLeaf  /serverless/observability/monitoring/metrics  "Metrics"
			      FileNavigationLeaf  /serverless/observability/monitoring/traces  "Traces"
			  DocumentationSetNavigation  /serverless/search  "Serverless Search" [island]
			    index: FileNavigationLeaf  /serverless/search  "Serverless Search"
			    FolderNavigation  /serverless/search/indexing  "Indexing"
			      index: FileNavigationLeaf  /serverless/search/indexing  "Indexing"
			      FileNavigationLeaf  /serverless/search/indexing/documents  "Documents"
			      FileNavigationLeaf  /serverless/search/indexing/bulk-api  "Bulk API"
			    FolderNavigation  /serverless/search/searching  "Searching"
			      index: FileNavigationLeaf  /serverless/search/searching  "Searching"
			      FileNavigationLeaf  /serverless/search/searching/query-dsl  "Query DSL"
			  DocumentationSetNavigation  /serverless/security  "Serverless Security" [island]
			    index: FileNavigationLeaf  /serverless/security  "Serverless Security"
			    FolderNavigation  /serverless/security/authentication  "Authentication"
			      index: FileNavigationLeaf  /serverless/security/authentication  "Authentication"
			      FileNavigationLeaf  /serverless/security/authentication/api-keys  "API Keys"
			      FileNavigationLeaf  /serverless/security/authentication/oauth  "OAuth"
			    FolderNavigation  /serverless/security/authorization  "Authorization"
			      index: FileNavigationLeaf  /serverless/security/authorization  "Authorization"
			      FileNavigationLeaf  /serverless/security/authorization/rbac  "RBAC"
			"""
			);
	}

	[Test]
	public void SiteNavigationWithNestedTocs()
	{
		// language=yaml
		var siteNavYaml =
			"""
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
		var platformContext = SiteNavigationTestFixture.CreateAssemblerContext(fileSystem, "/checkouts/current/platform");
		var platformDocset = DocumentationSetFile.LoadAndResolve(
			platformContext.Collector,
			fileSystem.FileInfo.New("/checkouts/current/platform/docs/docset.yml"),
			new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
		);
		var platformNav = new DocumentationSetNavigation<IDocumentationFile>(
			platformDocset,
			platformContext,
			GenericDocumentationFileFactory.Instance
		);
		platformNav.Url.Should().Be("/");
		platformNav.Index.Url.Should().Be("/");
		platformNav.NavigationItems.ElementAt(0).Url.Should().Be("/deployment-guide");
		platformNav.NavigationItems.ElementAt(1).Url.Should().Be("/cloud-guide");

		var documentationSets = new List<IDocumentationSetNavigation> { platformNav };

		var siteContext = SiteNavigationTestFixture.CreateAssemblerContext(fileSystem, "/checkouts/current/platform");

		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets, sitePrefix: null);

		NavigationTreeDump
			.ToText(siteNavigation)
			.Should()
			.MatchInlineSnapshot(
				"""
			SiteNavigation  /  "Docs"
			  index: FileNavigationLeaf  /platform  "Platform"
			  DocumentationSetNavigation  /platform  "Platform" [island]
			    index: FileNavigationLeaf  /platform  "Platform"
			    TableOfContentsNavigation  /platform/deployment  "Deployment Guide"
			      index: FileNavigationLeaf  /platform/deployment  "Deployment Guide"
			      FolderNavigation  /platform/deployment/self-managed/installation  "Installation"
			        index: FileNavigationLeaf  /platform/deployment/self-managed/installation  "Installation"
			        FileNavigationLeaf  /platform/deployment/self-managed/configuration  "Configuration"
			    TableOfContentsNavigation  /platform/cloud  "Cloud Guide"
			      index: FileNavigationLeaf  /platform/cloud  "Cloud Guide"
			      FolderNavigation  /platform/cloud/aws/setup  "AWS Setup"
			        index: FileNavigationLeaf  /platform/cloud/aws/setup  "AWS Setup"
			      FolderNavigation  /platform/cloud/azure/setup  "Azure Setup"
			        index: FileNavigationLeaf  /platform/cloud/azure/setup  "Azure Setup"
			"""
			);
	}

	[Test]
	public void SiteNavigationWithAllRepositories()
	{
		// language=yaml
		var siteNavYaml =
			"""
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
			var context = SiteNavigationTestFixture.CreateContext(fileSystem, repo.FullName);

			var docsetPath = fileSystem.File.Exists($"{repo.FullName}/docs/docset.yml")
				? $"{repo.FullName}/docs/docset.yml"
				: $"{repo.FullName}/docs/_docset.yml";

			var docset = DocumentationSetFile.LoadAndResolve(
				context.Collector,
				fileSystem.FileInfo.New(docsetPath),
				new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
			);

			var navigation = new DocumentationSetNavigation<IDocumentationFile>(docset, context, GenericDocumentationFileFactory.Instance);
			documentationSets.Add(navigation);
		}

		var siteContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability");

		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets, sitePrefix: null);

		NavigationTreeDump
			.ToText(siteNavigation)
			.Should()
			.MatchInlineSnapshot(
				"""
			SiteNavigation  /  "Docs"
			  index: FileNavigationLeaf  /serverless/observability  "Serverless Observability"
			  DocumentationSetNavigation  /serverless/observability  "Serverless Observability" [island]
			    index: FileNavigationLeaf  /serverless/observability  "Serverless Observability"
			    FolderNavigation  /serverless/observability/getting-started/quick-start  "Quick Start"
			      index: FileNavigationLeaf  /serverless/observability/getting-started/quick-start  "Quick Start"
			      FileNavigationLeaf  /serverless/observability/getting-started/installation  "Installation"
			    FolderNavigation  /serverless/observability/monitoring  "Monitoring"
			      index: FileNavigationLeaf  /serverless/observability/monitoring  "Monitoring"
			      FileNavigationLeaf  /serverless/observability/monitoring/logs  "Logs"
			      FileNavigationLeaf  /serverless/observability/monitoring/metrics  "Metrics"
			      FileNavigationLeaf  /serverless/observability/monitoring/traces  "Traces"
			  DocumentationSetNavigation  /serverless/search  "Serverless Search" [island]
			    index: FileNavigationLeaf  /serverless/search  "Serverless Search"
			    FolderNavigation  /serverless/search/indexing  "Indexing"
			      index: FileNavigationLeaf  /serverless/search/indexing  "Indexing"
			      FileNavigationLeaf  /serverless/search/indexing/documents  "Documents"
			      FileNavigationLeaf  /serverless/search/indexing/bulk-api  "Bulk API"
			    FolderNavigation  /serverless/search/searching  "Searching"
			      index: FileNavigationLeaf  /serverless/search/searching  "Searching"
			      FileNavigationLeaf  /serverless/search/searching/query-dsl  "Query DSL"
			  DocumentationSetNavigation  /serverless/security  "Serverless Security" [island]
			    index: FileNavigationLeaf  /serverless/security  "Serverless Security"
			    FolderNavigation  /serverless/security/authentication  "Authentication"
			      index: FileNavigationLeaf  /serverless/security/authentication  "Authentication"
			      FileNavigationLeaf  /serverless/security/authentication/api-keys  "API Keys"
			      FileNavigationLeaf  /serverless/security/authentication/oauth  "OAuth"
			    FolderNavigation  /serverless/security/authorization  "Authorization"
			      index: FileNavigationLeaf  /serverless/security/authorization  "Authorization"
			      FileNavigationLeaf  /serverless/security/authorization/rbac  "RBAC"
			  DocumentationSetNavigation  /platform  "Platform" [island]
			    index: FileNavigationLeaf  /platform  "Platform"
			    TableOfContentsNavigation  /platform  "Deployment Guide"
			      index: FileNavigationLeaf  /platform  "Deployment Guide"
			      FolderNavigation  /platform/self-managed/installation  "Installation"
			        index: FileNavigationLeaf  /platform/self-managed/installation  "Installation"
			        FileNavigationLeaf  /platform/self-managed/configuration  "Configuration"
			    TableOfContentsNavigation  /platform  "Cloud Guide"
			      index: FileNavigationLeaf  /platform  "Cloud Guide"
			      FolderNavigation  /platform/aws/setup  "AWS Setup"
			        index: FileNavigationLeaf  /platform/aws/setup  "AWS Setup"
			      FolderNavigation  /platform/azure/setup  "Azure Setup"
			        index: FileNavigationLeaf  /platform/azure/setup  "Azure Setup"
			  DocumentationSetNavigation  /elasticsearch/reference  "Elasticsearch Reference" [island]
			    index: FileNavigationLeaf  /elasticsearch/reference  "Elasticsearch Reference"
			    FolderNavigation  /elasticsearch/reference/rest-apis  "REST APIs"
			      index: FileNavigationLeaf  /elasticsearch/reference/rest-apis  "REST APIs"
			      FileNavigationLeaf  /elasticsearch/reference/rest-apis/document-apis  "Document APIs"
			      FileNavigationLeaf  /elasticsearch/reference/rest-apis/search-apis  "Search APIs"
			    FolderNavigation  /elasticsearch/reference/query-dsl  "Query DSL"
			      index: FileNavigationLeaf  /elasticsearch/reference/query-dsl  "Query DSL"
			      FileNavigationLeaf  /elasticsearch/reference/query-dsl/term-queries  "Term Queries"
			      FileNavigationLeaf  /elasticsearch/reference/query-dsl/full-text-queries  "Full Text Queries"
			"""
			);
	}

	[Test]
	public void DocumentationSetNavigationHasCorrectStructure()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		// Test observability repository structure
		var observabilityContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability");
		var observabilityDocset = DocumentationSetFile.LoadAndResolve(
			observabilityContext.Collector,
			fileSystem.FileInfo.New("/checkouts/current/observability/docs/docset.yml"),
			new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
		);
		var observabilityNav = new DocumentationSetNavigation<IDocumentationFile>(
			observabilityDocset,
			observabilityContext,
			GenericDocumentationFileFactory.Instance
		);

		NavigationTreeDump
			.ToText(observabilityNav)
			.Should()
			.MatchInlineSnapshot(
				"""
			DocumentationSetNavigation  /  "Serverless Observability"
			  index: FileNavigationLeaf  /  "Serverless Observability"
			  FolderNavigation  /getting-started/quick-start  "Quick Start"
			    index: FileNavigationLeaf  /getting-started/quick-start  "Quick Start"
			    FileNavigationLeaf  /getting-started/installation  "Installation"
			  FolderNavigation  /monitoring  "Monitoring"
			    index: FileNavigationLeaf  /monitoring  "Monitoring"
			    FileNavigationLeaf  /monitoring/logs  "Logs"
			    FileNavigationLeaf  /monitoring/metrics  "Metrics"
			    FileNavigationLeaf  /monitoring/traces  "Traces"
			"""
			);
	}

	[Test]
	public void DocumentationSetWithNestedTocs()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		// Test platform repository with nested TOCs
		var platformContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/platform");
		var platformDocset = DocumentationSetFile.LoadAndResolve(
			platformContext.Collector,
			fileSystem.FileInfo.New("/checkouts/current/platform/docs/docset.yml"),
			new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
		);
		var platformNav = new DocumentationSetNavigation<IDocumentationFile>(
			platformDocset,
			platformContext,
			GenericDocumentationFileFactory.Instance
		);

		NavigationTreeDump
			.ToText(platformNav)
			.Should()
			.MatchInlineSnapshot(
				"""
			DocumentationSetNavigation  /  "Platform"
			  index: FileNavigationLeaf  /  "Platform"
			  TableOfContentsNavigation  /deployment-guide  "Deployment Guide"
			    index: FileNavigationLeaf  /deployment-guide  "Deployment Guide"
			    FolderNavigation  /deployment-guide/self-managed/installation  "Installation"
			      index: FileNavigationLeaf  /deployment-guide/self-managed/installation  "Installation"
			      FileNavigationLeaf  /deployment-guide/self-managed/configuration  "Configuration"
			  TableOfContentsNavigation  /cloud-guide  "Cloud Guide"
			    index: FileNavigationLeaf  /cloud-guide  "Cloud Guide"
			    FolderNavigation  /cloud-guide/aws/setup  "AWS Setup"
			      index: FileNavigationLeaf  /cloud-guide/aws/setup  "AWS Setup"
			    FolderNavigation  /cloud-guide/azure/setup  "Azure Setup"
			      index: FileNavigationLeaf  /cloud-guide/azure/setup  "Azure Setup"
			"""
			);
	}

	[Test]
	public void DocumentationSetWithUnderscoreDocset()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		// Test serverless-security repository with _docset.yml
		var securityContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/serverless-security");
		var securityDocset = DocumentationSetFile.LoadAndResolve(
			securityContext.Collector,
			fileSystem.FileInfo.New("/checkouts/current/serverless-security/docs/_docset.yml"),
			new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
		);
		var securityNav = new DocumentationSetNavigation<IDocumentationFile>(
			securityDocset,
			securityContext,
			GenericDocumentationFileFactory.Instance
		);

		NavigationTreeDump
			.ToText(securityNav)
			.Should()
			.MatchInlineSnapshot(
				"""
			DocumentationSetNavigation  /  "Serverless Security"
			  index: FileNavigationLeaf  /  "Serverless Security"
			  FolderNavigation  /authentication  "Authentication"
			    index: FileNavigationLeaf  /authentication  "Authentication"
			    FileNavigationLeaf  /authentication/api-keys  "API Keys"
			    FileNavigationLeaf  /authentication/oauth  "OAuth"
			  FolderNavigation  /authorization  "Authorization"
			    index: FileNavigationLeaf  /authorization  "Authorization"
			    FileNavigationLeaf  /authorization/rbac  "RBAC"
			"""
			);
	}

	[Test]
	public void SiteNavigationAppliesPathPrefixToAllUrls()
	{
		// language=yaml
		var siteNavYaml =
			"""
		                  toc:
		                    - toc: observability://
		                      path_prefix: /serverless/observability
		                  """;

		var siteNavFile = SiteNavigationFile.Deserialize(siteNavYaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		var observabilityContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability");
		var observabilityDocset = DocumentationSetFile.LoadAndResolve(
			observabilityContext.Collector,
			fileSystem.FileInfo.New("/checkouts/current/observability/docs/docset.yml"),
			new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
		);
		var documentationSets = new List<IDocumentationSetNavigation>
		{
			new DocumentationSetNavigation<IDocumentationFile>(
				observabilityDocset,
				observabilityContext,
				GenericDocumentationFileFactory.Instance
			)
		};

		var siteContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability");
		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets, sitePrefix: null);

		// Verify root URL has path prefix
		NavigationTreeDump
			.ToText(siteNavigation)
			.Should()
			.MatchInlineSnapshot(
				"""
			SiteNavigation  /  "Docs"
			  index: FileNavigationLeaf  /serverless/observability  "Serverless Observability"
			  DocumentationSetNavigation  /serverless/observability  "Serverless Observability" [island]
			    index: FileNavigationLeaf  /serverless/observability  "Serverless Observability"
			    FolderNavigation  /serverless/observability/getting-started/quick-start  "Quick Start"
			      index: FileNavigationLeaf  /serverless/observability/getting-started/quick-start  "Quick Start"
			      FileNavigationLeaf  /serverless/observability/getting-started/installation  "Installation"
			    FolderNavigation  /serverless/observability/monitoring  "Monitoring"
			      index: FileNavigationLeaf  /serverless/observability/monitoring  "Monitoring"
			      FileNavigationLeaf  /serverless/observability/monitoring/logs  "Logs"
			      FileNavigationLeaf  /serverless/observability/monitoring/metrics  "Metrics"
			      FileNavigationLeaf  /serverless/observability/monitoring/traces  "Traces"
			"""
			);
	}

	[Test]
	public void SiteNavigationWithNestedTocsAppliesCorrectPathPrefixes()
	{
		// language=yaml
		var siteNavYaml =
			"""
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

		var platformContext = SiteNavigationTestFixture.CreateAssemblerContext(fileSystem, "/checkouts/current/platform");
		var platformDocset = DocumentationSetFile.LoadAndResolve(
			platformContext.Collector,
			fileSystem.FileInfo.New("/checkouts/current/platform/docs/docset.yml"),
			new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
		);
		var documentationSets = new List<IDocumentationSetNavigation>
		{
			new DocumentationSetNavigation<IDocumentationFile>(platformDocset, platformContext, GenericDocumentationFileFactory.Instance)
		};

		var siteContext = SiteNavigationTestFixture.CreateAssemblerContext(fileSystem, "/checkouts/current/platform");
		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets, sitePrefix: null);

		NavigationTreeDump
			.ToText(siteNavigation)
			.Should()
			.MatchInlineSnapshot(
				"""
			SiteNavigation  /  "Docs"
			  index: FileNavigationLeaf  /platform  "Platform"
			  DocumentationSetNavigation  /platform  "Platform" [island]
			    index: FileNavigationLeaf  /platform  "Platform"
			    TableOfContentsNavigation  /platform/deployment  "Deployment Guide"
			      index: FileNavigationLeaf  /platform/deployment  "Deployment Guide"
			      FolderNavigation  /platform/deployment/self-managed/installation  "Installation"
			        index: FileNavigationLeaf  /platform/deployment/self-managed/installation  "Installation"
			        FileNavigationLeaf  /platform/deployment/self-managed/configuration  "Configuration"
			    TableOfContentsNavigation  /platform/cloud  "Cloud Guide"
			      index: FileNavigationLeaf  /platform/cloud  "Cloud Guide"
			      FolderNavigation  /platform/cloud/aws/setup  "AWS Setup"
			        index: FileNavigationLeaf  /platform/cloud/aws/setup  "AWS Setup"
			      FolderNavigation  /platform/cloud/azure/setup  "Azure Setup"
			        index: FileNavigationLeaf  /platform/cloud/azure/setup  "Azure Setup"
			"""
			);
	}

	[Test]
	public void SiteNavigationRequiresPathPrefix()
	{
		// language=yaml - missing path_prefix
		var siteNavYaml = """
		                  toc:
		                    - toc: observability://
		                  """;

		var siteNavFile = SiteNavigationFile.Deserialize(siteNavYaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		var observabilityContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability");
		var observabilityDocset = DocumentationSetFile.LoadAndResolve(
			observabilityContext.Collector,
			fileSystem.FileInfo.New("/checkouts/current/observability/docs/docset.yml"),
			new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
		);
		var documentationSets = new List<IDocumentationSetNavigation>
		{
			new DocumentationSetNavigation<IDocumentationFile>(
				observabilityDocset,
				observabilityContext,
				GenericDocumentationFileFactory.Instance
			)
		};

		var siteContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability");
		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets, sitePrefix: null);

		// navigation will still be build
		siteNavigation.NavigationItems.Should().NotBeEmpty();

		var toc = siteNavigation.NavigationItems.First() as DocumentationSetNavigation<IDocumentationFile>;
		toc.Should().NotBeNull();
		toc.HomeProvider.PathPrefix.Should().Be("/bad-mapping-observability");
		// toc has no `path_prefix` so it will use a default ugly one to avoid clashes and emit an error
		toc.Url.Should().Be("/bad-mapping-observability");
	}

	[Test]
	public void ObservabilityDocumentationSetNavigationHasNoDiagnostics()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();
		var context = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability");

		var docsetPath = fileSystem.FileInfo.New("/checkouts/current/observability/docs/docset.yml");
		var docset = DocumentationSetFile.LoadAndResolve(
			context.Collector,
			docsetPath,
			new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
		);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docset, context, GenericDocumentationFileFactory.Instance);

		// Assert navigation was created successfully
		navigation.Should().NotBeNull();
		navigation.NavigationItems.Should().NotBeEmpty();

		// Assert no diagnostic errors or warnings
		context.Collector.Errors.Should().Be(0, "there should be no errors when loading observability documentation set");
		context.Collector.Warnings.Should().Be(0, "there should be no warnings when loading observability documentation set");
	}

	[Test]
	public void ServerlessSearchDocumentationSetNavigationHasNoDiagnostics()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();
		var context = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/serverless-search");

		var docsetPath = fileSystem.FileInfo.New("/checkouts/current/serverless-search/docs/docset.yml");
		var docset = DocumentationSetFile.LoadAndResolve(
			context.Collector,
			docsetPath,
			new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
		);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docset, context, GenericDocumentationFileFactory.Instance);

		// Assert navigation was created successfully
		navigation.Should().NotBeNull();
		navigation.NavigationItems.Should().NotBeEmpty();

		// Assert no diagnostic errors or warnings
		context.Collector.Errors.Should().Be(0, "there should be no errors when loading serverless-search documentation set");
		context.Collector.Warnings.Should().Be(0, "there should be no warnings when loading serverless-search documentation set");
	}

	[Test]
	public void ServerlessSecurityDocumentationSetNavigationHasNoDiagnostics()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();
		var context = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/serverless-security");

		var docsetPath = fileSystem.FileInfo.New("/checkouts/current/serverless-security/docs/_docset.yml");
		var docset = DocumentationSetFile.LoadAndResolve(
			context.Collector,
			docsetPath,
			new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
		);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docset, context, GenericDocumentationFileFactory.Instance);

		// Assert navigation was created successfully
		navigation.Should().NotBeNull();
		navigation.NavigationItems.Should().NotBeEmpty();

		// Assert no diagnostic errors or warnings
		context.Collector.Errors.Should().Be(0, "there should be no errors when loading serverless-security documentation set");
		context.Collector.Warnings.Should().Be(0, "there should be no warnings when loading serverless-security documentation set");
	}

	[Test]
	public void PlatformDocumentationSetNavigationHasNoDiagnostics()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();
		var context = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/platform");

		var docsetPath = fileSystem.FileInfo.New("/checkouts/current/platform/docs/docset.yml");
		var docset = DocumentationSetFile.LoadAndResolve(
			context.Collector,
			docsetPath,
			new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
		);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docset, context, GenericDocumentationFileFactory.Instance);

		// Assert navigation was created successfully
		navigation.Should().NotBeNull();
		navigation.NavigationItems.Should().NotBeEmpty();

		// Assert no diagnostic errors or warnings
		context.Collector.Errors.Should().Be(0, "there should be no errors when loading platform documentation set");
		context.Collector.Warnings.Should().Be(0, "there should be no warnings when loading platform documentation set");
	}

	[Test]
	public void ElasticsearchReferenceDocumentationSetNavigationHasNoDiagnostics()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();
		var context = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/elasticsearch-reference");

		var docsetPath = fileSystem.FileInfo.New("/checkouts/current/elasticsearch-reference/docs/docset.yml");
		var docset = DocumentationSetFile.LoadAndResolve(
			context.Collector,
			docsetPath,
			new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
		);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docset, context, GenericDocumentationFileFactory.Instance);

		// Assert navigation was created successfully
		navigation.Should().NotBeNull();
		navigation.NavigationItems.Should().NotBeEmpty();

		// Assert no diagnostic errors or warnings
		context.Collector.Errors.Should().Be(0, "there should be no errors when loading elasticsearch-reference documentation set");
		context.Collector.Warnings.Should().Be(0, "there should be no warnings when loading elasticsearch-reference documentation set");
	}

	[Test]
	public void AllDocumentationSetsHaveNoDiagnostics()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();
		var checkoutDir = fileSystem.DirectoryInfo.New("/checkouts/current");
		var repositories = checkoutDir.GetDirectories();

		repositories.Should().HaveCount(5);

		foreach (var repo in repositories)
		{
			var context = SiteNavigationTestFixture.CreateContext(fileSystem, repo.FullName);

			// Read the docset file
			var docsetPath = fileSystem.File.Exists($"{repo.FullName}/docs/docset.yml")
				? $"{repo.FullName}/docs/docset.yml"
				: $"{repo.FullName}/docs/_docset.yml";

			var docset = DocumentationSetFile.LoadAndResolve(
				context.Collector,
				fileSystem.FileInfo.New(docsetPath),
				new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
			);

			var navigation = new DocumentationSetNavigation<IDocumentationFile>(docset, context, GenericDocumentationFileFactory.Instance);

			// Assert navigation was created successfully
			navigation.Should().NotBeNull($"navigation for {repo.Name} should be created");
			navigation.NavigationItems.Should().NotBeEmpty($"navigation for {repo.Name} should have items");

			// Assert no diagnostic errors or warnings
			context.Collector.Errors.Should().Be(0, $"there should be no errors when loading {repo.Name} documentation set");
			context.Collector.Warnings.Should().Be(0, $"there should be no warnings when loading {repo.Name} documentation set");
		}
	}

	[Test]
	public void DocumentationSetNavigationWithNestedTocsHasNoDiagnostics()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();
		var context = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/platform");

		var docsetPath = fileSystem.FileInfo.New("/checkouts/current/platform/docs/docset.yml");
		var docset = DocumentationSetFile.LoadAndResolve(
			context.Collector,
			docsetPath,
			new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
		);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docset, context, GenericDocumentationFileFactory.Instance);

		// Assert navigation was created successfully with nested TOCs
		navigation.Should().NotBeNull();
		navigation.NavigationItems.Should().HaveCount(2); // deployment-guide TOC, cloud-guide TOC (index.md is in Index property)

		var deploymentGuide = navigation.NavigationItems.ElementAt(0);
		deploymentGuide.Should().BeOfType<TableOfContentsNavigation<IDocumentationFile>>();

		var cloudGuide = navigation.NavigationItems.ElementAt(1);
		cloudGuide.Should().BeOfType<TableOfContentsNavigation<IDocumentationFile>>();

		// Assert no diagnostic errors or warnings
		context.Collector.Errors.Should().Be(0, "there should be no errors when loading platform documentation set with nested TOCs");
		context.Collector.Warnings.Should().Be(0, "there should be no warnings when loading platform documentation set with nested TOCs");
	}

	[Test]
	public void DocumentationSetNavigationWithFoldersHasNoDiagnostics()
	{
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();
		var context = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability");

		var docsetPath = fileSystem.FileInfo.New("/checkouts/current/observability/docs/docset.yml");
		var docset = DocumentationSetFile.LoadAndResolve(
			context.Collector,
			docsetPath,
			new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
		);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docset, context, GenericDocumentationFileFactory.Instance);

		// Assert navigation was created successfully with folders
		navigation.Should().NotBeNull();
		navigation.NavigationItems.Should().HaveCount(2); // index.md, getting-started folder, monitoring folder

		var gettingStarted = navigation.NavigationItems.ElementAt(0);
		gettingStarted.Should().BeOfType<FolderNavigation<IDocumentationFile>>();

		var monitoring = navigation.NavigationItems.ElementAt(1);
		monitoring.Should().BeOfType<FolderNavigation<IDocumentationFile>>();

		// Assert no diagnostic errors or warnings
		context.Collector.Errors.Should().Be(0, "there should be no errors when loading documentation set with folders");
		context.Collector.Warnings.Should().Be(0, "there should be no warnings when loading documentation set with folders");
	}
}
