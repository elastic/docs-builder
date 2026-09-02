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

public class ChangelogGithubCommentServiceTests(ITestOutputHelper output) : ChangelogTestBase(output)
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

	[Fact]
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

	[Fact]
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

	[Fact]
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

	[Fact]
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

	[Fact]
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

	[Fact]
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

	[Fact]
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

	[Fact]
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

	[Fact]
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

	[Fact]
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

	// ── Gate.File + missing-entry dispatch ────────────────────────────────────────────────────────

	[Fact]
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
				A<string>.That.Contains("entry file required"),
				A<CancellationToken>._
			)
		).MustHaveHappenedOnceExactly();
	}
}
