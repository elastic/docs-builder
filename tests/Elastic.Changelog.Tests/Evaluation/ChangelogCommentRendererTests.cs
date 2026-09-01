// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Evaluation;

namespace Elastic.Changelog.Tests.Evaluation;

public class ChangelogCommentRendererTests
{
	[Fact]
	public void RenderEntryCommitted_EscapesUrlComponents()
	{
		var body = ChangelogCommentRenderer.RenderEntryCommitted("elastic", "test repo", "feature/my branch", "docs/changelog/42 fix.yaml");

		body.Should().Contain("feature%2Fmy%20branch");
		body.Should().Contain("42%20fix.yaml");
	}

	[Fact]
	public void RenderCommentOnly_WithYaml_ContainsCodeFence()
	{
		var body = ChangelogCommentRenderer.RenderCommentOnly("docs/changelog", "type: feature\ntitle: Test", "42.yaml", false, false);

		body.Should().Contain("```yaml");
		body.Should().Contain("type: feature");
	}

	[Fact]
	public void RenderCommentOnly_ForkVariant_ContainsInformationalGuidance()
	{
		var body = ChangelogCommentRenderer.RenderCommentOnly(
			"docs/changelog",
			"type: feature",
			"42.yaml",
			isFork: true,
			commitFailed: false
		);

		body.Should().Contain("regenerated from the live PR record");
	}

	[Fact]
	public void RenderCommentOnly_CommitFailedVariant_ContainsFailureGuidance()
	{
		var body = ChangelogCommentRenderer.RenderCommentOnly(
			"docs/changelog",
			"type: feature",
			"42.yaml",
			isFork: false,
			commitFailed: true
		);

		body.Should().Contain("could not commit");
	}

	[Fact]
	public void RenderCommentOnly_NoYaml_ContainsWarning()
	{
		var body = ChangelogCommentRenderer.RenderCommentOnly("docs/changelog", null, null, false, false);

		body.Should().Contain("could not be read");
	}

	[Fact]
	public void RenderLabelsNeeded_TypeMissing_ContainsTypeLabelHeadline()
	{
		var body = ChangelogCommentRenderer.RenderLabelsNeeded("| type:feature | feature |", null, null, null);

		body.Should().Contain("Changelog label needed");
		body.Should().Contain("type:feature");
	}

	[Fact]
	public void RenderLabelsNeeded_ProductMissing_ContainsProductLabelHeadline()
	{
		var body = ChangelogCommentRenderer.RenderLabelsNeeded(null, "| @Product:ECH | cloud |", null, null);

		body.Should().Contain("Product label needed");
		body.Should().Contain("@Product:ECH");
	}

	[Fact]
	public void RenderLabelsNeeded_BothMissing_ContainsBothTables()
	{
		var body = ChangelogCommentRenderer.RenderLabelsNeeded("| type:feature | feature |", "| @Product:ECH | cloud |", null, null);

		body.Should().Contain("Changelog labels needed");
		body.Should().Contain("type:feature");
		body.Should().Contain("@Product:ECH");
	}

	[Fact]
	public void RenderResolved_ContainsTitleAndCheckmark()
	{
		var body = ChangelogCommentRenderer.RenderResolved();

		body.Should().Contain(ChangelogCommentRenderer.Title);
		body.Should().Contain("✅");
	}

	[Fact]
	public void WrapCodeFence_ContentWithThreeBacktickRun_UsesFourBackticks()
	{
		var content = "prefix ``` suffix";
		var fenced = ChangelogCommentRenderer.WrapCodeFence(content);

		fenced.Should().StartWith("````");
	}

	[Fact]
	public void WrapInlineCode_ValueStartsWithBacktick_AddsPadding()
	{
		var result = ChangelogCommentRenderer.WrapInlineCode("`starts-with-tick");

		result.Should().Contain(" `starts-with-tick ");
	}

	[Theory]
	[InlineData("entry-committed")]
	[InlineData("comment-only")]
	[InlineData("labels-needed")]
	[InlineData("resolved")]
	public void AllBodies_StartWithTitle(string variant)
	{
		var body = variant switch
		{
			"entry-committed" => ChangelogCommentRenderer.RenderEntryCommitted("owner", "repo", "main", "file.yaml"),
			"comment-only" => ChangelogCommentRenderer.RenderCommentOnly(null, "type: feature", "42.yaml", false, false),
			"labels-needed" => ChangelogCommentRenderer.RenderLabelsNeeded("| label | type |", null, null, null),
			"resolved" => ChangelogCommentRenderer.RenderResolved(),
			_ => throw new InvalidOperationException()
		};

		body.Should().StartWith(ChangelogCommentRenderer.Title);
	}

	[Fact]
	public void RenderCommentOnly_LongBody_TruncatesAt65536()
	{
		var longYaml = new string('x', 70_000);
		var body = ChangelogCommentRenderer.RenderCommentOnly(null, longYaml, "42.yaml", false, false);

		body.Length.Should().BeLessThanOrEqualTo(65_536);
		body.Should().Contain("truncated");
	}
}
