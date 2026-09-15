// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Supplemental;

namespace Elastic.ApiExplorer.Tests.Supplemental;

public class ApiSupplementalNameTests
{
	[Theory]
	[InlineData("op-search.md", ApiSupplementalKind.Operation, "search", null)]
	[InlineData("op-getAlertingHealth.md", ApiSupplementalKind.Operation, "getAlertingHealth", null)]
	[InlineData("op-search.v8.md", ApiSupplementalKind.Operation, "search", 8)]
	[InlineData("tag-ml-anomaly.md", ApiSupplementalKind.Tag, "ml-anomaly", null)]
	[InlineData("tag-health_report.md", ApiSupplementalKind.Tag, "health_report", null)]
	[InlineData("tag-apm-agent-configuration.v9.md", ApiSupplementalKind.Tag, "apm-agent-configuration", 9)]
	public void TryParse_ConventionFile_ReturnsKindStemAndVersion(string fileName, ApiSupplementalKind kind, string stem, int? version)
	{
		ApiSupplementalName.TryParse(fileName, out var parsed).Should().BeTrue();
		parsed.Kind.Should().Be(kind);
		parsed.Stem.Should().Be(stem);
		parsed.VersionMajor.Should().Be(version);
		parsed.IsVersionSuffixed.Should().Be(version is not null);
	}

	[Theory]
	[InlineData("random-notes.md")]
	[InlineData("getting-started.md")]
	[InlineData("index.md")]
	[InlineData("op-.md")]
	[InlineData("search.md")]
	[InlineData("op-search.txt")]
	public void TryParse_NonConventionFile_ReturnsFalse(string fileName) =>
		ApiSupplementalName.TryParse(fileName, out _).Should().BeFalse();

	[Theory]
	[InlineData("APM agent configuration", "apm-agent-configuration")]
	[InlineData("health_report", "health_report")]
	[InlineData("ml anomaly", "ml-anomaly")]
	public void TagSlug_MatchesExpectedFileStem(string tagName, string expectedStem)
	{
		ApiUrlBuilder.TagSlug(tagName).Should().Be(expectedStem);
		ApiUrlBuilder.TagMoniker(tagName).Should().Be($"endpoint-{expectedStem}");
	}

	[Fact]
	public void TagSlug_EmptyName_IsUnknown()
	{
		ApiUrlBuilder.TagSlug("").Should().Be("unknown");
		ApiUrlBuilder.TagMoniker("").Should().Be("endpoint-unknown");
		ApiUrlBuilder.TagMoniker(null).Should().Be("endpoint-unknown");
	}

	[Theory]
	[InlineData("knn-guide.v9.md", "knn-guide", 9)]
	[InlineData("migration-from-v7.v8.md", "migration-from-v7", 8)]
	[InlineData("op-search.v8.md", "op-search", 8)]
	public void TryParseVersionSuffix_PeelsMajor(string fileName, string stem, int major)
	{
		ApiSupplementalName.TryParseVersionSuffix(fileName, out var parsedStem, out var parsedMajor).Should().BeTrue();
		parsedStem.Should().Be(stem);
		parsedMajor.Should().Be(major);
	}

	[Theory]
	[InlineData("getting-started.md")]
	[InlineData("knn-guide.md")]
	public void TryParseVersionSuffix_Unsuffixed_ReturnsFalse(string fileName) =>
		ApiSupplementalName.TryParseVersionSuffix(fileName, out _, out _).Should().BeFalse();
}
