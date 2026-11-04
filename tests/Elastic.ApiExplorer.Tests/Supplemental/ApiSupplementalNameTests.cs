// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Supplemental;

namespace Elastic.ApiExplorer.Tests.Supplemental;

public class ApiSupplementalNameTests
{
	[Test]
	[Arguments("op-search.md", ApiSupplementalKind.Operation, "search", null)]
	[Arguments("op-getAlertingHealth.md", ApiSupplementalKind.Operation, "getAlertingHealth", null)]
	[Arguments("op-search.v8.md", ApiSupplementalKind.Operation, "search", 8)]
	[Arguments("tag-ml-anomaly.md", ApiSupplementalKind.Tag, "ml-anomaly", null)]
	[Arguments("tag-health_report.md", ApiSupplementalKind.Tag, "health_report", null)]
	[Arguments("tag-apm-agent-configuration.v9.md", ApiSupplementalKind.Tag, "apm-agent-configuration", 9)]
	public void TryParse_ConventionFile_ReturnsKindStemAndVersion(string fileName, ApiSupplementalKind kind, string stem, int? version)
	{
		ApiSupplementalName.TryParse(fileName, out var parsed).Should().BeTrue();
		parsed.Kind.Should().Be(kind);
		parsed.Stem.Should().Be(stem);
		parsed.VersionMajor.Should().Be(version);
		parsed.IsVersionSuffixed.Should().Be(version is not null);
	}

	[Test]
	[Arguments("random-notes.md")]
	[Arguments("getting-started.md")]
	[Arguments("index.md")]
	[Arguments("op-.md")]
	[Arguments("search.md")]
	[Arguments("op-search.txt")]
	public void TryParse_NonConventionFile_ReturnsFalse(string fileName) =>
		ApiSupplementalName.TryParse(fileName, out _).Should().BeFalse();

	[Test]
	[Arguments("APM agent configuration", "apm-agent-configuration")]
	[Arguments("health_report", "health_report")]
	[Arguments("ml anomaly", "ml-anomaly")]
	public void TagSlug_MatchesExpectedFileStem(string tagName, string expectedStem)
	{
		ApiUrlBuilder.TagSlug(tagName).Should().Be(expectedStem);
		ApiUrlBuilder.TagMoniker(tagName).Should().Be($"endpoint-{expectedStem}");
	}

	[Test]
	public void TagSlug_EmptyName_IsUnknown()
	{
		ApiUrlBuilder.TagSlug("").Should().Be("unknown");
		ApiUrlBuilder.TagMoniker("").Should().Be("endpoint-unknown");
		ApiUrlBuilder.TagMoniker(null).Should().Be("endpoint-unknown");
	}

	[Test]
	[Arguments("knn-guide.v9.md", "knn-guide", 9)]
	[Arguments("migration-from-v7.v8.md", "migration-from-v7", 8)]
	[Arguments("op-search.v8.md", "op-search", 8)]
	public void TryParseVersionSuffix_PeelsMajor(string fileName, string stem, int major)
	{
		ApiSupplementalName.TryParseVersionSuffix(fileName, out var parsedStem, out var parsedMajor).Should().BeTrue();
		parsedStem.Should().Be(stem);
		parsedMajor.Should().Be(major);
	}

	[Test]
	[Arguments("getting-started.md")]
	[Arguments("knn-guide.md")]
	public void TryParseVersionSuffix_Unsuffixed_ReturnsFalse(string fileName) =>
		ApiSupplementalName.TryParseVersionSuffix(fileName, out _, out _).Should().BeFalse();
}
