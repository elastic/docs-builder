// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Assembler.Navigation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Documentation.Navigation.Isolated;
using Elastic.Documentation.Navigation.Isolated.Leaf;
using Elastic.Documentation.Navigation.Isolated.Node;
using Elastic.Documentation.Testing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Navigation.Tests.Assembler;

public class ComplexSiteNavigationTests()
{
	[Test]
	public async Task MultipleSectionsFromSameRepository_UseContentHashesAndCacheByRoot()
	{
		// language=yaml
		var siteNavYaml =
			"""
		                  toc:
		                    - toc: observability://
		                      path_prefix: /
		                    - toc: platform://deployment-guide
		                      path_prefix: /deployment
		                    - toc: platform://cloud-guide
		                      path_prefix: /cloud
		                  """;
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();
		var documentationSets = new List<IDocumentationSetNavigation>();
		foreach (var repository in new[] { "observability", "platform" })
		{
			var repositoryPath = $"/checkouts/current/{repository}";
			var context = SiteNavigationTestFixture.CreateAssemblerContext(fileSystem, repositoryPath);
			var docset = DocumentationSetFile.LoadAndResolve(
				context.Collector,
				fileSystem.FileInfo.New($"{repositoryPath}/docs/docset.yml"),
				new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
			);
			documentationSets.Add(
				new DocumentationSetNavigation<IDocumentationFile>(docset, context, GenericDocumentationFileFactory.Instance)
			);
		}

		var siteContext = SiteNavigationTestFixture.CreateAssemblerContext(fileSystem, "/checkouts/current/observability");
		var siteNavigation = new SiteNavigation(
			SiteNavigationFile.Deserialize(siteNavYaml),
			siteContext,
			documentationSets,
			sitePrefix: null
		);
		var sections = siteNavigation
			.NavigationItems
			.OfType<IRootNavigationItem<INavigationModel, INavigationItem>>()
			.Where(section => section.Identifier.Scheme == "platform")
			.ToArray();
		sections.Should().HaveCount(2);

		var writer = new GlobalNavigationHtmlWriter(NullLoggerFactory.Instance, siteNavigation, siteContext.Collector);
		var first = await writer.RenderNavigation(sections[0], sections[0].Index, TestContext.Current!.Execution.CancellationToken);
		var second = await writer.RenderNavigation(sections[1], sections[1].Index, TestContext.Current!.Execution.CancellationToken);
		var firstAgain = await writer.RenderNavigation(sections[0], sections[0].Index, TestContext.Current!.Execution.CancellationToken);

		first.Id.Should().NotBe(second.Id);
		firstAgain.Id.Should().Be(first.Id);
		firstAgain.Html.Should().Be(first.Html);
	}

