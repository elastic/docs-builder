// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using Elastic.Documentation.Services.Changelog;
using FluentAssertions;

namespace Elastic.Documentation.Services.Tests;

[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test method names with underscores are standard in xUnit")]
public class ReleaseNotesExtractorTests
{
	[Fact]
	public void FindReleaseNote_WithReleaseNotesColon_ExtractsContent()
	{
		// Arrange
		// language=markdown
		var prBody =
			"""
			## Summary

			This PR adds a new feature.

			Release Notes: Adds support for new aggregation types
			""";

		// Act
		var result = ReleaseNotesExtractor.FindReleaseNote(prBody);

		// Assert
		result.Should().Be("Adds support for new aggregation types");
	}

	[Fact]
	public void FindReleaseNote_WithReleaseNotesDash_ExtractsContent()
	{
		// Arrange
		// language=markdown
		var prBody =
			"""
			## Summary

			Release Notes - Adds support for new aggregation types
			""";

		// Act
		var result = ReleaseNotesExtractor.FindReleaseNote(prBody);

		// Assert
		result.Should().Be("Adds support for new aggregation types");
	}

	[Fact]
	public void FindReleaseNote_WithReleaseNoteSingular_ExtractsContent()
	{
		// Arrange
		// language=markdown
		var prBody =
			"""
			## Summary

			Release Note: Adds support for new aggregation types
			""";

		// Act
		var result = ReleaseNotesExtractor.FindReleaseNote(prBody);

		// Assert
		result.Should().Be("Adds support for new aggregation types");
	}

	[Fact]
	public void FindReleaseNote_WithMarkdownHeader_ExtractsContent()
	{
		// Arrange
		// language=markdown
		var prBody =
			"""
			## Summary

			## Release Note

			Adds support for new aggregation types
			""";

		// Act
		var result = ReleaseNotesExtractor.FindReleaseNote(prBody);

		// Assert
		result.Should().Be("Adds support for new aggregation types");
	}

	[Fact]
	public void FindReleaseNote_WithCaseVariations_ExtractsContent()
	{
		// Arrange
		// language=markdown
		var prBody = "release notes: Adds support for new aggregation types";

		// Act
		var result = ReleaseNotesExtractor.FindReleaseNote(prBody);

		// Assert
		result.Should().Be("Adds support for new aggregation types");
	}

	[Fact]
	public void FindReleaseNote_WithHyphenatedFormat_ExtractsContent()
	{
		// Arrange
		// language=markdown
		var prBody = "Release-Notes: Adds support for new aggregation types";

		// Act
		var result = ReleaseNotesExtractor.FindReleaseNote(prBody);

		// Assert
		result.Should().Be("Adds support for new aggregation types");
	}

	[Fact]
	public void FindReleaseNote_WithHtmlComments_StripsComments()
	{
		// Arrange
		// language=markdown
		var prBody =
			"""
			<!-- This is a comment -->
			Release Notes: Adds support for new aggregation types
			<!-- Another comment -->
			""";

		// Act
		var result = ReleaseNotesExtractor.FindReleaseNote(prBody);

		// Assert
		result.Should().Be("Adds support for new aggregation types");
	}

	[Fact]
	public void FindReleaseNote_WithNoReleaseNote_ReturnsNull()
	{
		// Arrange
		// language=markdown
		var prBody =
			"""
			## Summary

			This PR adds a new feature but has no release notes section.
			""";

		// Act
		var result = ReleaseNotesExtractor.FindReleaseNote(prBody);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public void FindReleaseNote_WithEmptyBody_ReturnsNull()
	{
		// Arrange
		// language=markdown
		var prBody = "";

		// Act
		var result = ReleaseNotesExtractor.FindReleaseNote(prBody);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public void FindReleaseNote_WithNullBody_ReturnsNull()
	{
		// Arrange
		string? prBody = null;

		// Act
		var result = ReleaseNotesExtractor.FindReleaseNote(prBody);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public void FindReleaseNote_WithMultiLineReleaseNote_ExtractsUntilDoubleNewline()
	{
		// Arrange
		// language=markdown
		var prBody =
			"""
			Release Notes: This is a multi-line
			release note that spans
			multiple lines

			## Next Section
			""";

		// Act
		var result = ReleaseNotesExtractor.FindReleaseNote(prBody);

		// Assert
		result.Should().Be("This is a multi-line\nrelease note that spans\nmultiple lines");
	}

	[Fact]
	public void ExtractReleaseNotes_WithShortReleaseNote_ReturnsAsTitle()
	{
		// Arrange
		// language=markdown
		var prBody = "Release Notes: Adds support for new aggregation types";

		// Act
		var (title, description) = ReleaseNotesExtractor.ExtractReleaseNotes(prBody);

		// Assert
		title.Should().Be("Adds support for new aggregation types");
		description.Should().BeNull();
	}

	[Fact]
	public void ExtractReleaseNotes_WithLongReleaseNote_ReturnsAsDescription()
	{
		// Arrange
		// language=markdown
		var prBody = "Release Notes: Adds support for new aggregation types including date histogram, range aggregations, and nested aggregations with improved performance";

		// Act
		var (title, description) = ReleaseNotesExtractor.ExtractReleaseNotes(prBody);

		// Assert
		title.Should().BeNull();
		description.Should().Be("Adds support for new aggregation types including date histogram, range aggregations, and nested aggregations with improved performance");
	}

	[Fact]
	public void ExtractReleaseNotes_WithMultiLineReleaseNote_ReturnsAsDescription()
	{
		// Arrange
		// The regex stops at double newline, so we need a release note that spans multiple lines without double newline
		// language=markdown
		var prBody =
			"""
			Release Notes: Adds support for new aggregation types
			This includes date histogram and range aggregations
			with improved performance
			""";

		// Act
		var (title, description) = ReleaseNotesExtractor.ExtractReleaseNotes(prBody);

		// Assert
		// Since there's a newline in the content, it should be treated as multi-line
		title.Should().BeNull();
		description.Should().Contain("Adds support for new aggregation types");
		description.Should().Contain("\n");
	}

	[Fact]
	public void ExtractReleaseNotes_WithExactly120Characters_ReturnsAsTitle()
	{
		// Arrange
		// language=markdown
		var prBody = "Release Notes: " + new string('a', 120);

		// Act
		var (title, description) = ReleaseNotesExtractor.ExtractReleaseNotes(prBody);

		// Assert
		title.Should().Be(new string('a', 120));
		description.Should().BeNull();
	}

	[Fact]
	public void ExtractReleaseNotes_With121Characters_ReturnsAsDescription()
	{
		// Arrange
		// language=markdown
		var prBody = "Release Notes: " + new string('a', 121);

		// Act
		var (title, description) = ReleaseNotesExtractor.ExtractReleaseNotes(prBody);

		// Assert
		title.Should().BeNull();
		description.Should().Be(new string('a', 121));
	}

	[Fact]
	public void ExtractReleaseNotes_WithNoReleaseNote_ReturnsNulls()
	{
		// Arrange
		// language=markdown
		var prBody =
			"""
			## Summary

			This PR has no release notes.
			""";

		// Act
		var (title, description) = ReleaseNotesExtractor.ExtractReleaseNotes(prBody);

		// Assert
		title.Should().BeNull();
		description.Should().BeNull();
	}
}
