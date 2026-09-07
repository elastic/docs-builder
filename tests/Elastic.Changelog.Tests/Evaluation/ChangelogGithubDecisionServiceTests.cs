// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.Changelog.Evaluation;
using Elastic.Changelog.Tests.Changelogs;
using Elastic.Documentation.Configuration;

namespace Elastic.Changelog.Tests.Evaluation;

public class ChangelogGithubDecisionServiceTests(ITestOutputHelper output) : ChangelogTestBase(output)
{
	private static readonly string Root = Paths.WorkingDirectoryRoot.FullName;

	private string MetadataPath => Path.Join(Root, GithubDecisionMetadataWriter.ArtifactDir, GithubDecisionMetadataWriter.MetadataFilename);

	private ChangelogGithubDecisionService CreateService() => new(LoggerFactory, RunnerTempFileSystem);

	private async Task WriteMetadata(GithubDecisionMetadata metadata)
	{
		var writer = new GithubDecisionMetadataWriter(LoggerFactory, RunnerTempFileSystem);
		await writer.WriteAsync(metadata, CancellationToken.None);
	}

	private async Task<GithubDecisionMetadata?> ReadMetadata()
	{
		var writer = new GithubDecisionMetadataWriter(LoggerFactory, RunnerTempFileSystem);
		return await writer.ReadAsync(MetadataPath, CancellationToken.None);
	}

	private static GithubDecisionMetadata BaseMetadata(int prNumber = 42) =>
		new()
		{
			PrNumber = prNumber,
			HeadRef = "feature/test",
			HeadSha = "abc123",
			Status = "proceed",
			IsFork = false,
			CanCommit = true,
			MaintainerCanModify = false
		};

	[Fact]
	public async Task RecordDecision_MetadataExists_UpdatesCommitOutcomeAndFile()
	{
		await WriteMetadata(BaseMetadata());
		var service = CreateService();
		var args = new GithubDecisionArguments
		{
			MetadataPath = MetadataPath,
			CommitOutcome = CommitOutcome.Committed,
			CommittedFile = "docs/changelog/42.yaml"
		};

		var result = await service.RecordDecision(args, CancellationToken.None);

		result.Should().BeTrue();
		var updated = await ReadMetadata();
		updated.Should().NotBeNull();
		updated!.CommitOutcome.Should().Be(CommitOutcome.Committed);
		updated.CommittedFile.Should().Be("docs/changelog/42.yaml");
	}

	[Fact]
	public async Task RecordDecision_MetadataNotFound_ReturnsTrueWithoutCrashing()
	{
		var service = CreateService();
		var args = new GithubDecisionArguments
		{
			MetadataPath = Path.Join(Root, "nonexistent", "metadata.json"),
			CommitOutcome = CommitOutcome.Failed
		};

		var result = await service.RecordDecision(args, CancellationToken.None);

		result.Should().BeTrue();
	}

	[Fact]
	public async Task RecordDecision_Roundtrip_PreservesAllOtherFields()
	{
		var original = BaseMetadata(prNumber: 99) with
		{
			HeadRef = "my-branch",
			HeadSha = "def456",
			IsFork = true,
			CanCommit = false,
			LabelTable = "| label | type |",
			SkipLabels = "changelog:skip"
		};
		await WriteMetadata(original);

		var service = CreateService();
		await service.RecordDecision(
			new GithubDecisionArguments
			{
				MetadataPath = MetadataPath,
				CommitOutcome = CommitOutcome.Committed,
				CommittedFile = "docs/changelog/99.yaml"
			},
			CancellationToken.None
		);

		var updated = await ReadMetadata();
		updated!.PrNumber.Should().Be(99);
		updated.HeadRef.Should().Be("my-branch");
		updated.HeadSha.Should().Be("def456");
		updated.IsFork.Should().BeTrue();
		updated.CanCommit.Should().BeFalse();
		updated.LabelTable.Should().Be("| label | type |");
		updated.SkipLabels.Should().Be("changelog:skip");
	}
}
