// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Documentation.Configuration.Tests.ReleaseNotes;

/// <summary>
/// Unit tests for ChangelogTextUtilities.
/// These tests verify the text processing utilities for changelog generation.
/// </summary>
public class ChangelogTextUtilitiesTests
{
	[Test]
	[Arguments("hello world", "Hello world.")]
	[Arguments("Hello world", "Hello world.")]
	[Arguments("Hello world.", "Hello world.")]
	[Arguments("a", "A.")]
	[Arguments("", "")]
	[Arguments(null, "")]
	public void Beautify_CapitalizesAndAddsPeriod(string? input, string expected)
	{
		var result = ChangelogTextUtilities.Beautify(input ?? "");
		result.Should().Be(expected);
	}

	[Test]
	[Arguments("9.3.0", "9.3.0")]
	[Arguments("Version 9.3.0", "version-9.3.0")]
	[Arguments("Version 9.3.0-beta1", "version-9.3.0-beta1")]
	public void TitleToSlug_ConvertsToSlugFormat(string input, string expected)
	{
		var result = ChangelogTextUtilities.TitleToSlug(input);
		result.Should().Be(expected);
	}

	[Test]
	[Arguments("search-api", "Search api")]
	[Arguments("ingest-pipeline", "Ingest pipeline")]
	[Arguments("api", "Api")]
	public void FormatAreaHeader_CapitalizesAndReplacesHyphens(string input, string expected)
	{
		var result = ChangelogTextUtilities.FormatAreaHeader(input);
		result.Should().Be(expected);
	}

	[Test]
	[Arguments("[Inference API] Add new endpoint", "Add new endpoint")]
	[Arguments("[ES]: Fix bug", "Fix bug")]
	[Arguments("[Test] Title", "Title")]
	[Arguments("No bracket prefix", "No bracket prefix")]
	[Arguments("[Unclosed bracket", "[Unclosed bracket")]
	[Arguments("[Cases] - Enable cases numerical id service", "Enable cases numerical id service")]
	[Arguments("[Team] - Leading", "Leading")]
	[Arguments("- Leading dash without brackets", "- Leading dash without brackets")]
	[Arguments("[Team]-NoSpace", "-NoSpace")]
	public void StripSquareBracketPrefix_RemovesPrefix(string input, string expected)
	{
		var result = ChangelogTextUtilities.StripSquareBracketPrefix(input);
		result.Should().Be(expected);
	}

	[Test]
	[Arguments(null, false)]
	[Arguments("", false)]
	[Arguments("  ", false)]
	[Arguments("Plain title", false)]
	[Arguments("- Leading dash", true)]
	[Arguments("  - Leading dash", true)]
	[Arguments("* Star", true)]
	[Arguments("+ Plus", true)]
	[Arguments("\u2013 En dash", true)]
	[Arguments("\u2014 Em dash", true)]
	public void TitleNeedsDefensiveYamlQuoting_DetectsBulletLikeScalars(string? input, bool expected) =>
		ChangelogTextUtilities.TitleNeedsDefensiveYamlQuoting(input).Should().Be(expected);

	[Test]
	[Arguments("https://github.com/elastic/elasticsearch/pull/123", 123)]
	[Arguments("elastic/elasticsearch#456", 456)]
	[Arguments("123", null)] // No default owner/repo

	public void ExtractPrNumber_ExtractsNumber(string input, int? expected)
	{
		var result = ChangelogTextUtilities.ExtractPrNumber(input);
		result.Should().Be(expected);
	}

	[Test]
	public void ExtractPrNumber_WithDefaultOwnerRepo_ExtractsNumber()
	{
		var result = ChangelogTextUtilities.ExtractPrNumber("123", "elastic", "elasticsearch");
		result.Should().Be(123);
	}

	[Test]
	[Arguments("https://github.com/elastic/elasticsearch/issues/123", 123)]
	[Arguments("https://github.com/owner/repo/issues/456", 456)]
	[Arguments("elastic/elasticsearch#789", 789)]
	[Arguments("123", null)]
	public void ExtractIssueNumber_ExtractsNumber(string input, int? expected)
	{
		var result = ChangelogTextUtilities.ExtractIssueNumber(input);
		result.Should().Be(expected);
	}

	[Test]
	public void ExtractIssueNumber_WithDefaultOwnerRepo_ExtractsNumber()
	{
		var result = ChangelogTextUtilities.ExtractIssueNumber("123", "elastic", "elasticsearch");
		result.Should().Be(123);
	}

	[Test]
	[Arguments("v1.0.0", "ga")]
	[Arguments("v1.0.0-beta1", "beta")]
	[Arguments("v1.0.0-preview.1", "preview")]
	[Arguments("1.0.0-alpha1", "preview")]
	[Arguments("1.0.0-rc1", "beta")]
	[Arguments("1.0.0", "ga")]
	public void InferLifecycleFromVersion_InfersCorrectly(string tagName, string expected)
	{
		var result = ChangelogTextUtilities.InferLifecycleFromVersion(tagName);
		result.Should().Be(expected);
	}

