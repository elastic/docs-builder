// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Landing;

namespace Elastic.ApiExplorer.Tests;

public class LandingCommonMarkCatalogTests
{
	[Test]
	public void Catalog_ApiNoGroupCovers_GetsItsOwnHeadingAfterTheGroups()
	{
		var markdown = LandingCommonMark.Catalog([
			Entry("cloud", "Elastic Cloud API"),
			Entry("kibana", "Kibana APIs"),
			Entry("extra", "Extra API")
		]);

		markdown
			.Should()
			.Contain("## Elastic Stack")
			.And
			.Contain("## Orchestration")
			.And
			.Contain($"## {LandingCommonMark.OtherApisHeading}");
		var otherHeading = markdown.IndexOf($"## {LandingCommonMark.OtherApisHeading}", StringComparison.Ordinal);
		markdown.IndexOf("(`cloud`)", StringComparison.Ordinal).Should().BeLessThan(otherHeading, "Orchestration entries stay above it");
		markdown
			.IndexOf("(`extra`)", StringComparison.Ordinal)
			.Should()
			.BeGreaterThan(otherHeading, "so the extra API is not listed under Orchestration");
	}

	[Test]
	[Arguments("extra")]
	[Arguments("cloud")]
	public void Catalog_LoneGroup_HasNoSecondLevelHeading(string key)
	{
		var markdown = LandingCommonMark.Catalog([Entry(key, "Some API")]);

		markdown.Should().Contain("# Elastic APIs");
		markdown.Should().NotContain("## ");
	}

	private static ApiCatalogEntry Entry(string key, string title) => new(key, title, $"/api/doc/{key}/");
}
