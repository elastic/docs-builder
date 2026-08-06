// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Toc;

namespace Elastic.Documentation.Configuration.Tests;

public class SiteNavigationFileTests
{
	[Fact]
	public void DeserializesSiteNavigationFile()
	{
		// language=yaml
		var yaml = """
		           phantoms:
		             - toc: elasticsearch://reference
		             - toc: docs-content://
		           toc:
		             - toc: serverless/observability
		               path_prefix: /serverless/observability
		             - toc: serverless/search
		               path_prefix: /serverless/search
		             - toc: serverless/security
		               path_prefix: /serverless/security
		           """;

		var siteNav = SiteNavigationFile.Deserialize(yaml);

		siteNav.Should().NotBeNull();
		siteNav.Phantoms.Should().HaveCount(2);
		siteNav.Phantoms.ElementAt(0).Source.Should().Be("elasticsearch://reference");
		siteNav.Phantoms.ElementAt(1).Source.Should().Be("docs-content://");

		siteNav.TableOfContents.Should().HaveCount(3);

		var observability = siteNav.TableOfContents.ElementAt(0);
		observability.Source.ToString().Should().Be("docs-content://serverless/observability");
		observability.PathPrefix.Should().Be("/serverless/observability");
		observability.Children.Should().BeEmpty();

		var search = siteNav.TableOfContents.ElementAt(1);
		search.Source.ToString().Should().Be("docs-content://serverless/search");
		search.PathPrefix.Should().Be("/serverless/search");

		var security = siteNav.TableOfContents.ElementAt(2);
		security.Source.ToString().Should().Be("docs-content://serverless/security");
		security.PathPrefix.Should().Be("/serverless/security");
	}

	[Fact]
	public void DeserializesSiteNavigationFileWithNestedChildren()
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

		var siteNav = SiteNavigationFile.Deserialize(yaml);

		siteNav.TableOfContents.Should().HaveCount(1);

		var platform = siteNav.TableOfContents.First();
		platform.Source.ToString().Should().Be("docs-content://platform/");
		platform.PathPrefix.Should().Be("/platform");
		platform.Children.Should().HaveCount(2);

		var deployment = platform.Children.ElementAt(0);
		deployment.Source.ToString().Should().Be("docs-content://platform/deployment-guide");
		deployment.PathPrefix.Should().Be("/platform/deployment");