	[Test]
	[Arguments("v1.0.0", "1.0.0")]
	[Arguments("v1.0.0-beta1", "1.0.0")]
	[Arguments("1.2.3-preview.1", "1.2.3")]
	[Arguments("9.3.0", "9.3.0")]
	public void ExtractBaseVersion_ExtractsVersion(string tagName, string expected)
	{
		var result = ChangelogTextUtilities.ExtractBaseVersion(tagName);
		result.Should().Be(expected);
	}

	[Test]
	[Arguments("elastic/elasticsearch", "elastic", "elasticsearch")]
	[Arguments("elasticsearch", null, "elasticsearch")]
	public void ParseRepository_ParsesCorrectly(string input, string? expectedOwner, string expectedRepo)
	{
		var (owner, repo) = ChangelogTextUtilities.ParseRepository(input);
		owner.Should().Be(expectedOwner);
		repo.Should().Be(expectedRepo);
	}

	[Test]
	[Arguments("Add new feature to API", "add-new-feature-to-api")]
	[Arguments("Fix bug in the search API endpoint handler", "fix-bug-in-the-search-api")] // Takes first 6 words by default

	[Arguments("", "untitled")]
	public void GenerateSlug_GeneratesSlug(string input, string expected)
	{
		var result = ChangelogTextUtilities.GenerateSlug(input);
		result.Should().Be(expected);
	}

	[Test]
	public void HasVisibleLinks_WithOnlyPrivateLinks_ReturnsFalse()
	{
		var entry = new ChangelogEntry
		{
			Prs = ["# PRIVATE: https://github.com/elastic/cloud/pull/123"],
			Issues = ["# PRIVATE: https://github.com/elastic/cloud/issues/456"]
		};

		var result = ChangelogTextUtilities.HasVisibleLinks(entry, "elasticsearch", false);

		result.Should().BeFalse();
	}

	[Test]
	public void HasVisibleLinks_WithMixedLinks_ReturnsTrue()
	{
		var entry = new ChangelogEntry
		{
			Prs = ["# PRIVATE: https://github.com/elastic/cloud/pull/123", "456"],
			Issues = ["# PRIVATE: https://github.com/elastic/cloud/issues/789"]
		};

		var result = ChangelogTextUtilities.HasVisibleLinks(entry, "elasticsearch", false);

		result.Should().BeTrue();
	}

	[Test]
	public void HasVisibleLinks_WithPublicLinks_ReturnsTrue()
	{
		var entry = new ChangelogEntry { Prs = ["123"], Issues = ["456"] };

		var result = ChangelogTextUtilities.HasVisibleLinks(entry, "elasticsearch", false);

		result.Should().BeTrue();
	}

	[Test]
	public void HasVisibleLinks_WithNoLinks_ReturnsFalse()
	{
		var entry = new ChangelogEntry { Prs = null, Issues = null };

		var result = ChangelogTextUtilities.HasVisibleLinks(entry, "elasticsearch", false);

		result.Should().BeFalse();
	}

	[Test]
	public void HasVisibleLinks_WithEmptyArrays_ReturnsFalse()
	{
		var entry = new ChangelogEntry { Prs = [], Issues = [] };

		var result = ChangelogTextUtilities.HasVisibleLinks(entry, "elasticsearch", false);

		result.Should().BeFalse();
	}

	[Test]
	public void HasVisibleLinks_WithHidePrivateLinks_ChecksCommentedFormat()
	{
		var entry = new ChangelogEntry
		{
			Prs = ["123"] // This should format as "% [#123](url)" when hidePrivateLinks=true
		};

		// When hidePrivateLinks is true, links are commented out but still "visible" (non-empty)
		var result = ChangelogTextUtilities.HasVisibleLinks(entry, "elasticsearch", true);

		result.Should().BeTrue();
	}

	[Test]
	[Arguments("# PRIVATE: https://github.com/elastic/elasticsearch-serverless/pull/7606", "https://github.com/elastic/elasticsearch-serverless/pull/7606")]
	[Arguments("https://github.com/elastic/elasticsearch/pull/1", "https://github.com/elastic/elasticsearch/pull/1")]
	[Arguments("  # PRIVATE: elastic/repo#12  ", "elastic/repo#12")]
	public void StripPrivateReferenceSentinel_UnwrapsSentinel(string input, string expected) =>
		ChangelogTextUtilities.StripPrivateReferenceSentinel(input).Should().Be(expected);

	[Test]
	public void StripPrivateReferenceSentinels_DropsEmptyAfterStrip()
	{
		var result = ChangelogTextUtilities.StripPrivateReferenceSentinels([
			"# PRIVATE: ",
			"https://github.com/elastic/elasticsearch/pull/1"
		]);
		result.Should().Equal("https://github.com/elastic/elasticsearch/pull/1");
	}
}
