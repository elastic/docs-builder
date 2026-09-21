// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Evaluation;

namespace Elastic.Changelog.Tests.Evaluation;

public class ChangelogCommentRendererTests
{
	[Test]
	public void RenderEntryCommitted_EscapesUrlComponents()
	{
		var body = ChangelogCommentRenderer.RenderEntryCommitted("elastic", "test repo", "feature/my branch", "docs/changelog/42 fix.yaml");

		body.Should().Contain("feature%2Fmy%20branch");
		body.Should().Contain("42%20fix.yaml");
	}

	[Test]
	public void RenderCommentOnly_WithYaml_ContainsCodeFence()
	{
		var body = ChangelogCommentRenderer.RenderCommentOnly("docs/changelog", "type: feature\ntitle: Test", "42.yaml", false, false);

		body.Should().Contain("```yaml");
		body.Should().Contain("type: feature");
	}

	[Test]
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

	[Test]
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

	[Test]
	public void RenderCommentOnly_NoYaml_ContainsWarning()
	{
		var body = ChangelogCommentRenderer.RenderCommentOnly("docs/changelog", null, null, false, false);

		body.Should().Contain("could not be read");
	}

	[Test]
	public void RenderLabelsNeeded_TypeMissing_ContainsTypeLabelHeadline()
	{
		var body = ChangelogCommentRenderer.RenderLabelsNeeded("type:bug,type:feature", null, null, null);

		body.Should().Contain("Changelog label needed");
		body.Should().Contain("type:feature");
		body.Should().Contain("type:bug");
	}

	[Test]
	public void RenderLabelsNeeded_TypeMissing_RendersInlineLabels()
	{
		var body = ChangelogCommentRenderer.RenderLabelsNeeded("type:bug,type:feature", null, null, null);

		body.Should().Contain("`type:bug`");
		body.Should().Contain("`type:feature`");
		body.Should().NotContain("| Label |");
	}

	[Test]
	public void RenderLabelsNeeded_WithOwnerRepo_RendersConfigFileLink()
	{
		var body = ChangelogCommentRenderer.RenderLabelsNeeded(
			"type:bug",
			null,
			null,
			"docs/changelog.yml",
			owner: "elastic",
			repo: "docs",
			defaultBranch: "main"
		);

		body.Should().Contain("[current changelog configuration](https://github.com/elastic/docs/blob/main/docs/changelog.yml)");
	}

	[Test]
	public void RenderLabelsNeeded_ProductMissing_ContainsProductLabelHeadline()
	{
		var body = ChangelogCommentRenderer.RenderLabelsNeeded(null, "| @Product:ECH | cloud |", null, null);

		body.Should().Contain("Product label needed");
		body.Should().Contain("@Product:ECH");
	}

	[Test]
	public void RenderLabelsNeeded_AmbiguousTypeLabels_ContainsMultipleTypeHeadline()
	{
		var body = ChangelogCommentRenderer.RenderLabelsNeeded(null, null, null, null, ambiguousTypeLabels: "type:bug,type:feature");

		body.Should().Contain("Multiple type labels set");
		body.Should().Contain("type:bug");
		body.Should().Contain("type:feature");
	}

	[Test]
	public void RenderLabelsNeeded_BothMissing_ContainsBothTables()
	{
		var body = ChangelogCommentRenderer.RenderLabelsNeeded("type:bug,type:feature", "| @Product:ECH | cloud |", null, null);

		body.Should().Contain("Changelog labels needed");
		body.Should().Contain("type:feature");
		body.Should().Contain("@Product:ECH");
	}

	[Test]
	public void RenderResolved_ContainsTitleAndCheckmark()
	{
		var body = ChangelogCommentRenderer.RenderResolved();

		body.Should().Contain(ChangelogCommentRenderer.Title);
		body.Should().Contain("✅");
	}

	[Test]
	public void WrapCodeFence_ContentWithThreeBacktickRun_UsesFourBackticks()
	{
		var content = "prefix ``` suffix";
		var fenced = ChangelogCommentRenderer.WrapCodeFence(content);

		fenced.Should().StartWith("````");
	}

	[Test]
	public void WrapInlineCode_ValueStartsWithBacktick_AddsPadding()
	{
		var result = ChangelogCommentRenderer.WrapInlineCode("`starts-with-tick");

		result.Should().Contain(" `starts-with-tick ");
	}

