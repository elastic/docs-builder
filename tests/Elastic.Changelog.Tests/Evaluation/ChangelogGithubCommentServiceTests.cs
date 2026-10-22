// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Evaluation;
using Elastic.Changelog.GitHub;
using Elastic.Changelog.Tests.Changelogs;
using Elastic.Documentation.Configuration;
using FakeItEasy;

namespace Elastic.Changelog.Tests.Evaluation;

public class ChangelogGithubCommentServiceTests() : ChangelogTestBase()
{
	private static readonly string Root = Paths.WorkingDirectoryRoot.FullName;

	private string MetadataPath => Path.Join(Root, GithubDecisionMetadataWriter.ArtifactDir, GithubDecisionMetadataWriter.MetadataFilename);
	private string MetadataDir => Path.Join(Root, GithubDecisionMetadataWriter.ArtifactDir);

	private async Task WriteMetadata(GithubDecisionMetadata metadata)
	{
		var writer = new GithubDecisionMetadataWriter(LoggerFactory, RunnerTempFileSystem);
		await writer.WriteAsync(metadata, CancellationToken.None);
	}

	private void WriteYaml(string filename, string content = "type: feature\ntitle: Test")
	{
		var path = RunnerTempFileSystem.Path.Join(MetadataDir, filename);
		RunnerTempFileSystem.File.WriteAllText(path, content);
	}

	private ChangelogGithubCommentService CreateService(IGitHubCommentService commentSvc) =>
		new(LoggerFactory, commentSvc, RunnerTempFileSystem);

	private GithubCommentArguments DefaultArgs() =>
		new() { MetadataPath = MetadataPath, MetadataDir = MetadataDir, Owner = "elastic", Repo = "test-repo" };

	private static GithubDecisionMetadata BaseMetadata(string status = "proceed", bool canCommit = true, bool isFork = false) =>
		new()
		{
			PrNumber = 42,
			HeadRef = "feature/test",
			HeadSha = "abc123",
			Status = status,
			IsFork = isFork,
			CanCommit = canCommit,
			MaintainerCanModify = false,
			HeadRepo = "elastic/test-repo"
		};

	[Test]
	public async Task PostComment_CommittedOutcome_RendersEntryCommittedBody()
	{
		await WriteMetadata(BaseMetadata() with { CommitOutcome = CommitOutcome.Committed, CommittedFile = "docs/changelog/42.yaml" });
		var commentSvc = A.Fake<IGitHubCommentService>();
		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(A<string>._, A<string>._, A<int>._, A<string>._, A<CancellationToken>._)
		).Returns((string?)"IC_test_node_id");
		A.CallTo(() => commentSvc.MinimizeCommentAsync(A<string>._, A<CancellationToken>._)).Returns(true);
		A.CallTo(() => commentSvc.UnminimizeCommentAsync(A<string>._, A<CancellationToken>._)).Returns(true);

