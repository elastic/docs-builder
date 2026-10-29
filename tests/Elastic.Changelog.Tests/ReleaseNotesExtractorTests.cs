// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Changelog.Tests;

public class ReleaseNotesExtractorTests
{
	[Test]
	public void ExtractReleaseNote_BlockquotedNote_StripsTheMarker()
	{
		// The shape of elastic/kibana#296878: the note is a blockquote under its heading, followed by more body.
		// language=markdown
		var prBody =
			"""
			## Summary

			Classify alerts with a prompt.

			### Release note

			> The Alert analysis workflow can now classify alerts with a single prompt instead of an AI agent.

			**Suggested label:** `release_note:enhancement`
			""";

		var result = ReleaseNotesExtractor.ExtractReleaseNote(prBody);

		result.Content.Should().Be("The Alert analysis workflow can now classify alerts with a single prompt instead of an AI agent.");
		result.WasTruncated.Should().BeTrue();
	}

	[Test]
	public void FindReleaseNote_MultiLineBlockquote_StripsEveryMarker()
	{
		// language=markdown
		var prBody =
			"""
			## Release note
			> Adds an Anonymization page under Stack Management.
			> Requires an Enterprise license.
			""";

		ReleaseNotesExtractor
			.FindReleaseNote(prBody)
			.Should()
			.Be("Adds an Anonymization page under Stack Management.\nRequires an Enterprise license.");
	}

	[Test]
	public void FindReleaseNote_NestedBlockquote_StripsEveryMarker()
	{
		// language=markdown
		var prBody =
			"""
			Release note:
			>> Nested quote.
			> > Spaced nested quote.
			  > Indented quote.
			""";

		ReleaseNotesExtractor.FindReleaseNote(prBody).Should().Be("Nested quote.\nSpaced nested quote.\nIndented quote.");
	}

	[Test]
	public void FindReleaseNote_FencedCodeInsideTheNote_KeepsItsLines()
	{
		// language=markdown
		var prBody =
			"""
			## Release note
			> Compare the exports:
			```sh
			sort a.txt
			> output.log
			```
			""";

		ReleaseNotesExtractor.FindReleaseNote(prBody).Should().Be("Compare the exports:\n```sh\nsort a.txt\n> output.log\n```");
	}

	[Test]
	public void FindImpact_IndentedCodeLine_KeepsItsMarkerAndIndentation()
	{
		// Four spaces or a tab start an indented code block, so its ">" is content, not a blockquote marker.
		// language=markdown
		var prBody =
			"""
			## Impact
			> Redirect the log:
			    > output.log
				> prompt.log
			""";

		ReleaseNotesExtractor.FindImpact(prBody).Should().Be("Redirect the log:\n    > output.log\n\t> prompt.log");
	}

	[Test]
	public void FindReleaseNote_CrlfBody_StripsMarkersAndReadsWithLf()
	{
		// GitHub sends pull request bodies with "\r\n", and a Windows checkout gives the raw literals above "\r\n" too.
		var prBody = "## Summary\r\n\r\nText.\r\n\r\n### Release note\r\n\r\n> First line.\r\n> Second line.\r\n\r\nMore body.";

		var result = ReleaseNotesExtractor.ExtractReleaseNote(prBody);

		result.Content.Should().Be("First line.\nSecond line.");
		result.WasTruncated.Should().BeTrue();
	}

	[Test]
	public void FindReleaseNote_GreaterThanInsideTheText_IsKept()
	{
		// language=markdown
		var prBody = """
			Release note: Rejects values > 10 and keeps a -> b as written.
			""";

		ReleaseNotesExtractor.FindReleaseNote(prBody).Should().Be("Rejects values > 10 and keeps a -> b as written.");
	}

	[Test]
	public void FindImpactAndAction_BlockquotedSections_StripTheMarkers()
	{
		// language=markdown
		var prBody =
			"""
			## Impact
			> Clients that send the old field break.
			> Update them before you upgrade.

			## Action
			> Upgrade the client.
			""";

		ReleaseNotesExtractor.FindImpact(prBody).Should().Be("Clients that send the old field break.\nUpdate them before you upgrade.");
		ReleaseNotesExtractor.FindAction(prBody).Should().Be("Upgrade the client.");
	}

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
	public void FindAction_TemplateSectionsOneLevelDeeper_EndTheSection()
	{
		// kibana#299837: the Kibana PR template puts `### Checklist` and `### Identify risks` after the author's
		// `## Action`. A deeper heading is the next section, not part of the action.
		// language=markdown
		var prBody =
			"""
			## Impact
			Only API clients that send invalid `agents` input are affected.

			## Action
			No action is needed for valid requests. If a bulk request now returns a `400`:

			- Unknown field in the KQL query: use only agent fields.

			### Checklist

			- [ ] Any text added follows EUI's writing guidelines

			### Identify risks

			- [ ] Bulk requests with invalid input now fail with a 400.

			Generated with [Claude Code](https://claude.com/claude-code)
			""";

		ReleaseNotesExtractor.FindImpact(prBody).Should().Be("Only API clients that send invalid `agents` input are affected.");
		ReleaseNotesExtractor
			.FindAction(prBody)
			.Should()
			.Be(
				"No action is needed for valid requests. If a bulk request now returns a `400`:\n\n- Unknown field in the KQL query: use only agent fields."
			);
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
