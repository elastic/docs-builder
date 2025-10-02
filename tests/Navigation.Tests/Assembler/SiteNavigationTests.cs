// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Navigation.Assembler;
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

		return new TestDocumentationSetContext(fileSystem, sourceDir, outputDir, configPath, output, "docs-builder");
	}

	[Fact]
	public void ConstructorCreatesSiteNavigation()
	{
		// language=yaml
		var yaml = """
		           toc:
		             - toc: serverless/observability
		               path_prefix: /serverless/observability
		             - toc: serverless/search
		               path_prefix: /serverless/search
		           """;

		var siteNavFile = SiteNavigationFile.Deserialize(yaml);
		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs/serverless/observability");
		fileSystem.AddDirectory("/docs/serverless/search");

		var context = CreateContext(fileSystem);
		var navigation = new SiteNavigation(siteNavFile, context, []);

		navigation.Should().NotBeNull();
		navigation.Url.Should().Be("/");
		navigation.NavigationTitle.Should().Be("Site Navigation");
		navigation.NavigationItems.Should().HaveCount(2);

		var observability = navigation.NavigationItems.ElementAt(0);
		observability.Should().BeOfType<SiteTableOfContentsNavigation>();
		observability.Url.Should().Be("/serverless/observability");

		var search = navigation.NavigationItems.ElementAt(1);
		search.Should().BeOfType<SiteTableOfContentsNavigation>();
		search.Url.Should().Be("/serverless/search");
	}

	[Fact]
	public void SiteNavigationWithNestedChildren()
	{
		// language=yaml
		var yaml = """
		           toc:
		             - toc: platform
		               path_prefix: /platform
		               children:
		                 - toc: platform/deployment-guide
		                   path_prefix: /platform/deployment
		                 - toc: platform/cloud-guide
		                   path_prefix: /platform/cloud
		           """;

		var siteNavFile = SiteNavigationFile.Deserialize(yaml);
		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs/platform");
		fileSystem.AddDirectory("/docs/platform/deployment-guide");
		fileSystem.AddDirectory("/docs/platform/cloud-guide");

		var context = CreateContext(fileSystem);
		var navigation = new SiteNavigation(siteNavFile, context, []);

		navigation.NavigationItems.Should().HaveCount(1);

		var platform = navigation.NavigationItems.First();
		platform.Should().NotBeNull();
		platform.Url.Should().Be("/platform");
		platform.NavigationItems.Should().HaveCount(2);

		var deployment = platform.NavigationItems.ElementAt(0) as SiteTableOfContentsNavigation;
		deployment.Should().NotBeNull();
		deployment!.Url.Should().Be("/platform/deployment");
		deployment.PathPrefix.Should().Be("/platform/deployment");

		var cloud = platform.NavigationItems.ElementAt(1) as SiteTableOfContentsNavigation;
		cloud.Should().NotBeNull();
		cloud!.Url.Should().Be("/platform/cloud");
		cloud.PathPrefix.Should().Be("/platform/cloud");
	}

	[Fact]
	public void SiteNavigationWithoutPathPrefixUsesBaseUrl()
	{
		// language=yaml
		var yaml = """
		           toc:
		             - toc: elasticsearch/reference
		           """;

		var siteNavFile = SiteNavigationFile.Deserialize(yaml);
		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs/elasticsearch/reference");

		var context = CreateContext(fileSystem);
		var navigation = new SiteNavigation(siteNavFile, context, []);

		navigation.NavigationItems.Should().HaveCount(1);

		var elasticsearch = navigation.NavigationItems.First();
		elasticsearch.Should().NotBeNull();
		elasticsearch.PathPrefix.Should().BeEmpty();
		// Without PathPrefix, URL is constructed from base implementation (UrlRoot + ParentPath)
		elasticsearch.Url.Should().Be("/elasticsearch/reference");
	}

	[Fact]
	public void SiteNavigationPreservesCustomSchemes()
	{
		// language=yaml
		var yaml = """
		           toc:
		             - toc: elasticsearch://reference/current
		               path_prefix: /elasticsearch/reference
		             - toc: kibana://reference/8.0
		               path_prefix: /kibana/reference
		           """;

		var siteNavFile = SiteNavigationFile.Deserialize(yaml);
		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs/reference/current");
		fileSystem.AddDirectory("/docs/reference/8.0");

		var context = CreateContext(fileSystem);
		var navigation = new SiteNavigation(siteNavFile, context, []);

		navigation.NavigationItems.Should().HaveCount(2);

		var elasticsearch = navigation.NavigationItems.ElementAt(0);
		elasticsearch.Url.Should().Be("/elasticsearch/reference");
		elasticsearch.PathPrefix.Should().Be("/elasticsearch/reference");

		var kibana = navigation.NavigationItems.ElementAt(1);
		kibana.Url.Should().Be("/kibana/reference");
		kibana.PathPrefix.Should().Be("/kibana/reference");
	}

	[Fact]
	public void SiteNavigationSetsParentChildRelationships()
	{
		// language=yaml
		var yaml = """
		           toc:
		             - toc: platform
		               path_prefix: /platform
		               children:
		                 - toc: platform/deployment-guide
		                   path_prefix: /platform/deployment
		           """;

		var siteNavFile = SiteNavigationFile.Deserialize(yaml);
		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs/platform");
		fileSystem.AddDirectory("/docs/platform/deployment-guide");

		var context = CreateContext(fileSystem);
		var navigation = new SiteNavigation(siteNavFile, context, []);

		var platform = navigation.NavigationItems.First();
		var deployment = platform.NavigationItems.First();

		// Check parent-child relationships
		deployment.Parent.Should().BeSameAs(platform);
		platform.Parent.Should().BeNull();

		// Check navigation root - each SiteTableOfContentsNavigation is its own root
		deployment.NavigationRoot.Should().BeSameAs(deployment);
		platform.NavigationRoot.Should().BeSameAs(platform);
	}

	[Fact]
	public void SiteNavigationSetsCorrectDepth()
	{
		// language=yaml
		var yaml = """
		           toc:
		             - toc: level1
		               path_prefix: /level1
		               children:
		                 - toc: level2
		                   path_prefix: /level1/level2
		                   children:
		                     - toc: level3
		                       path_prefix: /level1/level2/level3
		           """;

		var siteNavFile = SiteNavigationFile.Deserialize(yaml);
		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs/level1");
		fileSystem.AddDirectory("/docs/level2");
		fileSystem.AddDirectory("/docs/level3");

		var context = CreateContext(fileSystem);
		var navigation = new SiteNavigation(siteNavFile, context, []);

		navigation.Depth.Should().Be(0);

		var level1 = navigation.NavigationItems.First();
		level1.Depth.Should().Be(1);

		var level2 = level1.NavigationItems.First() as SiteTableOfContentsNavigation;
		level2!.Depth.Should().Be(2);

		var level3 = level2.NavigationItems.First() as SiteTableOfContentsNavigation;
		level3!.Depth.Should().Be(3);
	}

	[Fact]
	public void SiteNavigationWithMixedPathPrefixes()
	{
		// language=yaml
		var yaml = """
		           toc:
		             - toc: product-a
		               path_prefix: /products/a
		               children:
		                 - toc: product-a/guide
		                   path_prefix: /products/a/guide
		                 - toc: product-a/reference
		                   # No path prefix - should use base URL
		           """;

		var siteNavFile = SiteNavigationFile.Deserialize(yaml);
		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs/product-a");
		fileSystem.AddDirectory("/docs/product-a/guide");
		fileSystem.AddDirectory("/docs/product-a/reference");

		var context = CreateContext(fileSystem);
		var navigation = new SiteNavigation(siteNavFile, context, []);

		var productA = navigation.NavigationItems.First();
		productA.Url.Should().Be("/products/a");

		var guide = productA.NavigationItems.ElementAt(0) as SiteTableOfContentsNavigation;
		guide!.Url.Should().Be("/products/a/guide");
		guide.PathPrefix.Should().Be("/products/a/guide");

		var reference = productA.NavigationItems.ElementAt(1) as SiteTableOfContentsNavigation;
		// Without path prefix, uses base URL implementation
		reference!.PathPrefix.Should().BeEmpty();
		reference.Url.Should().Be("/products/a/product-a/reference");
	}
}
