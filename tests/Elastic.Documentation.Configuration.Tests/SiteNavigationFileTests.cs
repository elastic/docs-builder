// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.DocSet;
using FluentAssertions;

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
		               path: /serverless/observability
		             - toc: serverless/search
		               path: /serverless/search
		             - toc: serverless/security
		               path: /serverless/security
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
		               path: /platform
		               children:
		                 - toc: platform/deployment-guide
		                   path: /platform/deployment
		                 - toc: platform/cloud-guide
		                   path: /platform/cloud
		           """;

		var siteNav = SiteNavigationFile.Deserialize(yaml);

		siteNav.TableOfContents.Should().HaveCount(1);

		var platform = siteNav.TableOfContents.First();
		platform.Source.ToString().Should().Be("docs-content://platform");
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

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("Invalid TOC source: '://invalid' could not be parsed as a URI");
	}
}
