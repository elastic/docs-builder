// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Toc;

namespace Elastic.Documentation.Configuration.Tests;

public class SiteNavigationFileTests
{
	[Test]
	public void DeserializesSiteNavigationFile()
	{
		// language=yaml
		var yaml =
			"""
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

		SiteNavDump
			.ToText(siteNav)
			.Should()
			.MatchInlineSnapshot(
				"""
			phantom  elasticsearch://reference
			phantom  docs-content://
			toc  docs-content://serverless/observability  path_prefix="/serverless/observability"
			toc  docs-content://serverless/search  path_prefix="/serverless/search"
			toc  docs-content://serverless/security  path_prefix="/serverless/security"
			"""
			);
	}

	[Test]
	public void DeserializesSiteNavigationFileWithNestedChildren()
	{
		// language=yaml
		var yaml =
			"""
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

		SiteNavDump
			.ToText(siteNav)
			.Should()
			.MatchInlineSnapshot(
				"""
			toc  docs-content://platform/  path_prefix="/platform"
			  toc  docs-content://platform/deployment-guide  path_prefix="/platform/deployment"
			  toc  docs-content://platform/cloud-guide  path_prefix="/platform/cloud"
			"""
			);
	}

	[Test]
	public void DeserializesWithMissingPath()
	{
		// language=yaml
		var yaml = """
		           toc:
		             - toc: elasticsearch/reference
		           """;

		var siteNav = SiteNavigationFile.Deserialize(yaml);

		SiteNavDump.ToText(siteNav).Should().MatchInlineSnapshot("toc  docs-content://elasticsearch/reference  path_prefix=\"\"");
	}

	[Test]
	public void PreservesSchemeWhenPresent()
	{
		// language=yaml
		var yaml =
			"""
		           toc:
		             - toc: elasticsearch://reference/current
		             - toc: kibana://reference/8.0
		             - toc: serverless/observability
		           """;

		var siteNav = SiteNavigationFile.Deserialize(yaml);

		SiteNavDump
			.ToText(siteNav)
			.Should()
			.MatchInlineSnapshot(
				"""
			toc  elasticsearch://reference/current  path_prefix=""
			toc  kibana://reference/8.0  path_prefix=""
			toc  docs-content://serverless/observability  path_prefix=""
			"""
			);
	}

	[Test]
	public void DeserializesIslandOnTocEntry()
	{
		// language=yaml
		var yaml =
			"""
		           toc:
		             - toc: observability://
		               path_prefix: observability
		               island: true
		             - toc: security://
		               path_prefix: security
		           """;

		var siteNav = SiteNavigationFile.Deserialize(yaml);

		SiteNavDump
			.ToText(siteNav)
			.Should()
			.MatchInlineSnapshot(
				"""
			toc  observability:///  path_prefix="observability" [island]
			toc  security:///  path_prefix="security"
			"""
			);
	}

	[Test]
	public void ThrowsExceptionForInvalidUri()
	{
		// language=yaml
		var yaml = """
		           toc:
		             - toc: ://invalid
		           """;

		var act = () => SiteNavigationFile.Deserialize(yaml);

		act
			.Should()
			.Throw<SharpYaml.YamlException>()
			.WithInnerException<InvalidOperationException>()
			.WithMessage("Invalid TOC source: '://invalid' could not be parsed as a URI");
	}

	[Test]
	public void UnknownKeyThrows()
	{
		// language=yaml
		var yaml = """
		           toc:
		             - tocc: typo
		           """;

		// A typo (no 'toc:' or 'section:' key) must throw rather than silently drop the entry.
		var act = () => SiteNavigationFile.Deserialize(yaml);

		act.Should().Throw<SharpYaml.YamlException>().WithMessage("*has no 'toc:' key*");
	}

	[Test]
	public void DeserializesSectionWithChildren()
	{
		// language=yaml
		var yaml =
			"""
		           toc:
		             - section: Guides
		               children:
		                 - toc: get-started
		                 - toc: solutions
		             - toc: reference
		               path_prefix: reference
		           """;

		var siteNav = SiteNavigationFile.Deserialize(yaml);

		SiteNavDump
			.ToText(siteNav)
			.Should()
			.MatchInlineSnapshot(
				"""
			section  "Guides"
			  toc  docs-content://get-started/  path_prefix=""
			  toc  docs-content://solutions/  path_prefix=""
			toc  docs-content://reference/  path_prefix="reference"
			"""
			);
	}

	[Test]
	public void DeserializesExternalSection()
	{
		// language=yaml
		var yaml =
			"""
		           toc:
		             - section: APIs
		               external: https://www.elastic.co/docs/api/
		           """;

		var siteNav = SiteNavigationFile.Deserialize(yaml);

		SiteNavDump.ToText(siteNav).Should().MatchInlineSnapshot("section  \"APIs\" external=https://www.elastic.co/docs/api/");
	}

	/// <summary>
	/// The shipped <c>config/navigation.yml</c> must deserialize cleanly.
	/// </summary>
	[Test]
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
