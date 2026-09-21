// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Changelog.Tests;

public class ReleaseNotesExtractorTests
{
	[Test]
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

	[Test]
	public void FindReleaseNote_WithReleaseNotesDash_ExtractsContent()
	{
		// Arrange
		// language=markdown
		var prBody = """
			## Summary

			Release Notes - Adds support for new aggregation types
			""";

		// Act
		var result = ReleaseNotesExtractor.FindReleaseNote(prBody);

		// Assert
		result.Should().Be("Adds support for new aggregation types");
	}

	[Test]
	public void FindReleaseNote_WithReleaseNoteSingular_ExtractsContent()
	{
		// Arrange
		// language=markdown
		var prBody = """
			## Summary

			Release Note: Adds support for new aggregation types
			""";

		// Act
		var result = ReleaseNotesExtractor.FindReleaseNote(prBody);

		// Assert
		result.Should().Be("Adds support for new aggregation types");
	}

	[Test]
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

	[Test]
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

	[Test]
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

	[Test]
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

	[Test]
	public void FindReleaseNote_WithNoReleaseNote_ReturnsNull()
	{
		// Arrange
		// language=markdown
		var prBody = """
			## Summary

			This PR adds a new feature but has no release notes section.
			""";

		// Act
		var result = ReleaseNotesExtractor.FindReleaseNote(prBody);

		// Assert
		result.Should().BeNull();
	}

	[Test]
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

	[Test]
	public void FindReleaseNote_WithNullBody_ReturnsNull()
	{
		// Arrange
		string? prBody = null;

		// Act
		var result = ReleaseNotesExtractor.FindReleaseNote(prBody);

		// Assert
		result.Should().BeNull();
	}

	[Test]
	public void FindReleaseNote_WithMultiLineReleaseNote_ExtractsUntilDoubleNewline()
	{
		// Arrange
		// language=markdown
		var prBody = """
			Release Notes: This is a multi-line
			release note that spans
			multiple lines

			## Next Section
			""".ReplaceLineEndings(
			"\n"
		);

		// Act
		var result = ReleaseNotesExtractor.FindReleaseNote(prBody);

		// Assert
		result.Should().Be("This is a multi-line\nrelease note that spans\nmultiple lines");
	}

	[Test]
	public void ExtractReleaseNote_ContentAfterFirstParagraph_ReportsTruncation()
	{
		// language=markdown
		var prBody = """
			## Release note

			First paragraph.

			Second paragraph.
			""";

		var result = ReleaseNotesExtractor.ExtractReleaseNote(prBody);

		result.Content.Should().Be("First paragraph.");
		result.WasTruncated.Should().BeTrue();
	}

	[Test]
	public void ExtractReleaseNote_NoTrailingContent_DoesNotReportTruncation()
	{
		var result = ReleaseNotesExtractor.ExtractReleaseNote("Release note: Complete description.");

		result.Content.Should().Be("Complete description.");
		result.WasTruncated.Should().BeFalse();
	}

	[Test]
	public void FindReleaseNote_WithExactly120Characters_ReturnsContent()
	{
		// Arrange
		// language=markdown
		var expected = new string('a', 120);
		var prBody = "Release Notes: " + expected;

		// Act
		var result = ReleaseNotesExtractor.FindReleaseNote(prBody);

		// Assert
		result.Should().Be(expected);
	}

	[Test]
	public void FindReleaseNote_With121Characters_ReturnsContent()
	{
		// Arrange
		// language=markdown
		var expected = new string('a', 121);
		var prBody = "Release Notes: " + expected;

		// Act
		var result = ReleaseNotesExtractor.FindReleaseNote(prBody);

		// Assert
		result.Should().Be(expected);
	}
}
