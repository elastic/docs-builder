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
	public void ComplexNavigationWithMultipleNestedTocsAppliesPathPrefixToAllUrls()
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

			var navigation = new DocumentationSetNavigation(docset, context);
			documentationSets.Add(navigation);
		}

		var siteContext = SiteNavigationTestFixture.CreateContext(
			fileSystem, "/checkouts/current/observability", output);

		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets);

		// Verify we have all expected top-level items
		siteNavigation.NavigationItems.Should().HaveCount(4);

		// Test 1: Observability (simple repository with folders)
		var observability = siteNavigation.NavigationItems.ElementAt(0) as INodeNavigationItem<INavigationModel, INavigationItem>;
		observability.Should().NotBeNull();
		observability!.Url.Should().Be("/serverless/observability");
		observability.NavigationTitle.Should().Be("serverless-observability");

		// Verify all child URLs start with the path prefix
		AssertAllUrlsStartWith(observability.NavigationItems, "/serverless/observability");

		// Verify specific file URLs - items are wrapped in SiteNavigationItemWrapper
		var observabilityIndex = observability.NavigationItems.ElementAt(0);
		observabilityIndex.Url.Should().Be("/serverless/observability");
		observabilityIndex.NavigationTitle.Should().Be("index");

		// Verify folder navigation
		var gettingStarted = observability.NavigationItems.ElementAt(1) as INodeNavigationItem<INavigationModel, INavigationItem>;
		gettingStarted.Should().NotBeNull();
		gettingStarted!.Url.Should().StartWith("/serverless/observability");
		AssertAllUrlsStartWith(gettingStarted.NavigationItems, "/serverless/observability");

		// Test 2: Serverless Search (simple repository with folders)
		var search = siteNavigation.NavigationItems.ElementAt(1) as INodeNavigationItem<INavigationModel, INavigationItem>;
		search.Should().NotBeNull();
		search!.Url.Should().Be("/serverless/search");
		AssertAllUrlsStartWith(search.NavigationItems, "/serverless/search");

		// Test 3: Platform (complex repository with nested TOCs)
		var platform = siteNavigation.NavigationItems.ElementAt(2) as INodeNavigationItem<INavigationModel, INavigationItem>;
		platform.Should().NotBeNull();
		platform!.Url.Should().Be("/platform");
		platform.NavigationItems.Should().HaveCount(2, "platform should only show the two nested TOCs as children");

		// Verify nested TOC: deployment-guide
		var deploymentGuide = platform.NavigationItems.ElementAt(0);
		deploymentGuide.Should().NotBeNull();
		deploymentGuide.Url.Should().Be("/platform/deployment");
		deploymentGuide.NavigationTitle.Should().Be("deployment-guide");

		// Note: When children are specified in site navigation, the TOC only shows those specific children
		// Since deployment-guide has no children specified, it will be wrapped with its original items
		// But we can't access them directly in this test context

		// Verify nested TOC: cloud-guide
		var cloudGuide = platform.NavigationItems.ElementAt(1);
		cloudGuide.Should().NotBeNull();
		cloudGuide.Url.Should().Be("/platform/cloud");
		cloudGuide.NavigationTitle.Should().Be("cloud-guide");

		// Test 4: Elasticsearch Reference (simple repository)
		var elasticsearch = siteNavigation.NavigationItems.ElementAt(3) as INodeNavigationItem<INavigationModel, INavigationItem>;
		elasticsearch.Should().NotBeNull();
		elasticsearch!.Url.Should().Be("/elasticsearch/reference");
		AssertAllUrlsStartWith(elasticsearch.NavigationItems, "/elasticsearch/reference");
	}

	[Fact]
	public void DeeplyNestedNavigationMaintainsPathPrefixThroughoutHierarchy()
	{
		// language=yaml
		var siteNavYaml = """
		                  toc:
		                    - toc: platform://
		                      path_prefix: /docs/platform
		                      children:
		                        - toc: platform://deployment-guide
		                          path_prefix: /docs/platform/deployment
		                        - toc: platform://cloud-guide
		                          path_prefix: /docs/platform/cloud
		                  """;

		var siteNavFile = SiteNavigationFile.Deserialize(siteNavYaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		var platformContext = SiteNavigationTestFixture.CreateContext(
			fileSystem, "/checkouts/current/platform", output);
		var platformDocset = DocumentationSetFile.Deserialize(
			fileSystem.File.ReadAllText("/checkouts/current/platform/docs/docset.yml"));

		var documentationSets = new List<DocumentationSetNavigation>
		{
			new(platformDocset, platformContext)
		};

		var siteContext = SiteNavigationTestFixture.CreateContext(
			fileSystem, "/checkouts/current/platform", output);

		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets);

		var platform = siteNavigation.NavigationItems.First() as INodeNavigationItem<INavigationModel, INavigationItem>;
		platform.Should().NotBeNull();
		platform!.Url.Should().Be("/docs/platform");

		// Verify nested TOC maintains path prefix
		var deploymentGuide = platform.NavigationItems.ElementAt(0) as INodeNavigationItem<INavigationModel, INavigationItem>;
		deploymentGuide.Should().NotBeNull();
		deploymentGuide!.Url.Should().Be("/docs/platform/deployment");

		// Walk through the entire tree and verify every single URL
		var allUrls = CollectAllUrls(deploymentGuide.NavigationItems);
		allUrls.Should().NotBeEmpty();
		allUrls.Should().OnlyContain(url => url.StartsWith("/docs/platform/deployment"),
			"all URLs in deployment-guide should start with /docs/platform/deployment");

		// Verify cloud-guide as well
		var cloudGuide = platform.NavigationItems.ElementAt(1) as INodeNavigationItem<INavigationModel, INavigationItem>;
		cloudGuide.Should().NotBeNull();
		cloudGuide!.Url.Should().Be("/docs/platform/cloud");

		var cloudUrls = CollectAllUrls(cloudGuide.NavigationItems);
		cloudUrls.Should().NotBeEmpty();
		cloudUrls.Should().OnlyContain(url => url.StartsWith("/docs/platform/cloud"),
			"all URLs in cloud-guide should start with /docs/platform/cloud");
	}

	[Fact]
	public void FileNavigationLeafUrlsReflectPathPrefixInDeeplyNestedStructures()
	{
		// language=yaml
		var siteNavYaml = """
		                  toc:
		                    - toc: platform://
		                      path_prefix: /platform
		                      children:
		                        - toc: platform://deployment-guide
		                          path_prefix: /platform/deployment
		                  """;

		var siteNavFile = SiteNavigationFile.Deserialize(siteNavYaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		var platformContext = SiteNavigationTestFixture.CreateContext(
			fileSystem, "/checkouts/current/platform", output);
		var platformDocset = DocumentationSetFile.Deserialize(
			fileSystem.File.ReadAllText("/checkouts/current/platform/docs/docset.yml"));

		var documentationSets = new List<DocumentationSetNavigation>
		{
			new(platformDocset, platformContext)
		};

		var siteContext = SiteNavigationTestFixture.CreateContext(
			fileSystem, "/checkouts/current/platform", output);

		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets);

		var platform = siteNavigation.NavigationItems.First() as INodeNavigationItem<INavigationModel, INavigationItem>;
		var deploymentGuide = platform!.NavigationItems.ElementAt(0) as INodeNavigationItem<INavigationModel, INavigationItem>;

		// Find all FileNavigationLeaf items recursively
		var fileLeaves = CollectAllFileLeaves(deploymentGuide!.NavigationItems);
		fileLeaves.Should().NotBeEmpty("deployment-guide should contain file leaves");

		// Verify every single file leaf has the correct path prefix
		foreach (var fileLeaf in fileLeaves)
		{
			fileLeaf.Url.Should().StartWith("/platform/deployment",
				$"file '{fileLeaf.NavigationTitle}' should have URL starting with /platform/deployment but got '{fileLeaf.Url}'");
		}

		// Verify at least one specific file to ensure we're testing real data
		var indexFile = fileLeaves.FirstOrDefault(f => f.NavigationTitle == "index");
		indexFile.Should().NotBeNull();
		indexFile!.Url.Should().StartWith("/platform/deployment");
	}

	[Fact]
	public void FolderNavigationWithinNestedTocsHasCorrectPathPrefix()
	{
		// language=yaml
		var siteNavYaml = """
		                  toc:
		                    - toc: platform://
		                      path_prefix: /platform
		                      children:
		                        - toc: platform://cloud-guide
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
			new(platformDocset, platformContext)
		};

		var siteContext = SiteNavigationTestFixture.CreateContext(
			fileSystem, "/checkouts/current/platform", output);

		var siteNavigation = new SiteNavigation(siteNavFile, siteContext, documentationSets);

		var platform = siteNavigation.NavigationItems.First() as INodeNavigationItem<INavigationModel, INavigationItem>;
		var cloudGuide = platform!.NavigationItems.First() as INodeNavigationItem<INavigationModel, INavigationItem>;

		// cloud-guide should have folders (aws, azure, etc.)
		var folders = cloudGuide!.NavigationItems
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
	private static List<FileNavigationLeaf> CollectAllFileLeaves(IEnumerable<INavigationItem> items)
	{
		var fileLeaves = new List<FileNavigationLeaf>();

		foreach (var item in items)
		{
			if (item is FileNavigationLeaf fileLeaf)
			{
				fileLeaves.Add(fileLeaf);
			}
			else if (item is INodeNavigationItem<INavigationModel, INavigationItem> nodeItem)
			{
				fileLeaves.AddRange(CollectAllFileLeaves(nodeItem.NavigationItems));
			}
		}

		return fileLeaves;
	}
}