		var result = await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		result.Should().BeTrue();
		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(
				"elastic",
				"test-repo",
				42,
				A<string>.That.Contains("docs/changelog/42.yaml"),
				A<CancellationToken>._
			)
		).MustHaveHappenedOnceExactly();
	}

	[Test]
	public async Task PostComment_CommitFailed_RendersCommentOnlyWithFailureGuidance()
	{
		await WriteMetadata(BaseMetadata() with { CommitOutcome = CommitOutcome.Failed });
		WriteYaml("42.yaml");
		var commentSvc = A.Fake<IGitHubCommentService>();
		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(A<string>._, A<string>._, A<int>._, A<string>._, A<CancellationToken>._)
		).Returns((string?)"IC_test_node_id");
		A.CallTo(() => commentSvc.MinimizeCommentAsync(A<string>._, A<CancellationToken>._)).Returns(true);
		A.CallTo(() => commentSvc.UnminimizeCommentAsync(A<string>._, A<CancellationToken>._)).Returns(true);

		await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(
				A<string>._,
				A<string>._,
				A<int>._,
				A<string>.That.Contains("could not commit"),
				A<CancellationToken>._
			)
		).MustHaveHappenedOnceExactly();
	}

	[Test]
	public async Task PostComment_SuccessNotCanCommit_RendersCommentOnlyInformational()
	{
		await WriteMetadata(BaseMetadata(status: "proceed", canCommit: false));
		WriteYaml("42.yaml");
		var commentSvc = A.Fake<IGitHubCommentService>();
		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(A<string>._, A<string>._, A<int>._, A<string>._, A<CancellationToken>._)
		).Returns((string?)"IC_test_node_id");
		A.CallTo(() => commentSvc.MinimizeCommentAsync(A<string>._, A<CancellationToken>._)).Returns(true);
		A.CallTo(() => commentSvc.UnminimizeCommentAsync(A<string>._, A<CancellationToken>._)).Returns(true);

		await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(
				A<string>._,
				A<string>._,
				A<int>._,
				A<string>.That.Contains("informational"),
				A<CancellationToken>._
			)
		).MustHaveHappenedOnceExactly();
	}

	[Test]
	public async Task PostComment_NoLabel_RendersLabelsNeededBody()
	{
		await WriteMetadata(BaseMetadata(status: "no-label") with
		{
			LabelTable = "| type:feature | feature |",
			SkipLabels = "changelog:skip"
		});
		var commentSvc = A.Fake<IGitHubCommentService>();
		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(A<string>._, A<string>._, A<int>._, A<string>._, A<CancellationToken>._)
		).Returns((string?)"IC_test_node_id");
		A.CallTo(() => commentSvc.MinimizeCommentAsync(A<string>._, A<CancellationToken>._)).Returns(true);
		A.CallTo(() => commentSvc.UnminimizeCommentAsync(A<string>._, A<CancellationToken>._)).Returns(true);

		await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(
				A<string>._,
				A<string>._,
				A<int>._,
				A<string>.That.Contains("Changelog label needed"),
				A<CancellationToken>._
			)
		).MustHaveHappenedOnceExactly();
	}

	[Test]
	public async Task PostComment_SuccessWithNoYaml_DeletesStickyComment()
	{
		await WriteMetadata(BaseMetadata(status: "proceed", canCommit: true));
		// no yaml file in MetadataDir — labels validated, comment should be deleted not updated
		var commentSvc = A.Fake<IGitHubCommentService>();
		A.CallTo(() => commentSvc.DeleteStickyCommentAsync(A<string>._, A<string>._, A<int>._, A<CancellationToken>._)).Returns(true);

		await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		A.CallTo(
			() => commentSvc.DeleteStickyCommentAsync("elastic", "test-repo", 42, A<CancellationToken>._)
		).MustHaveHappenedOnceExactly();
		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(A<string>._, A<string>._, A<int>._, A<string>._, A<CancellationToken>._)
		).MustNotHaveHappened();
	}

	[Test]
	public async Task PostComment_MetadataNotFound_ReturnsTrueWithoutCalling()
	{
		var commentSvc = A.Fake<IGitHubCommentService>();
		var args = DefaultArgs() with { MetadataPath = Path.Join(Root, "missing", "metadata.json") };

		var result = await CreateService(commentSvc).PostComment(args, CancellationToken.None);

		result.Should().BeTrue();
		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(A<string>._, A<string>._, A<int>._, A<string>._, A<CancellationToken>._)
		).MustNotHaveHappened();
	}

	[Test]
	public async Task PostComment_CommentServiceFails_ReturnsTrueAnyway()
	{
		await WriteMetadata(BaseMetadata(status: "no-label") with { LabelTable = "| label | type |" });
		var commentSvc = A.Fake<IGitHubCommentService>();
		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(A<string>._, A<string>._, A<int>._, A<string>._, A<CancellationToken>._)
		).Returns((string?)null);

		var result = await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		result.Should().BeTrue();
	}

	[Test]
	public async Task PostComment_Skipped_DeletesStickyComment()
	{
		await WriteMetadata(BaseMetadata(status: "skipped"));
		var commentSvc = A.Fake<IGitHubCommentService>();
		A.CallTo(() => commentSvc.DeleteStickyCommentAsync(A<string>._, A<string>._, A<int>._, A<CancellationToken>._)).Returns(true);

		await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		A.CallTo(
			() => commentSvc.DeleteStickyCommentAsync("elastic", "test-repo", 42, A<CancellationToken>._)
		).MustHaveHappenedOnceExactly();
		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(A<string>._, A<string>._, A<int>._, A<string>._, A<CancellationToken>._)
		).MustNotHaveHappened();
	}

	// ── Gate.Entries dispatch ──────────────────────────────────────────────────────────────────────

	[Test]
	public async Task PostComment_EntriesGateWithFindings_RendersEntriesInvalidBody()
	{
		await WriteMetadata(BaseMetadata(status: "entries-invalid") with
		{
			Gate = ValidationGate.Entries,
			EntryFindings = [new EntryFinding { File = "docs/changelog/42.yaml", Severity = "Error", Message = "title is required" }]
		});
		var commentSvc = A.Fake<IGitHubCommentService>();
		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(A<string>._, A<string>._, A<int>._, A<string>._, A<CancellationToken>._)
		).Returns((string?)"IC_test_node_id");

		await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(
				A<string>._,
				A<string>._,
				A<int>._,
				A<string>.That.Contains("validation failed"),
				A<CancellationToken>._
			)
		).MustHaveHappenedOnceExactly();
	}

	[Test]
	public async Task PostComment_EntriesGateNoFindings_DeletesStickyComment()
	{
		await WriteMetadata(BaseMetadata(status: "ok", canCommit: true) with { Gate = ValidationGate.Entries, EntryFindings = null });
		var commentSvc = A.Fake<IGitHubCommentService>();
		A.CallTo(() => commentSvc.DeleteStickyCommentAsync(A<string>._, A<string>._, A<int>._, A<CancellationToken>._)).Returns(true);

		await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		A.CallTo(
			() => commentSvc.DeleteStickyCommentAsync("elastic", "test-repo", 42, A<CancellationToken>._)
		).MustHaveHappenedOnceExactly();
	}

	// ── Gate.Onboarding dispatch ──────────────────────────────────────────────────────────────────

	[Test]
	public async Task PostComment_OnboardingGate_RendersRepositoryNotOnboardedBody()
	{
		await WriteMetadata(BaseMetadata(status: "onboarding-required") with { Gate = ValidationGate.Onboarding });
		var commentSvc = A.Fake<IGitHubCommentService>();
		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(A<string>._, A<string>._, A<int>._, A<string>._, A<CancellationToken>._)
		).Returns((string?)"IC_test_node_id");

		await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(
				A<string>._,
				A<string>._,
				A<int>._,
				A<string>.That.Contains("products.yml"),
				A<CancellationToken>._
			)
		).MustHaveHappenedOnceExactly();
	}

	// ── Gate.File + missing-entry dispatch ────────────────────────────────────────────────────────

	[Test]
	public async Task PostComment_FileGateMissingEntry_RendersMissingEntryBody()
	{
		await WriteMetadata(BaseMetadata(status: "missing-entry") with { Gate = ValidationGate.File, ChangelogDir = "docs/changelog" });
		var commentSvc = A.Fake<IGitHubCommentService>();
		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(A<string>._, A<string>._, A<int>._, A<string>._, A<CancellationToken>._)
		).Returns((string?)"IC_test_node_id");

		await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(
				A<string>._,
				A<string>._,
				A<int>._,
				A<string>.That.Contains("pending"),
				A<CancellationToken>._
			)
		).MustHaveHappenedOnceExactly();
	}

	private const string BreakingYaml =
		"type: breaking-change\ntitle: Test\nimpact: Users break.\naction: Upgrade now.\nproducts:\n  - product: elasticsearch\n";

	private static IGitHubCommentService CreateCommentFake()
	{
		var commentSvc = A.Fake<IGitHubCommentService>();
		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(A<string>._, A<string>._, A<int>._, A<string>._, A<CancellationToken>._)
		).Returns((string?)"IC_test_node_id");
		A.CallTo(() => commentSvc.DeleteStickyCommentAsync(A<string>._, A<string>._, A<int>._, A<CancellationToken>._)).Returns(true);
		return commentSvc;
	}

	[Test]
	public async Task PostComment_MissingImpactAction_RendersMissingSectionsBody()
	{
		await WriteMetadata(BaseMetadata(status: "missing-impact-action") with
		{
			Gate = ValidationGate.File,
			MissingSections = "impact,action"
		});
		var commentSvc = CreateCommentFake();

		await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(
				"elastic",
				"test-repo",
				42,
				A<string>.That.Contains("## Impact"),
				A<CancellationToken>._
			)
		).MustHaveHappenedOnceExactly();
	}

	[Test]
	public async Task PostComment_SuccessBreakingChange_KeepsCommentWithCapturedImpactAndAction()
	{
		await WriteMetadata(BaseMetadata(status: "proceed", canCommit: true));
		WriteYaml("42.yaml", BreakingYaml);
		var commentSvc = CreateCommentFake();

		await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(
				"elastic",
				"test-repo",
				42,
				A<string>.That.Matches(b => b.Contains("<details>") && b.Contains("Users break.") && b.Contains("Upgrade now.")),
				A<CancellationToken>._
			)
		).MustHaveHappenedOnceExactly();
		A.CallTo(
			() => commentSvc.DeleteStickyCommentAsync(A<string>._, A<string>._, A<int>._, A<CancellationToken>._)
		).MustNotHaveHappened();
	}

	[Test]
	public async Task PostComment_CommittedBreakingChange_AppendsCapturedBlock()
	{
		await WriteMetadata(BaseMetadata() with { CommitOutcome = CommitOutcome.Committed, CommittedFile = "docs/changelog/42.yaml" });
		WriteYaml("42.yaml", BreakingYaml);
		var commentSvc = CreateCommentFake();

		await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(
				A<string>._,
				A<string>._,
				A<int>._,
				A<string>.That.Matches(b => b.Contains("docs/changelog/42.yaml") && b.Contains("<details>") && b.Contains("Users break.")),
				A<CancellationToken>._
			)
		).MustHaveHappenedOnceExactly();
	}

	[Test]
	public async Task PostComment_SuccessNonBreakingStagedEntry_StillDeletesComment()
	{
		await WriteMetadata(BaseMetadata(status: "proceed", canCommit: true));
		WriteYaml("42.yaml", "type: feature\ntitle: Test\nproducts:\n  - product: elasticsearch\n");
		var commentSvc = CreateCommentFake();

		await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		A.CallTo(
			() => commentSvc.DeleteStickyCommentAsync("elastic", "test-repo", 42, A<CancellationToken>._)
		).MustHaveHappenedOnceExactly();
	}

	[Test]
	public async Task PostComment_GateOnlyBreakingChange_EchoesMetadataWithoutStagedYaml()
	{
		await WriteMetadata(BaseMetadata(status: "proceed", canCommit: false) with
		{
			Gate = ValidationGate.File,
			ResolvedType = "breaking-change",
			Impact = "Users break.",
			Action = "Upgrade now."
		});
		var commentSvc = CreateCommentFake();

		await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(
				"elastic",
				"test-repo",
				42,
				A<string>.That.Matches(
					b => b.Contains("Users break.") && b.Contains("Upgrade now.") && b.Contains("found in the PR description") &&
						!b.Contains("generated") && !b.Contains("could not be read")
				),
				A<CancellationToken>._
			)
		).MustHaveHappenedOnceExactly();
	}

	[Test]
	public async Task PostComment_ConflictingImpactAction_RendersConflictBody()
	{
		await WriteMetadata(BaseMetadata(status: "conflicting-impact-action") with
		{
			Gate = ValidationGate.File,
			ChangelogDir = "docs/changelog",
			ChangelogFilename = "42.yaml",
			ConflictingSections = "impact"
		});
		var commentSvc = CreateCommentFake();

		await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(
				"elastic",
				"test-repo",
				42,
				A<string>.That.Matches(b => b.Contains("docs/changelog/42.yaml") && b.Contains("## Impact") && !b.Contains("## Action")),
				A<CancellationToken>._
			)
		).MustHaveHappenedOnceExactly();
	}

	[Test]
	public async Task PostComment_BreakingChangeWithNothingScraped_DoesNotEcho()
	{
		await WriteMetadata(BaseMetadata(status: "proceed", canCommit: false) with
		{
			Gate = ValidationGate.File,
			ResolvedType = "breaking-change"
		});
		var commentSvc = CreateCommentFake();

		await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(
				A<string>._,
				A<string>._,
				A<int>._,
				A<string>.That.Contains("<details>"),
				A<CancellationToken>._
			)
		).MustNotHaveHappened();
	}

	[Test]
	public async Task PostComment_StagedBreakingEntry_WinsOverMetadataValues()
	{
		await WriteMetadata(BaseMetadata(status: "proceed", canCommit: true) with
		{
			Gate = ValidationGate.File,
			ResolvedType = "breaking-change",
			Impact = "Scraped impact.",
			Action = "Scraped action."
		});
		WriteYaml("42.yaml", BreakingYaml);
		var commentSvc = CreateCommentFake();

		await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(
				"elastic",
				"test-repo",
				42,
				A<string>.That.Matches(b => b.Contains("Users break.") && b.Contains("Upgrade now.") && !b.Contains("Scraped")),
				A<CancellationToken>._
			)
		).MustHaveHappenedOnceExactly();
	}

	[Test]
	public async Task PostComment_ForkCommentOnlyBreakingEntry_KeepsYamlPreviewAndAppendsEcho()
	{
		await WriteMetadata(BaseMetadata(status: "proceed", canCommit: false, isFork: true) with
		{
			ChangelogDir = "docs/changelog",
			ResolvedType = "breaking-change",
			Impact = "Scraped impact.",
			Action = "Scraped action."
		});
		WriteYaml("42.yaml", BreakingYaml);
		var commentSvc = CreateCommentFake();

		await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(
				"elastic",
				"test-repo",
				42,
				A<string>.That.Matches(
					b => b.Contains("Generated changelog entry") && b.Contains("<details>") && b.Contains("Users break.")
				),
				A<CancellationToken>._
			)
		).MustHaveHappenedOnceExactly();
	}

	[Test]
	public async Task PostComment_ManuallyEdited_DeletesStaleComment()
	{
		await WriteMetadata(BaseMetadata(status: "manually-edited") with { Gate = ValidationGate.File });
		var commentSvc = CreateCommentFake();

		await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		A.CallTo(
			() => commentSvc.DeleteStickyCommentAsync("elastic", "test-repo", 42, A<CancellationToken>._)
		).MustHaveHappenedOnceExactly();
	}

	[Test]
	public async Task PostComment_GateOnlySuccess_DeletesCommentInsteadOfClaimingAnEntryWasGenerated()
	{
		await WriteMetadata(BaseMetadata(status: "proceed", canCommit: false) with
		{
			Gate = ValidationGate.File,
			ResolvedType = "feature"
		});
		var commentSvc = CreateCommentFake();

		await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(A<string>._, A<string>._, A<int>._, A<string>._, A<CancellationToken>._)
		).MustNotHaveHappened();
		A.CallTo(
			() => commentSvc.DeleteStickyCommentAsync("elastic", "test-repo", 42, A<CancellationToken>._)
		).MustHaveHappenedOnceExactly();
	}

	[Test]
	public async Task WriteMetadata_ConfigFileInsideTheCheckout_IsStoredRepoRelative()
	{
		await WriteMetadata(BaseMetadata() with { ConfigFile = Path.Join(Root, "docs", "changelog.yml") });

		var written = await new GithubDecisionMetadataWriter(LoggerFactory, RunnerTempFileSystem).ReadAsync(
			MetadataPath,
			CancellationToken.None
		);

		written!.ConfigFile.Should().Be("docs/changelog.yml");
	}

	[Test]
	public async Task PostComment_StagedBreakingEntry_StillSaysTheEntryWasGenerated()
	{
		await WriteMetadata(BaseMetadata(status: "proceed", canCommit: true));
		WriteYaml("42.yaml", BreakingYaml);
		var commentSvc = CreateCommentFake();

		await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(
				"elastic",
				"test-repo",
				42,
				A<string>.That.Contains("Changelog entry generated for this breaking change"),
				A<CancellationToken>._
			)
		).MustHaveHappenedOnceExactly();
	}

	private static ReleaseNoteOutcome IncludedOutcome() =>
		new() { Included = true, Type = "feature", Products = "elasticsearch", Areas = "Search, Aggregations" };

	private static void MustHaveUpserted(IGitHubCommentService commentSvc, Func<string, bool> bodyMatches) =>
		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(
				"elastic",
				"test-repo",
				42,
				A<string>.That.Matches(b => bodyMatches(b)),
				A<CancellationToken>._
			)
		).MustHaveHappenedOnceExactly();

	[Test]
	public async Task PostComment_OutcomeRecordedButOptInOff_StillDeletesStickyComment()
	{
		await WriteMetadata(BaseMetadata(status: "ok") with { Gate = ValidationGate.Labels, Outcome = IncludedOutcome() });
		var commentSvc = CreateCommentFake();

		await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		A.CallTo(
			() => commentSvc.DeleteStickyCommentAsync("elastic", "test-repo", 42, A<CancellationToken>._)
		).MustHaveHappenedOnceExactly();
		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(A<string>._, A<string>._, A<int>._, A<string>._, A<CancellationToken>._)
		).MustNotHaveHappened();
	}

	[Test]
	public async Task PostComment_OptInAndIncluded_RendersTypeProductsAndAreas()
	{
		await WriteMetadata(BaseMetadata(status: "ok") with
		{
			Gate = ValidationGate.Labels,
			CommentOnSuccess = true,
			Outcome = IncludedOutcome()
		});
		var commentSvc = CreateCommentFake();

		await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		MustHaveUpserted(
			commentSvc,
			b => b.Contains("will be part of the release notes") && b.Contains("`feature`") && b.Contains("`elasticsearch`") && b.Contains(
				"`Search, Aggregations`"
			)
		);
		A.CallTo(
			() => commentSvc.DeleteStickyCommentAsync(A<string>._, A<string>._, A<int>._, A<CancellationToken>._)
		).MustNotHaveHappened();
	}

	[Test]
	public async Task PostComment_OptInAndSkipped_RendersExclusionReason()
	{
		await WriteMetadata(BaseMetadata(status: "skipped") with
		{
			CommentOnSuccess = true,
			Outcome = new ReleaseNoteOutcome { Included = false, Reason = "it has the label changelog:skip" }
		});
		var commentSvc = CreateCommentFake();

		await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		MustHaveUpserted(
			commentSvc,
			b => b.Contains("will not be part of the release notes") && b.Contains("it has the label changelog:skip")
		);
	}

	[Test]
	public async Task PostComment_OptInAndBreakingChange_PostsOneCommentWithOutcomeAndImpactAction()
	{
		await WriteMetadata(BaseMetadata(status: "proceed", canCommit: false) with
		{
			Gate = ValidationGate.File,
			CommentOnSuccess = true,
			ResolvedType = "breaking-change",
			Impact = "Users break.",
			Action = "Upgrade now.",
			Outcome = new ReleaseNoteOutcome { Included = true, Type = "breaking-change" }
		});
		var commentSvc = CreateCommentFake();

		await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		MustHaveUpserted(
			commentSvc,
			b => b.Contains("will be part of the release notes") && b.Contains("`breaking-change`") && b.Contains(
				"Users break."
			) && b.Contains("Upgrade now.")
		);
	}

	[Test]
	public async Task PostComment_OptInButCheckFailed_RendersFailureNotOutcome()
	{
		await WriteMetadata(BaseMetadata(status: "no-label") with
		{
			LabelTable = "| label | type |",
			CommentOnSuccess = true,
			Outcome = IncludedOutcome()
		});
		var commentSvc = CreateCommentFake();

		await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		MustHaveUpserted(commentSvc, b => !b.Contains("will be part of the release notes"));
	}

	[Test]
	public async Task WriteAsync_LaterGateWithoutOutcome_KeepsOutcomeAndOptIn()
	{
		var writer = new GithubDecisionMetadataWriter(LoggerFactory, RunnerTempFileSystem);
		await WriteMetadata(BaseMetadata(status: "ok") with
		{
			Gate = ValidationGate.Labels,
			CommentOnSuccess = true,
			Outcome = IncludedOutcome()
		});

		await WriteMetadata(BaseMetadata(status: "ok") with { Gate = ValidationGate.Entries });

		var stored = await writer.ReadAsync(MetadataPath, CancellationToken.None);
		stored!.Gate.Should().Be(ValidationGate.Entries);
		stored.CommentOnSuccess.Should().BeTrue();
		stored.Outcome.Should().BeEquivalentTo(IncludedOutcome());
	}

	[Test]
	public async Task WriteAsync_OutcomeFromOtherPr_IsNotCarriedForward()
	{
		var writer = new GithubDecisionMetadataWriter(LoggerFactory, RunnerTempFileSystem);
		await WriteMetadata(BaseMetadata(status: "ok") with { CommentOnSuccess = true, Outcome = IncludedOutcome() });

		await WriteMetadata(BaseMetadata(status: "ok") with { PrNumber = 43 });

		var stored = await writer.ReadAsync(MetadataPath, CancellationToken.None);
		stored!.Outcome.Should().BeNull();
		stored.CommentOnSuccess.Should().BeFalse();
	}

	[Test]
	public async Task PostComment_NoLabelFromFileGate_RendersLabelsNeededBodyWithAreaTable()
	{
		await WriteMetadata(BaseMetadata(status: "no-label") with
		{
			Gate = ValidationGate.File,
			AreaLabelTable = "| `:Search` | Search |"
		});
		var commentSvc = A.Fake<IGitHubCommentService>();
		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(A<string>._, A<string>._, A<int>._, A<string>._, A<CancellationToken>._)
		).Returns((string?)"IC_test_node_id");
		A.CallTo(() => commentSvc.MinimizeCommentAsync(A<string>._, A<CancellationToken>._)).Returns(true);
		A.CallTo(() => commentSvc.UnminimizeCommentAsync(A<string>._, A<CancellationToken>._)).Returns(true);

		await CreateService(commentSvc).PostComment(DefaultArgs(), CancellationToken.None);

		A.CallTo(
			() => commentSvc.UpsertStickyCommentAsync(
				A<string>._,
				A<string>._,
				A<int>._,
				A<string>.That.Contains("Area label needed"),
				A<CancellationToken>._
			)
		).MustHaveHappenedOnceExactly();
	}
}
