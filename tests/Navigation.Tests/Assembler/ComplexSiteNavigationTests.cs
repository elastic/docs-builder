// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Documentation.Navigation.Isolated;
using FluentAssertions;

namespace Elastic.Documentation.Navigation.Tests.Assembler;

public class ComplexSiteNavigationTests(ITestOutputHelper output)
{
	[Fact]
	public void ComplexNavigationWithMultipleNestedTocsAppliesPathPrefixToRootUrls()
	{
		// language=yaml
		var siteNavYaml = """
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
			var context = SiteNavigationTestFixture.CreateAssemblerContext(fileSystem, repo.FullName, output);

			var docsetPath = fileSystem.File.Exists($"{repo.FullName}/docs/docset.yml")
				? $"{repo.FullName}/docs/docset.yml"
				: $"{repo.FullName}/docs/_docset.yml";

			var docset = DocumentationSetFile.LoadAndResolve(context.Collector, fileSystem.FileInfo.New(docsetPath), fileSystem);

			var navigation = new DocumentationSetNavigation<IDocumentationFile>(docset, context, GenericDocumentationFileFactory.Instance);
			documentationSets.Add(navigation);
		}

		var siteContext = SiteNavigationTestFixture.CreateAssemblerContext(fileSystem, "/checkouts/current/observability", output);

		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets, sitePrefix: null);

		// Verify we have all expected top-level items
		siteNavigation.NavigationItems.Should().HaveCount(4);

		// Test 1: Observability - verify root URL has path prefix
		var observability = siteNavigation.NavigationItems.ElementAt(0) as INodeNavigationItem<INavigationModel, INavigationItem>;
		observability.Should().NotBeNull();
		observability.Url.Should().Be("/serverless/observability/");
		observability.NavigationTitle.Should().Be(observability.Index.NavigationTitle);

		// Test 2: Serverless Search - verify root URL has path prefix
		var search = siteNavigation.NavigationItems.ElementAt(1);
		search.Should().NotBeNull();
		search.Url.Should().Be("/serverless/search/");

		// Test 3: Platform - verify root URL has path prefix
		var platform = siteNavigation.NavigationItems.ElementAt(2) as INodeNavigationItem<INavigationModel, INavigationItem>;
		platform.Should().NotBeNull();
		platform.Url.Should().Be("/platform/");
		platform.NavigationItems.Should().HaveCount(2, "platform should only show the two nested TOCs as children");

		// Verify nested TOC URLs have their specified path prefixes
		var deploymentGuide = platform.NavigationItems.ElementAt(0) as INodeNavigationItem<INavigationModel, INavigationItem>;
		deploymentGuide.Should().NotBeNull();
		deploymentGuide.Url.Should().Be("/platform/deployment/");
		deploymentGuide.NavigationTitle.Should().Be(deploymentGuide.Index.NavigationTitle);

		var cloudGuide = platform.NavigationItems.ElementAt(1);
		cloudGuide.Should().NotBeNull();
		cloudGuide.Url.Should().Be("/platform/cloud/");
		cloudGuide.NavigationTitle.Should().Be("Cloud Guide");

		// Test 4: Elasticsearch Reference - verify root URL has path prefix
		var elasticsearch = siteNavigation.NavigationItems.ElementAt(3) as INodeNavigationItem<INavigationModel, INavigationItem>;
		elasticsearch.Should().NotBeNull();
		elasticsearch.Url.Should().Be("/elasticsearch/reference/");
		elasticsearch.NavigationItems.Should().HaveCount(2, "elasticsearch should have read its toc");

		// rest-apis is a folder (not a TOC)
		var restApis = elasticsearch.NavigationItems.ElementAt(0).Should().BeOfType<FolderNavigation<IDocumentationFile>>().Subject;
		restApis.Url.Should().Be("/elasticsearch/reference/rest-apis/");
		restApis.NavigationItems.Should().HaveCount(2, "rest-apis folder should have 2 files");

		// Verify the file inside the folder has the correct path prefix
		var documentApisFile = restApis.NavigationItems.ElementAt(0).Should().BeOfType<FileNavigationLeaf<IDocumentationFile>>().Subject;
		documentApisFile.Url.Should().Be("/elasticsearch/reference/rest-apis/document-apis/");
		documentApisFile.NavigationTitle.Should().Be("Document APIs");
	}

	[Fact]
	public void DeeplyNestedNavigationMaintainsPathPrefixThroughoutHierarchy()
	{
		// language=YAML - test without specifying children for nested TOCs
		var siteNavYaml = """
		                  toc:
		                    - toc: platform://
		                      path_prefix: /docs/platform
		                  """;

		var siteNavFile = SiteNavigationFile.Deserialize(siteNavYaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		var platformContext = SiteNavigationTestFixture.CreateAssemblerContext(fileSystem, "/checkouts/current/platform", output);
		var platformDocset = DocumentationSetFile.LoadAndResolve(platformContext.Collector,
			fileSystem.FileInfo.New("/checkouts/current/platform/docs/docset.yml"), fileSystem);

		var documentationSets = new List<IDocumentationSetNavigation>
		{
			new DocumentationSetNavigation<IDocumentationFile>(platformDocset, platformContext, GenericDocumentationFileFactory.Instance)
		};

		var siteContext = SiteNavigationTestFixture.CreateAssemblerContext(fileSystem, "/checkouts/current/platform", output);

		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets, sitePrefix: null);

		var platform = siteNavigation.NavigationItems.First() as INodeNavigationItem<INavigationModel, INavigationItem>;
		platform.Should().NotBeNull();
		platform.Url.Should().Be("/docs/platform/");

		// Platform should have its children (deployment-guide, cloud-guide)
		platform.NavigationItems.Should().HaveCount(2);

		// Find the deployment-guide TOC (it's the second item after index)
		var deploymentGuide = platform.NavigationItems.ElementAt(0) as INodeNavigationItem<INavigationModel, INavigationItem>;
		deploymentGuide.Should().NotBeNull();
		deploymentGuide.Should().BeOfType<TableOfContentsNavigation<IDocumentationFile>>();
		deploymentGuide.Url.Should().StartWith("/docs/platform/");

		// Walk through the entire tree and verify every single URL starts with a path prefix
		var allUrls = CollectAllUrls(platform.NavigationItems);
		allUrls.Should().NotBeEmpty();
		allUrls.Should().OnlyContain(url => url.StartsWith("/docs/platform/"),
			"all URLs in platform should start with /docs/platform");
	}

	[Fact]
	public void FileNavigationLeafUrlsReflectPathPrefixInDeeplyNestedStructures()
	{
		// language=YAML - don't specify children so we can access the actual file leaves
		var siteNavYaml = """
		                  toc:
		                    - toc: platform://
		                      path_prefix: /platform
		                  """;

		var siteNavFile = SiteNavigationFile.Deserialize(siteNavYaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		var platformContext = SiteNavigationTestFixture.CreateAssemblerContext(fileSystem, "/checkouts/current/platform", output);
		var platformDocset = DocumentationSetFile.LoadAndResolve(platformContext.Collector,
			fileSystem.FileInfo.New("/checkouts/current/platform/docs/docset.yml"), fileSystem);

		var documentationSets = new List<IDocumentationSetNavigation>
		{
			new DocumentationSetNavigation<IDocumentationFile>(platformDocset, platformContext, GenericDocumentationFileFactory.Instance)
		};

		var siteContext = SiteNavigationTestFixture.CreateAssemblerContext(fileSystem, "/checkouts/current/platform", output);

		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets, sitePrefix: null);

		var platform = siteNavigation.NavigationItems.First() as INodeNavigationItem<INavigationModel, INavigationItem>;
		platform.Should().NotBeNull();

		// Platform should have its children including deployment-guide TOC
		platform.NavigationItems.Should().HaveCount(2);

		// Get deployment-guide TOC
		var deploymentGuide = platform.NavigationItems.ElementAt(0) as INodeNavigationItem<INavigationModel, INavigationItem>;
		deploymentGuide.Should().NotBeNull();
		deploymentGuide.Should().BeOfType<TableOfContentsNavigation<IDocumentationFile>>();

		// Find all FileNavigationLeaf items recursively
		var fileLeaves = CollectAllFileLeaves(deploymentGuide.NavigationItems);
		fileLeaves.Should().NotBeEmpty("deployment-guide should contain file leaves");

		// Verify every single file leaf has the correct path prefix
		foreach (var fileLeaf in fileLeaves)
		{
			fileLeaf.Url.Should().StartWith("/platform",
				$"file '{fileLeaf.NavigationTitle}' should have URL starting with /platform but got '{fileLeaf.Url}'");
		}

		// Verify at least one specific file to ensure we're testing real data
		var indexFile = fileLeaves.OfType<FileNavigationLeaf<IDocumentationFile>>()
			.FirstOrDefault(f => f.FileInfo.FullName.EndsWith(".md", StringComparison.OrdinalIgnoreCase));
		indexFile.Should().NotBeNull();
		indexFile.Url.Should().StartWith("/platform");
	}

	[Fact]
	public void FolderNavigationWithinNestedTocsHasCorrectPathPrefix()
	{
		// language=YAML - don't specify children so we can access the actual folders
		var siteNavYaml = """
		                  toc:
		                    - toc: platform://
		                      path_prefix: /platform/cloud
		                  """;

		var siteNavFile = SiteNavigationFile.Deserialize(siteNavYaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		var platformContext = SiteNavigationTestFixture.CreateContext(
			fileSystem, "/checkouts/current/platform", output);
		var platformDocset = DocumentationSetFile.LoadAndResolve(platformContext.Collector,
			fileSystem.FileInfo.New("/checkouts/current/platform/docs/docset.yml"), fileSystem);

		var documentationSets = new List<IDocumentationSetNavigation>
		{
			new DocumentationSetNavigation<IDocumentationFile>(platformDocset, platformContext, GenericDocumentationFileFactory.Instance)
		};

		var siteContext = SiteNavigationTestFixture.CreateContext(
			fileSystem, "/checkouts/current/platform", output);

		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets, sitePrefix: null);

		var platform = siteNavigation.NavigationItems.First() as INodeNavigationItem<INavigationModel, INavigationItem>;
		platform.Should().NotBeNull();

		// Platform should have its children including cloud-guide TOC
		platform.NavigationItems.Should().HaveCount(2);

		// Get cloud-guide TOC (third item after deployment-guide)
		var cloudGuide = platform.NavigationItems.ElementAt(1) as INodeNavigationItem<INavigationModel, INavigationItem>;
		cloudGuide.Should().NotBeNull();
		cloudGuide.Should().BeOfType<TableOfContentsNavigation<IDocumentationFile>>();

		// cloud-guide should have folders (index, aws, azure)
		var folders = cloudGuide.NavigationItems
			.OfType<FolderNavigation<IDocumentationFile>>()
			.ToList();

		folders.Should().NotBeEmpty("cloud-guide should contain folders");

		// Verify each folder and all its contents have a correct path prefix
		foreach (var folder in folders)
		{
			folder.Url.Should().StartWith("/platform/cloud",
				$"folder '{folder.NavigationTitle}' should have URL starting with /platform/cloud");

			// Verify all items within the folder
			AssertAllUrlsStartWith(folder.NavigationItems, "/platform/cloud");

			// Verify specific file leaves within the folder
			var filesInFolder = CollectAllFileLeaves(folder.NavigationItems);
			foreach (var file in filesInFolder)
			{
				file.Url.Should().StartWith("/platform/cloud",
					$"file '{file.NavigationTitle}' in folder '{folder.NavigationTitle}' should have URL starting with /platform/cloud");
			}
		}
	}

	/// <summary>
	/// Helper method to recursively assert all URLs start with a given prefix
	/// </summary>
	private static void AssertAllUrlsStartWith(IEnumerable<INavigationItem> items, string expectedPrefix)
	{
		foreach (var item in items)
		{
			item.Url.Should().StartWith(expectedPrefix,
				$"item '{item.NavigationTitle}' should have URL starting with '{expectedPrefix}' but got '{item.Url}'");

			if (item is INodeNavigationItem<INavigationModel, INavigationItem> nodeItem)
				AssertAllUrlsStartWith(nodeItem.NavigationItems, expectedPrefix);
		}
	}

	/// <summary>
	/// Helper method to collect all URLs recursively
	/// </summary>
	private static List<string> CollectAllUrls(IEnumerable<INavigationItem> items)
	{
		var urls = new List<string>();

		foreach (var item in items)
		{
			urls.Add(item.Url);

			if (item is INodeNavigationItem<INavigationModel, INavigationItem> nodeItem)
				urls.AddRange(CollectAllUrls(nodeItem.NavigationItems));
		}

		return urls;
	}

	/// <summary>
	/// Helper method to collect all FileNavigationLeaf items recursively
	/// </summary>
	private static List<ILeafNavigationItem<INavigationModel>> CollectAllFileLeaves(IEnumerable<INavigationItem> items)
	{
		var fileLeaves = new List<ILeafNavigationItem<INavigationModel>>();

		foreach (var item in items)
		{
			switch (item)
			{
				case ILeafNavigationItem<IDocumentationFile> fileLeaf:
					fileLeaves.Add(fileLeaf);
					break;
				case INodeNavigationItem<INavigationModel, INavigationItem> node:
					fileLeaves.Add(node.Index);
					fileLeaves.AddRange(CollectAllFileLeaves(node.NavigationItems));
					break;
			}
		}

		return fileLeaves;
	}
}