	[Test]
	public void ComplexNavigationWithMultipleNestedTocsAppliesPathPrefixToRootUrls()
	{
		// language=yaml
		var siteNavYaml =
			"""
		                  toc:
		                    - toc: observability://
		                      path_prefix: /serverless/observability
		                    - toc: serverless-search://
		                      path_prefix: /serverless/search
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
			var context = SiteNavigationTestFixture.CreateAssemblerContext(fileSystem, repo.FullName);

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

		var siteContext = SiteNavigationTestFixture.CreateAssemblerContext(fileSystem, "/checkouts/current/observability");

		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets, sitePrefix: null);

		NavigationDump
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
	public void DeeplyNestedNavigationMaintainsPathPrefixThroughoutHierarchy()
	{
		// language=YAML - test without specifying children for nested TOCs
		var siteNavYaml =
			"""
		                  toc:
		                    - toc: platform://
		                      path_prefix: /docs/platform
		                      children:
		                        - toc: platform://deployment-guide
		                          path_prefix: /docs/platform/deployment-guide
		                        - toc: platform://cloud-guide
		                          path_prefix: /docs/platform/cloud-guide
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

		NavigationDump
			.ToText(siteNavigation)
			.Should()
			.MatchInlineSnapshot(
				"""
			SiteNavigation  /  "Docs"
			  index: FileNavigationLeaf  /docs/platform  "Platform"
			  DocumentationSetNavigation  /docs/platform  "Platform" [island]
			    index: FileNavigationLeaf  /docs/platform  "Platform"
			    TableOfContentsNavigation  /docs/platform/deployment-guide  "Deployment Guide"
			      index: FileNavigationLeaf  /docs/platform/deployment-guide  "Deployment Guide"
			      FolderNavigation  /docs/platform/deployment-guide/self-managed/installation  "Installation"
			        index: FileNavigationLeaf  /docs/platform/deployment-guide/self-managed/installation  "Installation"
			        FileNavigationLeaf  /docs/platform/deployment-guide/self-managed/configuration  "Configuration"
			    TableOfContentsNavigation  /docs/platform/cloud-guide  "Cloud Guide"
			      index: FileNavigationLeaf  /docs/platform/cloud-guide  "Cloud Guide"
			      FolderNavigation  /docs/platform/cloud-guide/aws/setup  "AWS Setup"
			        index: FileNavigationLeaf  /docs/platform/cloud-guide/aws/setup  "AWS Setup"
			      FolderNavigation  /docs/platform/cloud-guide/azure/setup  "Azure Setup"
			        index: FileNavigationLeaf  /docs/platform/cloud-guide/azure/setup  "Azure Setup"
			"""
			);
	}

	[Test]
	public void FileNavigationLeafUrlsReflectPathPrefixInDeeplyNestedStructures()
	{
		// language=YAML - don't specify children so we can access the actual file leaves
		var siteNavYaml =
			"""
		                  toc:
		                    - toc: platform://
		                      path_prefix: /platform
		                      children:
		                        - toc: platform://deployment-guide
		                          path_prefix: /platform/deployment-guide
		                        - toc: platform://cloud-guide
		                          path_prefix: /platform/cloud-guide
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

		NavigationDump
			.ToText(siteNavigation)
			.Should()
			.MatchInlineSnapshot(
				"""
			SiteNavigation  /  "Docs"
			  index: FileNavigationLeaf  /platform  "Platform"
			  DocumentationSetNavigation  /platform  "Platform" [island]
			    index: FileNavigationLeaf  /platform  "Platform"
			    TableOfContentsNavigation  /platform/deployment-guide  "Deployment Guide"
			      index: FileNavigationLeaf  /platform/deployment-guide  "Deployment Guide"
			      FolderNavigation  /platform/deployment-guide/self-managed/installation  "Installation"
			        index: FileNavigationLeaf  /platform/deployment-guide/self-managed/installation  "Installation"
			        FileNavigationLeaf  /platform/deployment-guide/self-managed/configuration  "Configuration"
			    TableOfContentsNavigation  /platform/cloud-guide  "Cloud Guide"
			      index: FileNavigationLeaf  /platform/cloud-guide  "Cloud Guide"
			      FolderNavigation  /platform/cloud-guide/aws/setup  "AWS Setup"
			        index: FileNavigationLeaf  /platform/cloud-guide/aws/setup  "AWS Setup"
			      FolderNavigation  /platform/cloud-guide/azure/setup  "Azure Setup"
			        index: FileNavigationLeaf  /platform/cloud-guide/azure/setup  "Azure Setup"
			"""
			);
	}

	[Test]
	public void FolderNavigationWithinNestedTocsHasCorrectPathPrefix()
	{
		// language=YAML - don't specify children so we can access the actual folders
		var siteNavYaml =
			"""
		                  toc:
		                    - toc: platform://
		                      path_prefix: /platform/cloud
		                      children:
		                        - toc: platform://deployment-guide
		                          path_prefix: /platform/deployment-guide
		                        - toc: platform://cloud-guide
		                          path_prefix: /platform/cloud-guide

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

		NavigationDump
			.ToText(siteNavigation)
			.Should()
			.MatchInlineSnapshot(
				"""
				SiteNavigation  /  "Docs"
				  index: FileNavigationLeaf  /platform/cloud  "Platform"
				  DocumentationSetNavigation  /platform/cloud  "Platform" [island]
				    index: FileNavigationLeaf  /platform/cloud  "Platform"
				    TableOfContentsNavigation  /platform/deployment-guide  "Deployment Guide"
				      index: FileNavigationLeaf  /platform/deployment-guide  "Deployment Guide"
				      FolderNavigation  /platform/deployment-guide/self-managed/installation  "Installation"
				        index: FileNavigationLeaf  /platform/deployment-guide/self-managed/installation  "Installation"
				        FileNavigationLeaf  /platform/deployment-guide/self-managed/configuration  "Configuration"
				    TableOfContentsNavigation  /platform/cloud-guide  "Cloud Guide"
				      index: FileNavigationLeaf  /platform/cloud-guide  "Cloud Guide"
				      FolderNavigation  /platform/cloud-guide/aws/setup  "AWS Setup"
				        index: FileNavigationLeaf  /platform/cloud-guide/aws/setup  "AWS Setup"
				      FolderNavigation  /platform/cloud-guide/azure/setup  "Azure Setup"
				        index: FileNavigationLeaf  /platform/cloud-guide/azure/setup  "Azure Setup"
				"""
			);
	}

