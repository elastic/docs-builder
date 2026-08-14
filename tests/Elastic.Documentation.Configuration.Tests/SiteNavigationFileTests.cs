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

		var observability = siteNav.TableOfContents.ElementAt(0).Should().BeOfType<SiteTableOfContentsRef>().Which;
		observability.Source.ToString().Should().Be("docs-content://serverless/observability");
		observability.PathPrefix.Should().Be("/serverless/observability");
		observability.Children.Should().BeEmpty();

		var search = siteNav.TableOfContents.ElementAt(1).Should().BeOfType<SiteTableOfContentsRef>().Which;
		search.Source.ToString().Should().Be("docs-content://serverless/search");
		search.PathPrefix.Should().Be("/serverless/search");

		var security = siteNav.TableOfContents.ElementAt(2).Should().BeOfType<SiteTableOfContentsRef>().Which;
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

		var platform = siteNav.TableOfContents.First().Should().BeOfType<SiteTableOfContentsRef>().Which;
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
		var ref1 = siteNav.TableOfContents.First().Should().BeOfType<SiteTableOfContentsRef>().Which;
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

		var elasticsearch = siteNav.TableOfContents.ElementAt(0).Should().BeOfType<SiteTableOfContentsRef>().Which;
		elasticsearch.Source.ToString().Should().Be("elasticsearch://reference/current");

		var kibana = siteNav.TableOfContents.ElementAt(1).Should().BeOfType<SiteTableOfContentsRef>().Which;
		kibana.Source.ToString().Should().Be("kibana://reference/8.0");

		var serverless = siteNav.TableOfContents.ElementAt(2).Should().BeOfType<SiteTableOfContentsRef>().Which;
		serverless.Source.ToString().Should().Be("docs-content://serverless/observability");
	}

	[Fact]
	public void DeserializesIslandOnTocEntry()
	{
		// language=yaml
		var yaml = """
		           toc:
		             - toc: observability://
		               path_prefix: observability
		               island: true
		             - toc: security://
		               path_prefix: security
		           """;

		var siteNav = SiteNavigationFile.Deserialize(yaml);

		siteNav.TableOfContents.Should().HaveCount(2);

		var observability = siteNav.TableOfContents.ElementAt(0).Should().BeOfType<SiteTableOfContentsRef>().Which;
		observability.Island.Should().BeTrue("island: true must be captured on the SiteTableOfContentsRef");

		var security = siteNav.TableOfContents.ElementAt(1).Should().BeOfType<SiteTableOfContentsRef>().Which;
		security.Island.Should().BeFalse("island defaults to false when omitted");
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
	public void UnknownKeyThrows()
	{
		// language=yaml
		var yaml = """
		           toc:
		             - tocc: typo
		           """;

		// A typo (no 'toc:' or 'section:' key) must throw rather than silently drop the entry.
		var act = () => SiteNavigationFile.Deserialize(yaml);

		act.Should().Throw<YamlDotNet.Core.YamlException>()
			.WithMessage("*has no 'toc:' key*");
	}

	[Fact]
	public void DeserializesSectionWithChildren()
	{
		// language=yaml
		var yaml = """
		           toc:
		             - section: Guides
		               children:
		                 - toc: get-started
		                 - toc: solutions
		             - toc: reference
		               path_prefix: reference
		           """;

		var siteNav = SiteNavigationFile.Deserialize(yaml);

		siteNav.TableOfContents.Should().HaveCount(2);

		var guides = siteNav.TableOfContents.ElementAt(0).Should().BeOfType<SiteSectionRef>().Which;
		guides.Title.Should().Be("Guides");
		guides.IsExternal.Should().BeFalse();
		guides.ExternalUrl.Should().BeNull();
		guides.Children.Should().HaveCount(2);
		guides.Children.ElementAt(0).Source.ToString().Should().Be("docs-content://get-started/");
		guides.Children.ElementAt(1).Source.ToString().Should().Be("docs-content://solutions/");

		var reference = siteNav.TableOfContents.ElementAt(1).Should().BeOfType<SiteTableOfContentsRef>().Which;
		reference.Source.ToString().Should().Be("docs-content://reference/");
	}

	[Fact]
	public void DeserializesExternalSection()
	{
		// language=yaml
		var yaml = """
		           toc:
		             - section: APIs
		               external: https://www.elastic.co/docs/api/
		           """;

		var siteNav = SiteNavigationFile.Deserialize(yaml);

		siteNav.TableOfContents.Should().HaveCount(1);

		var apis = siteNav.TableOfContents.First().Should().BeOfType<SiteSectionRef>().Which;
		apis.Title.Should().Be("APIs");
		apis.IsExternal.Should().BeTrue();
		apis.ExternalUrl.Should().Be("https://www.elastic.co/docs/api/");
		apis.Children.Should().BeEmpty();
	}

	/// <summary>
	/// The shipped <c>config/navigation.yml</c> must deserialize cleanly.
	/// </summary>
	[Fact]
	public void ShippedNavigationYmlDeserializes()
	{
		var root = Paths.GetSolutionDirectory() ?? throw new InvalidOperationException("Solution directory not found.");
		var path = Path.Combine(root.FullName, "config", "navigation.yml");
		File.Exists(path).Should().BeTrue();

		var siteNav = SiteNavigationFile.Deserialize(File.ReadAllText(path));

		siteNav.TableOfContents.Should().NotBeEmpty();
		foreach (var entry in siteNav.TableOfContents)
		{
			if (entry is SiteTableOfContentsRef tocRef)
				tocRef.Source.Should().NotBeNull();
			else if (entry is SiteSectionRef section)
				section.Title.Should().NotBeNullOrEmpty();
		}
	}
}