		var cloud = platform.Children.ElementAt(1);
		cloud.Source.ToString().Should().Be("docs-content://platform/cloud-guide");
		cloud.PathPrefix.Should().Be("/platform/cloud");
	}

	[Fact]
	public void DeserializesWithMissingPath()
	{
		// language=yaml
		var yaml = """
		           toc:
		             - toc: elasticsearch/reference
		           """;

		var siteNav = SiteNavigationFile.Deserialize(yaml);

		siteNav.TableOfContents.Should().HaveCount(1);
		var ref1 = siteNav.TableOfContents.First();
		ref1.Source.ToString().Should().Be("docs-content://elasticsearch/reference");
		ref1.PathPrefix.Should().BeEmpty();
	}

	[Fact]
	public void PreservesSchemeWhenPresent()
	{
		// language=yaml
		var yaml = """
		           toc:
		             - toc: elasticsearch://reference/current
		             - toc: kibana://reference/8.0
		             - toc: serverless/observability
		           """;

		var siteNav = SiteNavigationFile.Deserialize(yaml);

		siteNav.TableOfContents.Should().HaveCount(3);

		// With elasticsearch:// scheme
		var elasticsearch = siteNav.TableOfContents.ElementAt(0);
		elasticsearch.Source.ToString().Should().Be("elasticsearch://reference/current");

		// With kibana:// scheme
		var kibana = siteNav.TableOfContents.ElementAt(1);
		kibana.Source.ToString().Should().Be("kibana://reference/8.0");

		// Without scheme - should get docs-content://
		var serverless = siteNav.TableOfContents.ElementAt(2);
		serverless.Source.ToString().Should().Be("docs-content://serverless/observability");
	}

	[Fact]
	public void ThrowsExceptionForInvalidUri()
	{
		// language=yaml
		var yaml = """
		           toc:
		             - toc: ://invalid
		           """;

		var act = () => SiteNavigationFile.Deserialize(yaml);

		act.Should().Throw<YamlDotNet.Core.YamlException>()
			.WithInnerException<InvalidOperationException>()
			.WithMessage("Invalid TOC source: '://invalid' could not be parsed as a URI");
	}

	[Fact]
	public void DeserializesTopNavLinks()
	{
		// language=yaml
		var yaml = """
		           top_nav:
		             - title: Release notes
		               url: /release-notes/
		             - title: APIs
		               url: https://www.elastic.co/docs/api/
		           toc:
		             - toc: serverless/search
		               path_prefix: /serverless/search
		           """;

		var siteNav = SiteNavigationFile.Deserialize(yaml);

		siteNav.TopNav.Should().HaveCount(2);
		siteNav.TopNav[0].Title.Should().Be("Release notes");
		siteNav.TopNav[0].Url.Should().Be("/release-notes/");
		siteNav.TopNav[0].Page.Should().BeNull();
		siteNav.TopNav[0].Children.Should().BeEmpty();
		siteNav.TopNav[1].Url.Should().Be("https://www.elastic.co/docs/api/");

		// the new key must not disturb the existing ones
		siteNav.TableOfContents.Should().HaveCount(1);
	}

	[Fact]
	public void DeserializesTopNavDropdownWithGroupsAndPageReferences()
	{
		// language=yaml
		var yaml = """
		           top_nav:
		             - title: Products
		               children:
		                 - title: Stack products
		                   children:
		                     - title: Elasticsearch
		                       page: docs-content://products/elasticsearch/v9.md
		                     - title: Kibana
		                       page: docs-content://products/kibana/v9.md
		                 - title: All products
		                   url: /products/
		           """;

		var siteNav = SiteNavigationFile.Deserialize(yaml);

		siteNav.TopNav.Should().HaveCount(1);
		var products = siteNav.TopNav[0];
		products.Title.Should().Be("Products");
		products.Children.Should().HaveCount(2);

		var group = products.Children[0];
		group.Title.Should().Be("Stack products");
		group.Children.Should().HaveCount(2);
		group.Children[0].Title.Should().Be("Elasticsearch");
		group.Children[0].Page.Should().Be(new Uri("docs-content://products/elasticsearch/v9.md"));

		var ungrouped = products.Children[1];
		ungrouped.Url.Should().Be("/products/");
		ungrouped.Children.Should().BeEmpty();
	}

	[Fact]
	public void TopNavDefaultsToEmptyWhenAbsent()
	{
		// language=yaml
		var yaml = """
		           toc:
		             - toc: serverless/search
		               path_prefix: /serverless/search
		           """;

		SiteNavigationFile.Deserialize(yaml).TopNav.Should().BeEmpty();
	}

	/// <summary>
	/// The shipped <c>config/navigation.yml</c> drives the top nav on every assembled page,
	/// so a typo there breaks the whole site rather than one doc.
	/// </summary>
	[Fact]
	public void ShippedNavigationYmlHasAUsableTopNav()
	{
		var root = Paths.GetSolutionDirectory() ?? throw new InvalidOperationException("Solution directory not found.");
		var path = Path.Combine(root.FullName, "config", "navigation.yml");
		File.Exists(path).Should().BeTrue();

		var siteNav = SiteNavigationFile.Deserialize(File.ReadAllText(path));

		siteNav.TopNav.Should().NotBeEmpty();
		foreach (var item in siteNav.TopNav)
		{
			item.Title.Should().NotBeNullOrWhiteSpace();
			var hasTarget = item.Url is not null || item.Page is not null || item.Children.Count > 0;
			hasTarget.Should().BeTrue($"top_nav entry '{item.Title}' needs a url, page or children");
			(item.Url is not null && item.Page is not null).Should().BeFalse($"top_nav entry '{item.Title}' sets both url and page");
		}
	}

	[Fact]
	public void ThrowsExceptionForInvalidTopNavPageReference()
	{
		// language=yaml
		var yaml = """
		           top_nav:
		             - title: Broken
		               page: ://invalid
		           """;

		var act = () => SiteNavigationFile.Deserialize(yaml);

		act.Should().Throw<YamlDotNet.Core.YamlException>()
			.WithInnerException<InvalidOperationException>()
			.WithMessage("Invalid top_nav page reference: '://invalid' could not be parsed as a URI");
	}
}