	// Documents current behaviour: outside an assembler build (BuildType.Isolated) TOCs share the
	// documentation set home provider, so the nested path_prefix values are not applied.
	// See FolderNavigationWithinNestedTocsHasCorrectPathPrefix for the assembler behaviour.
	[Test]
	public void NestedTocPathPrefixesAreIgnoredInNonAssemblerContext()
	{
		// language=YAML - don't specify children so we can access the actual folders
		var siteNavYaml =
			"""
		                  toc:
		                    - toc: platform://
		                      path_prefix: /platform/cloud
		                      children:
		                        - toc: platform://deployment-guide
		                          path_prefix: /platform/deployment-guide
		                        - toc: platform://cloud-guide
		                          path_prefix: /platform/cloud-guide

		                  """;

		var siteNavFile = SiteNavigationFile.Deserialize(siteNavYaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		var platformContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/platform");
		var platformDocset = DocumentationSetFile.LoadAndResolve(
			platformContext.Collector,
			fileSystem.FileInfo.New("/checkouts/current/platform/docs/docset.yml"),
			new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
		);

		var documentationSets = new List<IDocumentationSetNavigation>
		{
			new DocumentationSetNavigation<IDocumentationFile>(platformDocset, platformContext, GenericDocumentationFileFactory.Instance)
		};

		var siteContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/platform");

		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets, sitePrefix: null);

		NavigationDump
			.ToText(siteNavigation)
			.Should()
			.MatchInlineSnapshot(
				"""
			SiteNavigation  /  "Docs"
			  index: FileNavigationLeaf  /platform/cloud  "Platform"
			  DocumentationSetNavigation  /platform/cloud  "Platform" [island]
			    index: FileNavigationLeaf  /platform/cloud  "Platform"
			    TableOfContentsNavigation  /platform/cloud  "Deployment Guide"
			      index: FileNavigationLeaf  /platform/cloud  "Deployment Guide"
			      FolderNavigation  /platform/cloud/self-managed/installation  "Installation"
			        index: FileNavigationLeaf  /platform/cloud/self-managed/installation  "Installation"
			        FileNavigationLeaf  /platform/cloud/self-managed/configuration  "Configuration"
			    TableOfContentsNavigation  /platform/cloud  "Cloud Guide"
			      index: FileNavigationLeaf  /platform/cloud  "Cloud Guide"
			      FolderNavigation  /platform/cloud/aws/setup  "AWS Setup"
			        index: FileNavigationLeaf  /platform/cloud/aws/setup  "AWS Setup"
			      FolderNavigation  /platform/cloud/azure/setup  "Azure Setup"
			        index: FileNavigationLeaf  /platform/cloud/azure/setup  "Azure Setup"
			"""
			);
	}
}
