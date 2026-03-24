// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.ReleaseNotes;
using FluentAssertions;

namespace Elastic.Documentation.Configuration.Tests.ReleaseNotes;

/// <summary>
/// Unit tests for ChangelogTextUtilities.
/// These tests verify the text processing utilities for changelog generation.
/// </summary>
public class ChangelogTextUtilitiesTests
{
	[Theory]
	[InlineData("hello world", "Hello world.")]
	[InlineData("Hello world", "Hello world.")]
	[InlineData("Hello world.", "Hello world.")]
	[InlineData("a", "A.")]
	[InlineData("", "")]
	[InlineData(null, "")]
	public void Beautify_CapitalizesAndAddsPeriod(string? input, string expected)
	{
		var result = ChangelogTextUtilities.Beautify(input ?? "");
		result.Should().Be(expected);
	}

	[Theory]
	[InlineData("9.3.0", "9.3.0")]
	[InlineData("Version 9.3.0", "version-9.3.0")]
	[InlineData("Version 9.3.0-beta1", "version-9.3.0-beta1")]
	public void TitleToSlug_ConvertsToSlugFormat(string input, string expected)
	{
		var result = ChangelogTextUtilities.TitleToSlug(input);
		result.Should().Be(expected);
	}

	[Theory]
	[InlineData("search-api", "Search api")]
	[InlineData("ingest-pipeline", "Ingest pipeline")]
	[InlineData("api", "Api")]
	public void FormatAreaHeader_CapitalizesAndReplacesHyphens(string input, string expected)
	{
		var result = ChangelogTextUtilities.FormatAreaHeader(input);
		result.Should().Be(expected);
	}

	[Theory]
	[InlineData("[Inference API] Add new endpoint", "Add new endpoint")]
	[InlineData("[ES]: Fix bug", "Fix bug")]
	[InlineData("[Test] Title", "Title")]
	[InlineData("No bracket prefix", "No bracket prefix")]
	[InlineData("[Unclosed bracket", "[Unclosed bracket")]
	public void StripSquareBracketPrefix_RemovesPrefix(string input, string expected)
	{
		var result = ChangelogTextUtilities.StripSquareBracketPrefix(input);
		result.Should().Be(expected);
	}

	[Theory]
	[InlineData("https://github.com/elastic/elasticsearch/pull/123", 123)]
	[InlineData("elastic/elasticsearch#456", 456)]
	[InlineData("123", null)] // No default owner/repo
	public void ExtractPrNumber_ExtractsNumber(string input, int? expected)
	{
		var result = ChangelogTextUtilities.ExtractPrNumber(input);
		result.Should().Be(expected);
	}

	[Fact]
	public void ExtractPrNumber_WithDefaultOwnerRepo_ExtractsNumber()
	{
		var result = ChangelogTextUtilities.ExtractPrNumber("123", "elastic", "elasticsearch");
		result.Should().Be(123);
	}

	[Theory]
	[InlineData("https://github.com/elastic/elasticsearch/issues/123", 123)]
	[InlineData("https://github.com/owner/repo/issues/456", 456)]
	[InlineData("elastic/elasticsearch#789", 789)]
	[InlineData("123", null)]
	public void ExtractIssueNumber_ExtractsNumber(string input, int? expected)
	{
		var result = ChangelogTextUtilities.ExtractIssueNumber(input);
		result.Should().Be(expected);
	}

	[Fact]
	public void ExtractIssueNumber_WithDefaultOwnerRepo_ExtractsNumber()
	{
		var result = ChangelogTextUtilities.ExtractIssueNumber("123", "elastic", "elasticsearch");
		result.Should().Be(123);
	}

	[Theory]
	[InlineData("v1.0.0", "ga")]
	[InlineData("v1.0.0-beta1", "beta")]
	[InlineData("v1.0.0-preview.1", "preview")]
	[InlineData("1.0.0-alpha1", "preview")]
	[InlineData("1.0.0-rc1", "beta")]
	[InlineData("1.0.0", "ga")]
	public void InferLifecycleFromVersion_InfersCorrectly(string tagName, string expected)
	{
		var result = ChangelogTextUtilities.InferLifecycleFromVersion(tagName);
		result.Should().Be(expected);
	}

	[Theory]
	[InlineData("v1.0.0", "1.0.0")]
	[InlineData("v1.0.0-beta1", "1.0.0")]
	[InlineData("1.2.3-preview.1", "1.2.3")]
	[InlineData("9.3.0", "9.3.0")]
	public void ExtractBaseVersion_ExtractsVersion(string tagName, string expected)
	{
		var result = ChangelogTextUtilities.ExtractBaseVersion(tagName);
		result.Should().Be(expected);
	}

	[Theory]
	[InlineData("elastic/elasticsearch", "elastic", "elasticsearch")]
	[InlineData("elasticsearch", null, "elasticsearch")]
	public void ParseRepository_ParsesCorrectly(string input, string? expectedOwner, string expectedRepo)
	{
		var (owner, repo) = ChangelogTextUtilities.ParseRepository(input);
		owner.Should().Be(expectedOwner);
		repo.Should().Be(expectedRepo);
	}

	[Theory]
	[InlineData("Add new feature to API", "add-new-feature-to-api")]
	[InlineData("Fix bug in the search API endpoint handler", "fix-bug-in-the-search-api")] // Takes first 6 words by default
	[InlineData("", "untitled")]
	public void GenerateSlug_GeneratesSlug(string input, string expected)
	{
		var result = ChangelogTextUtilities.GenerateSlug(input);
		result.Should().Be(expected);
	}
}