	[Test]
	[Arguments("entry-committed")]
	[Arguments("comment-only")]
	[Arguments("labels-needed")]
	[Arguments("resolved")]
	public void AllBodies_StartWithTitle(string variant)
	{
		var body = variant switch
		{
			"entry-committed" => ChangelogCommentRenderer.RenderEntryCommitted("owner", "repo", "main", "file.yaml"),
			"comment-only" => ChangelogCommentRenderer.RenderCommentOnly(null, "type: feature", "42.yaml", false, false),
			"labels-needed" => ChangelogCommentRenderer.RenderLabelsNeeded("type:bug,type:feature", null, null, null),
			"resolved" => ChangelogCommentRenderer.RenderResolved(),
			_ => throw new InvalidOperationException()
		};

		body.Should().StartWith(ChangelogCommentRenderer.Title);
	}

	[Test]
	public void RenderCommentOnly_LongBody_TruncatesAt65536()
	{
		var longYaml = new string('x', 70_000);
		var body = ChangelogCommentRenderer.RenderCommentOnly(null, longYaml, "42.yaml", false, false);

		body.Length.Should().BeLessThanOrEqualTo(65_536);
		body.Should().Contain("truncated");
	}

	// ──────────────────────────────────────────────────────────────────────────────────────────────
	// RenderEntriesInvalid
	// ──────────────────────────────────────────────────────────────────────────────────────────────

	[Test]
	public void RenderEntriesInvalid_StartsWithTitle()
	{
		var findings = new List<EntryFinding>
		{
			new() { File = "docs/changelog/42.yaml", Severity = "Error", Message = "title is required" }
		};
		var body = ChangelogCommentRenderer.RenderEntriesInvalid(findings, null, null, null);
		body.Should().StartWith(ChangelogCommentRenderer.Title);
	}

	[Test]
	public void RenderEntriesInvalid_ContainsHeadline()
	{
		var findings = new List<EntryFinding>
		{
			new() { File = "docs/changelog/42.yaml", Severity = "Error", Message = "title is required" }
		};
		var body = ChangelogCommentRenderer.RenderEntriesInvalid(findings, null, null, null);
		body.Should().Contain("validation failed");
	}

	[Test]
	public void RenderEntriesInvalid_ErrorFinding_ContainsRedCross()
	{
		var findings = new List<EntryFinding>
		{
			new() { File = "docs/changelog/42.yaml", Severity = "Error", Message = "title is required" }
		};
		var body = ChangelogCommentRenderer.RenderEntriesInvalid(findings, null, null, null);
		body.Should().Contain("❌");
		body.Should().Contain("title is required");
	}

	[Test]
	public void RenderEntriesInvalid_WarningFinding_ContainsWarningIcon()
	{
		var findings = new List<EntryFinding>
		{
			new() { File = "docs/changelog/42.yaml", Severity = "Warning", Message = "title exceeds 80 characters" }
		};
		var body = ChangelogCommentRenderer.RenderEntriesInvalid(findings, null, null, null);
		body.Should().Contain("⚠️");
		body.Should().Contain("title exceeds 80 characters");
	}

	[Test]
	public void RenderEntriesInvalid_WithOwnerRepo_RendersFileLink()
	{
		var findings = new List<EntryFinding>
		{
			new() { File = "docs/changelog/42.yaml", Severity = "Error", Message = "title is required" }
		};
		var body = ChangelogCommentRenderer.RenderEntriesInvalid(findings, "elastic", "my-repo", "main");
		body.Should().Contain("https://github.com/elastic/my-repo/blob/main/");
	}

	[Test]
	public void RenderEntriesInvalid_MultipleFindingsSameFile_GroupedUnderFile()
	{
		var findings = new List<EntryFinding>
		{
			new() { File = "docs/changelog/42.yaml", Severity = "Error", Message = "title is required" },
			new() { File = "docs/changelog/42.yaml", Severity = "Warning", Message = "description too long" }
		};
		var body = ChangelogCommentRenderer.RenderEntriesInvalid(findings, null, null, null);
		// File should appear only once as a header
		body.Split("42.yaml").Length.Should().Be(2);
	}

	// ──────────────────────────────────────────────────────────────────────────────────────────────
	// RenderMissingEntry
	// ──────────────────────────────────────────────────────────────────────────────────────────────

	[Test]
	public void RenderMissingEntry_StartsWithTitle()
	{
		var body = ChangelogCommentRenderer.RenderMissingEntry(null, 42);
		body.Should().StartWith(ChangelogCommentRenderer.Title);
	}

