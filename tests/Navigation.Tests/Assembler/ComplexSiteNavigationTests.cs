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

		var documentationSets = new List<DocumentationSetNavigation>();

		foreach (var repo in repositories)
		{
			var context = SiteNavigationTestFixture.CreateContext(fileSystem, repo.FullName, output);

			var docsetPath = fileSystem.File.Exists($"{repo.FullName}/docs/docset.yml")
				? $"{repo.FullName}/docs/docset.yml"
				: $"{repo.FullName}/docs/_docset.yml";

			var docsetYaml = fileSystem.File.ReadAllText(docsetPath);
			var docset = DocumentationSetFile.Deserialize(docsetYaml);

			var navigation = new DocumentationSetNavigation(docset, context, TestDocumentationFileFactory.Instance);
			documentationSets.Add(navigation);
		}

		var siteContext = SiteNavigationTestFixture.CreateContext(
			fileSystem, "/checkouts/current/observability", output);

		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets);

		// Verify we have all expected top-level items
		siteNavigation.NavigationItems.Should().HaveCount(4);

		// Test 1: Observability - verify root URL has path prefix
		var observability = siteNavigation.NavigationItems.ElementAt(0);
		observability.Should().NotBeNull();
		observability.Url.Should().Be("/serverless/observability");
		observability.NavigationTitle.Should().Be("serverless-observability");

		// Test 2: Serverless Search - verify root URL has path prefix
		var search = siteNavigation.NavigationItems.ElementAt(1);
		search.Should().NotBeNull();
		search.Url.Should().Be("/serverless/search");

		// Test 3: Platform - verify root URL has path prefix
		var platform = siteNavigation.NavigationItems.ElementAt(2) as INodeNavigationItem<INavigationModel, INavigationItem>;
		platform.Should().NotBeNull();
		platform.Url.Should().Be("/platform");
		platform.NavigationItems.Should().HaveCount(2, "platform should only show the two nested TOCs as children");

		// Verify nested TOC URLs have their specified path prefixes
		var deploymentGuide = platform.NavigationItems.ElementAt(0);
		deploymentGuide.Should().NotBeNull();
		deploymentGuide.Url.Should().Be("/platform/deployment");
		deploymentGuide.NavigationTitle.Should().Be("deployment-guide");

		var cloudGuide = platform.NavigationItems.ElementAt(1);
		cloudGuide.Should().NotBeNull();
		cloudGuide.Url.Should().Be("/platform/cloud");
		cloudGuide.NavigationTitle.Should().Be("cloud-guide");

		// Test 4: Elasticsearch Reference - verify root URL has path prefix
		var elasticsearch = siteNavigation.NavigationItems.ElementAt(3) as INodeNavigationItem<INavigationModel, INavigationItem>;
		elasticsearch.Should().NotBeNull();
		elasticsearch.Url.Should().Be("/elasticsearch/reference");
		elasticsearch.NavigationItems.Should().HaveCount(3, "elasticsearch should have read its toc");

		// rest-apis is a folder (not a TOC)
		var restApis = elasticsearch.NavigationItems.ElementAt(1).Should().BeOfType<FolderNavigation>().Subject;
		restApis.Url.Should().Be("/elasticsearch/reference/rest-apis");
		restApis.NavigationItems.Should().HaveCount(3, "rest-apis folder should have 3 files");

		// Verify the file inside the folder has the correct path prefix
		var documentApisFile = restApis.NavigationItems.ElementAt(1).Should().BeOfType<FileNavigationLeaf<IDocumentationFile>>().Subject;
		documentApisFile.Url.Should().Be("/elasticsearch/reference/rest-apis/document-apis");
		documentApisFile.NavigationTitle.Should().Be("document-apis");
	}

	[Fact]
	public void DeeplyNestedNavigationMaintainsPathPrefixThroughoutHierarchy()
	{
		// language=yaml - test without specifying children for nested TOCs
		var siteNavYaml = """
		                  toc:
		                    - toc: platform://
		                      path_prefix: /docs/platform
		                  """;

		var siteNavFile = SiteNavigationFile.Deserialize(siteNavYaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		var platformContext = SiteNavigationTestFixture.CreateContext(
			fileSystem, "/checkouts/current/platform", output);
		var platformDocset = DocumentationSetFile.Deserialize(
			fileSystem.File.ReadAllText("/checkouts/current/platform/docs/docset.yml"));

		var documentationSets = new List<DocumentationSetNavigation>
		{
			new(platformDocset, platformContext, TestDocumentationFileFactory.Instance)
		};

		var siteContext = SiteNavigationTestFixture.CreateContext(
			fileSystem, "/checkouts/current/platform", output);

		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets);

		var platform = siteNavigation.NavigationItems.First() as INodeNavigationItem<INavigationModel, INavigationItem>;
		platform.Should().NotBeNull();
		platform!.Url.Should().Be("/docs/platform");

		// Platform should have its children (index, deployment-guide, cloud-guide)
		platform.NavigationItems.Should().HaveCount(3);

		// Find the deployment-guide TOC (it's the second item after index)
		var deploymentGuide = platform.NavigationItems.ElementAt(1) as INodeNavigationItem<INavigationModel, INavigationItem>;
		deploymentGuide.Should().NotBeNull();
		deploymentGuide!.Should().BeOfType<TableOfContentsNavigation>();
		deploymentGuide.Url.Should().StartWith("/docs/platform");

		// Walk through the entire tree and verify every single URL starts with path prefix
		var allUrls = CollectAllUrls(platform.NavigationItems);
		allUrls.Should().NotBeEmpty();
		allUrls.Should().OnlyContain(url => url.StartsWith("/docs/platform"),
			"all URLs in platform should start with /docs/platform");
	}

	[Fact]
	public void FileNavigationLeafUrlsReflectPathPrefixInDeeplyNestedStructures()
	{
		// language=yaml - don't specify children so we can access the actual file leaves
		var siteNavYaml = """
		                  toc:
		                    - toc: platform://
		                      path_prefix: /platform
		                  """;

		var siteNavFile = SiteNavigationFile.Deserialize(siteNavYaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		var platformContext = SiteNavigationTestFixture.CreateContext(
			fileSystem, "/checkouts/current/platform", output);
		var platformDocset = DocumentationSetFile.Deserialize(
			fileSystem.File.ReadAllText("/checkouts/current/platform/docs/docset.yml"));

		var documentationSets = new List<DocumentationSetNavigation>
		{
			new(platformDocset, platformContext, TestDocumentationFileFactory.Instance)
		};

		var siteContext = SiteNavigationTestFixture.CreateContext(
			fileSystem, "/checkouts/current/platform", output);

		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets);

		var platform = siteNavigation.NavigationItems.First() as INodeNavigationItem<INavigationModel, INavigationItem>;
		platform.Should().NotBeNull();

		// Platform should have its children including deployment-guide TOC
		platform!.NavigationItems.Should().HaveCount(3);

		// Get deployment-guide TOC (second item after index)
		var deploymentGuide = platform.NavigationItems.ElementAt(1) as INodeNavigationItem<INavigationModel, INavigationItem>;
		deploymentGuide.Should().NotBeNull();
		deploymentGuide!.Should().BeOfType<TableOfContentsNavigation>();

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
		var indexFile = fileLeaves.FirstOrDefault(f => f.NavigationTitle == "index");
		indexFile.Should().NotBeNull();
		indexFile!.Url.Should().StartWith("/platform");
	}

	[Fact]
	public void FolderNavigationWithinNestedTocsHasCorrectPathPrefix()
	{
		// language=yaml - don't specify children so we can access the actual folders
		var siteNavYaml = """
		                  toc:
		                    - toc: platform://
		                      path_prefix: /platform/cloud
		                  """;

		var siteNavFile = SiteNavigationFile.Deserialize(siteNavYaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		var platformContext = SiteNavigationTestFixture.CreateContext(
			fileSystem, "/checkouts/current/platform", output);
		var platformDocset = DocumentationSetFile.Deserialize(
			fileSystem.File.ReadAllText("/checkouts/current/platform/docs/docset.yml"));

		var documentationSets = new List<DocumentationSetNavigation>
		{
			new(platformDocset, platformContext, TestDocumentationFileFactory.Instance)
		};

		var siteContext = SiteNavigationTestFixture.CreateContext(
			fileSystem, "/checkouts/current/platform", output);

		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets);

		var platform = siteNavigation.NavigationItems.First() as INodeNavigationItem<INavigationModel, INavigationItem>;
		platform.Should().NotBeNull();

		// Platform should have its children including cloud-guide TOC
		platform!.NavigationItems.Should().HaveCount(3);

		// Get cloud-guide TOC (third item after index and deployment-guide)
		var cloudGuide = platform.NavigationItems.ElementAt(2) as INodeNavigationItem<INavigationModel, INavigationItem>;
		cloudGuide.Should().NotBeNull();
		cloudGuide!.Should().BeOfType<TableOfContentsNavigation>();

		// cloud-guide should have folders (index, aws, azure)
		var folders = cloudGuide.NavigationItems
			.OfType<FolderNavigation>()
			.ToList();

		folders.Should().NotBeEmpty("cloud-guide should contain folders");

		// Verify each folder and all its contents have correct path prefix
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
			{
				AssertAllUrlsStartWith(nodeItem.NavigationItems, expectedPrefix);
			}
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
			{
				urls.AddRange(CollectAllUrls(nodeItem.NavigationItems));
			}
		}

		return urls;
	}

	/// <summary>
	/// Helper method to collect all FileNavigationLeaf items recursively
	/// </summary>
	private static List<ILeafNavigationItem<IDocumentationFile>> CollectAllFileLeaves(IEnumerable<INavigationItem> items)
	{
		var fileLeaves = new List<ILeafNavigationItem<IDocumentationFile>>();

		foreach (var item in items)
		{
			switch (item)
			{
				case ILeafNavigationItem<IDocumentationFile> fileLeaf:
					fileLeaves.Add(fileLeaf);
					break;
				case INodeNavigationItem<INavigationModel, INavigationItem>:
					break;
			}
		}

		return fileLeaves;
	}
}
