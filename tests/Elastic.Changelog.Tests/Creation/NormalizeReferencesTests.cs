// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Creation;

namespace Elastic.Changelog.Tests.Creation;

/// <summary>
/// Covers <see cref="ChangelogFileWriter.NormalizeReferences"/>, which is the choke point for making
/// generated entry YAMLs self-describing. Bare PR/issue numbers must be expanded to full GitHub URLs
/// when owner+repo are supplied so downstream consumers without per-entry repo context — most
/// importantly the changelog-scrubber Lambda — can resolve the target repository.
/// </summary>
public class NormalizeReferencesTests
{
	[Fact]
	public void Null_ReturnsNull() =>
		ChangelogFileWriter.NormalizeReferences(null, "elastic", "cloud", "pull")
			.Should().BeNull();

	[Fact]
	public void Empty_ReturnsNull() =>
		ChangelogFileWriter.NormalizeReferences([], "elastic", "cloud", "pull")
			.Should().BeNull();

	[Fact]
	public void BareNumber_WithOwnerAndRepo_ExpandsToFullUrl()
	{
		var result = ChangelogFileWriter.NormalizeReferences(
			["155500"], "elastic", "cloud", "pull");

		result.Should().BeEquivalentTo(["https://github.com/elastic/cloud/pull/155500"]);
	}

	[Fact]
	public void BareIssueNumber_WithOwnerAndRepo_ExpandsToIssuesUrl()
	{
		var result = ChangelogFileWriter.NormalizeReferences(
			["4274"], "elastic", "cloud", "issues");

		result.Should().BeEquivalentTo(["https://github.com/elastic/cloud/issues/4274"]);
	}

	[Fact]
	public void BareNumber_WithoutOwner_LeftAsIs()
	{
		var result = ChangelogFileWriter.NormalizeReferences(
			["155500"], null, "cloud", "pull");

		result.Should().BeEquivalentTo(["155500"]);
	}

	[Fact]
	public void BareNumber_WithoutRepo_LeftAsIs()
	{
		var result = ChangelogFileWriter.NormalizeReferences(
			["155500"], "elastic", null, "pull");

		result.Should().BeEquivalentTo(["155500"]);
	}

	[Fact]
	public void BareNumber_WithBundleStyleRepo_LeftAsIs()
	{
		// `elasticsearch+kibana` is a multi-repo bundle string; we can't pick which one a bare
		// number targets, so we conservatively leave it alone.
		var result = ChangelogFileWriter.NormalizeReferences(
			["100"], "elastic", "elasticsearch+kibana", "pull");

		result.Should().BeEquivalentTo(["100"]);
	}

	[Fact]
	public void BareNumber_WithSlashInRepo_LeftAsIs()
	{
		// A pre-qualified `org/repo` value supplied through `--repo` shouldn't be re-combined.
		var result = ChangelogFileWriter.NormalizeReferences(
			["100"], "elastic", "elastic/cloud", "pull");

		result.Should().BeEquivalentTo(["100"]);
	}

	[Fact]
	public void FullUrl_LeftAsIs()
	{
		var result = ChangelogFileWriter.NormalizeReferences(
			["https://github.com/elastic/cloud/pull/155500"], "elastic", "cloud", "pull");

		result.Should().BeEquivalentTo(["https://github.com/elastic/cloud/pull/155500"]);
	}

	[Fact]
	public void ShortFormReference_LeftAsIs()
	{
		var result = ChangelogFileWriter.NormalizeReferences(
			["elastic/cloud#155500"], "elastic", "cloud", "pull");

		result.Should().BeEquivalentTo(["elastic/cloud#155500"]);
	}

	[Fact]
	public void MixedReferences_OnlyBareNumbersExpand()
	{
		var result = ChangelogFileWriter.NormalizeReferences(
			[
				"155500",
				"https://github.com/elastic/cloud/pull/155501",
				"elastic/cloud#155502"
			],
			"elastic", "cloud", "pull");

		result.Should().BeEquivalentTo(
		[
			"https://github.com/elastic/cloud/pull/155500",
			"https://github.com/elastic/cloud/pull/155501",
			"elastic/cloud#155502"
		]);
	}

	[Fact]
	public void NumberWithWhitespace_TrimmedAndExpanded()
	{
		var result = ChangelogFileWriter.NormalizeReferences(
			["  155500  "], "elastic", "cloud", "pull");

		result.Should().BeEquivalentTo(["https://github.com/elastic/cloud/pull/155500"]);
	}
}