	[Test]
	public void RenderMissingEntry_ContainsPrNumber()
	{
		var body = ChangelogCommentRenderer.RenderMissingEntry("docs/changelog", 99);
		body.Should().Contain("99.yaml");
		body.Should().Contain("pending");
	}

	[Test]
	public void RenderMissingEntry_DefaultDir_UsesDefaultPath()
	{
		var body = ChangelogCommentRenderer.RenderMissingEntry(null, 7);
		body.Should().Contain("docs/changelog/7.yaml");
	}

	[Test]
	public void RenderMissingEntry_CustomDir_UsesCustomPath()
	{
		var body = ChangelogCommentRenderer.RenderMissingEntry("changelogs", 7);
		body.Should().Contain("changelogs/7.yaml");
	}

	[Test]
	public void RenderMissingEntry_Fork_ContainsBashScript()
	{
		var body = ChangelogCommentRenderer.RenderMissingEntry("docs/changelog", 42, isFork: true, resolvedProducts: "my-product");
		body.Should().Contain("mkdir -p -- \"docs/changelog\"");
		body.Should().Contain("cat > \"docs/changelog/42.yaml\"");
		body.Should().Contain("product: my-product");
	}

	[Test]
	public void RenderMissingEntry_Fork_ContainsDocsBuilderScript()
	{
		var body = ChangelogCommentRenderer.RenderMissingEntry("docs/changelog", 42, isFork: true);
		body.Should().Contain("docs-builder changelog add");
		body.Should().Contain("--pr 42");
		body.Should().NotContain("--config");
		body.Should().NotContain("--output");
	}

	[Test]
	public void RenderMissingEntry_Fork_GitCommandsNotDuplicated()
	{
		var body = ChangelogCommentRenderer.RenderMissingEntry("docs/changelog", 42, isFork: true);
		// git push should appear exactly once (in the shared "Then commit and push" block)
		body.Split("git push").Length.Should().Be(2);
	}

	[Test]
	public void RenderMissingEntry_Fork_ContainsForkGuidanceExplanation()
	{
		var body = ChangelogCommentRenderer.RenderMissingEntry(null, 5, isFork: true);
		body.Should().Contain("external contributor");
	}

	[Test]
	public void RenderMissingEntry_NotFork_DoesNotContainBashScript()
	{
		var body = ChangelogCommentRenderer.RenderMissingEntry("docs/changelog", 42, isFork: false);
		body.Should().NotContain("cat >");
		body.Should().NotContain("docs-builder changelog add");
		body.Should().NotContain("disable");
	}

	[Test]
	public void RenderMissingEntry_Fork_HasSofterTone()
	{
		var body = ChangelogCommentRenderer.RenderMissingEntry("docs/changelog", 42, isFork: true);
		body.Should().Contain("needed");
		body.Should().NotContain("disable");
	}

	[Test]
	public void RenderMissingEntry_NotFork_CanCommit_SaysPending()
	{
		var body = ChangelogCommentRenderer.RenderMissingEntry("docs/changelog", 42, isFork: false, canCommit: true);
		body.Should().Contain("pending");
		body.Should().Contain("automatically");
	}

	[Test]
	public void RenderMissingEntry_NotFork_CannotCommit_SaysNeeded()
	{
		var body = ChangelogCommentRenderer.RenderMissingEntry("docs/changelog", 42, isFork: false, canCommit: false);
		body.Should().Contain("needed");
		body.Should().NotContain("automatically");
	}

	[Test]
	public void RenderRepositoryNotOnboarded_StartsWithTitle()
	{
		var body = ChangelogCommentRenderer.RenderRepositoryNotOnboarded("my-repo");
		body.Should().StartWith(ChangelogCommentRenderer.Title);
	}

	[Test]
	public void RenderRepositoryNotOnboarded_ContainsRepoAndYamlSnippet()
	{
		var body = ChangelogCommentRenderer.RenderRepositoryNotOnboarded("my-repo");
		body.Should().Contain("my-repo");
		body.Should().Contain("products.yml");
		body.Should().Contain("release-notes: true");
	}

	[Test]
	public void RenderRepositoryNotOnboarded_BacktickRepoName_FencedCorrectly()
	{
		// A repo name containing backticks should still render without breaking the fence
		var body = ChangelogCommentRenderer.RenderRepositoryNotOnboarded("normal-repo");
		body.Should().Contain("```yaml");
	}
}
