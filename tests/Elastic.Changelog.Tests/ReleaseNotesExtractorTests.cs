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

	[Test]
	public void FindImpactAndAction_FromHeadings_ExtractsSections()
	{
		// language=markdown
		var prBody =
			"""
			## Release note
			Removes the `simulate` parameter.

			## Impact
			API users that pass `simulate` get a 400.

			- second line

			## Action
			Stop sending `simulate`.

			## Checklist
			- [ ] tests
			""";

		ReleaseNotesExtractor.FindImpact(prBody).Should().Be("API users that pass `simulate` get a 400.\n\n- second line");
		ReleaseNotesExtractor.FindAction(prBody).Should().Be("Stop sending `simulate`.");
	}

	[Test]
	public void FindImpactAndAction_FromLabels_ExtractsInlineText()
	{
		// language=markdown
		var prBody =
			"""
			Impact: Clients break.
			Action: Upgrade the client.

			Unrelated paragraph.
			""";

		ReleaseNotesExtractor.FindImpact(prBody).Should().Be("Clients break.");
		ReleaseNotesExtractor.FindAction(prBody).Should().Be("Upgrade the client.");
	}

	[Test]
	public void FindAction_PluralHeading_ExtractsSection()
	{
		// language=markdown
		var prBody = """
			### Actions
			Run the migration.
			""";

		ReleaseNotesExtractor.FindAction(prBody).Should().Be("Run the migration.");
	}

	[Test]
	public void FindImpactAndAction_OnlyHtmlComments_ReturnNull()
	{
		// language=markdown
		var prBody =
			"""
			## Impact
			<!-- describe the impact -->

			## Action
			<!-- describe the action -->
			""";

		ReleaseNotesExtractor.FindImpact(prBody).Should().BeNull();
		ReleaseNotesExtractor.FindAction(prBody).Should().BeNull();
	}

	[Test]
	public void FindImpactAndAction_ProseMentions_DoNotMatch()
	{
		// language=markdown
		var prBody =
			"""
			Impact of this change is small.
			Actions taken in CI are unchanged.

			## Impact on tests
			None.
			""";

		ReleaseNotesExtractor.FindImpact(prBody).Should().BeNull();
		ReleaseNotesExtractor.FindAction(prBody).Should().BeNull();
	}

	[Test]
	public void FindImpactAndAction_KibanaBreakingPrBodyWithoutSections_ReturnNull()
	{
		// language=markdown
		var prBody =
			"""
			**Do not merge. This is a throwaway PR.**

			## What

			Two intentional breaking changes.

			## Why

			To see the PR comment the API contracts check posts.
			""";

		ReleaseNotesExtractor.FindImpact(prBody).Should().BeNull();
		ReleaseNotesExtractor.FindAction(prBody).Should().BeNull();
	}

	[Test]
	public void FindImpact_EmptyImpactLabelFollowedByActionLabel_ReturnsNull()
	{
		// language=markdown
		var prBody = """
			Impact:
			Action: Upgrade the client.
			""";

		ReleaseNotesExtractor.FindImpact(prBody).Should().BeNull();
		ReleaseNotesExtractor.FindAction(prBody).Should().Be("Upgrade the client.");
	}

	[Test]
	public void FindImpact_ActionHeadingNestedUnderImpact_StopsAtAction()
	{
		// language=markdown
		var prBody = """
			## Impact
			Clients break.
			### Action
			Upgrade the client.
			""";

		ReleaseNotesExtractor.FindImpact(prBody).Should().Be("Clients break.");
		ReleaseNotesExtractor.FindAction(prBody).Should().Be("Upgrade the client.");
	}

	[Test]
	public void FindAction_YamlExampleInsideFence_StaysInTheSection()
	{
		// language=markdown
		var prBody =
			"""
			## Impact
			Clients break.

			## Action
			Set the option:

			```yaml
			action: restart
			## not a heading
			```

			Then migrate your clients.

			## Checklist
			- [ ] tests
			""";

		ReleaseNotesExtractor
			.FindAction(prBody)
			.Should()
			.Be("Set the option:\n\n```yaml\naction: restart\n## not a heading\n```\n\nThen migrate your clients.");
		ReleaseNotesExtractor.FindImpact(prBody).Should().Be("Clients break.");
	}

	[Test]
	public void FindImpact_LabelInsideFencedExampleOnly_DoesNotStartASection()
	{
		// language=markdown
		var prBody = """
			Example:

			```
			Impact: not a real section
			```
			""";

		ReleaseNotesExtractor.FindImpact(prBody).Should().BeNull();
	}

	[Test]
	public void FindImpactAndAction_ShorterFenceInsideLongerFence_DoesNotCloseIt()
	{
		// language=markdown
		var prBody =
			"""
			## Impact
			Clients break. Example:

			````markdown
			```yaml
			action: restart
			```
			## not a heading
			````

			## Action
			Upgrade the client.
			""";

		ReleaseNotesExtractor.FindAction(prBody).Should().Be("Upgrade the client.");
		ReleaseNotesExtractor
			.FindImpact(prBody)
			.Should()
			.Be("Clients break. Example:\n\n````markdown\n```yaml\naction: restart\n```\n## not a heading\n````");
	}

	[Test]
	public void FindAction_FenceWithTextAfterTheRun_DoesNotCloseTheBlock()
	{
		// language=markdown
		var prBody =
			"""
			## Action
			Run:

			```bash
			```not a closing fence
			## still code
			```

			Then restart.
			""";

		ReleaseNotesExtractor
			.FindAction(prBody)
			.Should()
			.Be("Run:\n\n```bash\n```not a closing fence\n## still code\n```\n\nThen restart.");
	}
}
