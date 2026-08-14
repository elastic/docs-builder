// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.RelatedLearning;
using Elastic.Documentation.FileSystems;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Configuration.Tests;

public class RelatedLearningConfigurationTests
{
	[Fact]
	public void EmbeddedCatalog_LoadsFourTrainingModules()
	{
		var config = LoadActualCatalog();

		config.Links.Should().HaveCount(4);
		config.Links.Select(l => l.Id).Should().Equal(
			"apm-with-elastic",
			"elastic-agent",
			"index-basics",
			"data-types-and-mappings");
		config.Links[0].Title.Should().Be("APM with Elastic");
		config.Links[0].Url.Should().Be("https://www.elastic.co/training/apm-with-elastic");
		config.Links[0].Pages.Should().Equal("docs-content://solutions/observability/apm/index.md");
	}

	[Fact]
	public void Parse_UnqualifiedPage_Throws()
	{
		const string yaml =
			"""
			links:
			  index-basics:
			    title: Index Basics
			    url: https://www.elastic.co/training/index-basics
			    pages:
			      - manage-data/data-store/index-basics.md
			""";

		var act = () => RelatedLearningConfigurationExtensions.Parse(yaml);

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*unqualified page*manage-data/data-store/index-basics.md*");
	}

	[Fact]
	public void GetLinksForPage_MatchingDocsContentPath_ReturnsLink()
	{
		var config = LoadActualCatalog();

		var links = config.GetLinksForPage("docs-content", "manage-data/data-store/index-basics.md");

		links.Should().ContainSingle()
			.Which.Id.Should().Be("index-basics");
	}

	[Fact]
	public void GetLinksForPage_SamePathDifferentRepository_ReturnsEmpty()
	{
		var config = LoadActualCatalog();

		var links = config.GetLinksForPage("elasticsearch", "manage-data/data-store/index-basics.md");

		links.Should().BeEmpty();
	}

	[Fact]
	public void GetLinksForPage_UnmappedPath_ReturnsEmpty()
	{
		var config = LoadActualCatalog();

		var links = config.GetLinksForPage("docs-content", "getting-started/index.md");

		links.Should().BeEmpty();
	}

	[Fact]
	public void GetLinksForPage_DoesNotReturnUnrelatedMappedLinks()
	{
		var config = LoadActualCatalog();

		var links = config.GetLinksForPage("docs-content", "manage-data/data-store/index-basics.md");

		links.Should().NotContain(l => l.Id == "apm-with-elastic");
	}

	private static RelatedLearningConfiguration LoadActualCatalog()
	{
		var fileSystem = new FileSystem();
		var provider = new ConfigurationFileProvider(NullLoggerFactory.Instance, new ConfigurationFileSystem(fileSystem));
		return provider.CreateRelatedLearningConfiguration();
	}
}
