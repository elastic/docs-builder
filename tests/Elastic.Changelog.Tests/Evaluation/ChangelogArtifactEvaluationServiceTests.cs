// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Actions.Core.Services;
using AwesomeAssertions;
using Elastic.Changelog.Evaluation;
using Elastic.Changelog.GitHub;
using Elastic.Changelog.Tests.Changelogs;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.ReleaseNotes;
using FakeItEasy;

namespace Elastic.Changelog.Tests.Evaluation;

public class ChangelogArtifactEvaluationServiceTests(ITestOutputHelper output) : ChangelogTestBase(output)
{
	private readonly IGitHubPrService _mockGitHub = A.Fake<IGitHubPrService>();
	private readonly ICoreService _mockCore = A.Fake<ICoreService>();

	private static readonly string Root = Paths.WorkingDirectoryRoot.FullName;
	private static readonly string MetadataFilePath = Path.Join(Root, "artifact/metadata.json");

	private ChangelogArtifactEvaluationService CreateService() =>
		new(LoggerFactory, _mockGitHub, _mockCore, FileSystem);

	private static EvaluateArtifactArguments DefaultArgs() =>
		new()
		{
			MetadataPath = MetadataFilePath,
			Owner = "elastic",
			Repo = "test-repo"
		};

	private async Task WriteMetadata(ChangelogArtifactMetadata metadata, string? path = null)
	{
		path ??= MetadataFilePath;
		var dir = FileSystem.Path.GetDirectoryName(path)!;
		FileSystem.Directory.CreateDirectory(dir);
		var json = JsonSerializer.Serialize(metadata, ChangelogArtifactMetadataJsonContext.Default.ChangelogArtifactMetadata);
		await FileSystem.File.WriteAllTextAsync(path, json);
	}

	private static ChangelogArtifactMetadata DefaultMetadata(
		string status = "success",
		string? changelogFilename = "42.yaml",
		bool canCommit = true
	) =>
		new()
		{
			PrNumber = 42,
			HeadRef = "feature/test",
			HeadSha = "abc123",
			Status = status,
			IsFork = false,
			CanCommit = canCommit,
			MaintainerCanModify = false,
			ConfigFile = "docs/changelog.yml",
			ChangelogDir = "changelogs",
			ChangelogFilename = changelogFilename,
			CreateRules = new CreateRules { Labels = ["changelog:skip"], Mode = FieldMode.Exclude }
		};

	private void SetupPrInfo(string headSha = "abc123", bool isFork = false, string[]? labels = null) =>
		A.CallTo(() => _mockGitHub.FetchPrInfoAsync("42", "elastic", "test-repo", A<CancellationToken>._))
			.Returns(new GitHubPrInfo
			{
				Title = "Test PR",
				HeadSha = headSha,
				HeadRef = "feature/test",
				IsFork = isFork,
				Labels = labels ?? ["type:feature"]
			});

	private void VerifyOutputSet(string name, string value) =>
		A.CallTo(() => _mockCore.SetOutputAsync(name, value)).MustHaveHappened();

	[Fact]
	public async Task EvaluateArtifact_MissingMetadata_ReturnsTrue()
	{
		var service = CreateService();

		var result = await service.EvaluateArtifact(Collector, DefaultArgs(), CancellationToken.None);

		result.Should().BeTrue();
		A.CallTo(() => _mockGitHub.FetchPrInfoAsync(A<string>._, A<string>._, A<string>._, A<CancellationToken>._))
			.MustNotHaveHappened();
	}

	[Fact]
	public async Task EvaluateArtifact_FetchPrFails_ReturnsFalse()
	{
		await WriteMetadata(DefaultMetadata());
		A.CallTo(() => _mockGitHub.FetchPrInfoAsync("42", "elastic", "test-repo", A<CancellationToken>._))
			.Returns((GitHubPrInfo?)null);

		var service = CreateService();
		var result = await service.EvaluateArtifact(Collector, DefaultArgs(), CancellationToken.None);

		result.Should().BeFalse();
	}

	[Fact]
	public async Task EvaluateArtifact_HeadShaMoved_ReturnsTrueWithoutSettingFlags()
	{
		await WriteMetadata(DefaultMetadata());
		SetupPrInfo(headSha: "different-sha");

		var service = CreateService();
		var result = await service.EvaluateArtifact(Collector, DefaultArgs(), CancellationToken.None);

		result.Should().BeTrue();
		A.CallTo(() => _mockCore.SetOutputAsync("should-commit", A<string>._)).MustNotHaveHappened();
	}

	[Fact]
	public async Task EvaluateArtifact_AllProductsBlocked_ReturnsTrueGracefully()
	{
		var metadata = DefaultMetadata() with
		{
			CreateRules = new CreateRules { Labels = ["changelog:skip"], Mode = FieldMode.Exclude }
		};
		await WriteMetadata(metadata);
		SetupPrInfo(labels: ["changelog:skip", "type:feature"]);

		var service = CreateService();
		var result = await service.EvaluateArtifact(Collector, DefaultArgs(), CancellationToken.None);

		result.Should().BeTrue();
		A.CallTo(() => _mockCore.SetOutputAsync("should-commit", A<string>._)).MustNotHaveHappened();
	}

	[Fact]
	public async Task EvaluateArtifact_SuccessCanCommit_SetsCommitFlag()
	{
		await WriteMetadata(DefaultMetadata(canCommit: true));
		SetupPrInfo();

		var service = CreateService();
		var result = await service.EvaluateArtifact(Collector, DefaultArgs(), CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("should-commit", "true");
		VerifyOutputSet("should-comment-success", "false");
		VerifyOutputSet("should-comment-failure", "false");
		VerifyOutputSet("pr-number", "42");
		VerifyOutputSet("head-ref", "feature/test");
		VerifyOutputSet("head-sha", "abc123");
		VerifyOutputSet("status", "success");
		VerifyOutputSet("changelog-filename", "42.yaml");
	}

	[Fact]
	public async Task EvaluateArtifact_SuccessCannotCommit_SetsCommentSuccessFlag()
	{
		await WriteMetadata(DefaultMetadata(canCommit: false));
		SetupPrInfo();

		var service = CreateService();
		var result = await service.EvaluateArtifact(Collector, DefaultArgs(), CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("should-commit", "false");
		VerifyOutputSet("should-comment-success", "true");
	}

	[Fact]
	public async Task EvaluateArtifact_ForkCanCommit_SetsCommitFlag()
	{
		var metadata = DefaultMetadata(canCommit: true) with
		{
			IsFork = true,
			HeadRepo = "contributor/repo",
			MaintainerCanModify = true
		};
		await WriteMetadata(metadata);
		SetupPrInfo();

		var service = CreateService();
		var result = await service.EvaluateArtifact(Collector, DefaultArgs(), CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("should-commit", "true");
		VerifyOutputSet("is-fork", "true");
		VerifyOutputSet("head-repo", "contributor/repo");
	}

	[Fact]
	public async Task EvaluateArtifact_ForkCannotCommit_SetsCommentSuccessFlag()
	{
		var metadata = DefaultMetadata(canCommit: false) with
		{
			IsFork = true,
			HeadRepo = "contributor/repo",
			MaintainerCanModify = false
		};
		await WriteMetadata(metadata);
		SetupPrInfo();

		var service = CreateService();
		var result = await service.EvaluateArtifact(Collector, DefaultArgs(), CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("should-commit", "false");
		VerifyOutputSet("should-comment-success", "true");
		VerifyOutputSet("is-fork", "true");
	}

	[Fact]
	public async Task EvaluateArtifact_NoLabel_SetsCommentFailureFlag()
	{
		await WriteMetadata(DefaultMetadata(status: "no-label", canCommit: false) with
		{
			LabelTable = "| Label | Type |",
			ProductLabelTable = "| Label | Product |",
			SkipLabels = "changelog:skip"
		});
		SetupPrInfo();

		var service = CreateService();
		var result = await service.EvaluateArtifact(Collector, DefaultArgs(), CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("should-commit", "false");
		VerifyOutputSet("should-comment-failure", "true");
		VerifyOutputSet("label-table", "| Label | Type |");
		VerifyOutputSet("product-label-table", "| Label | Product |");
		VerifyOutputSet("skip-labels", "changelog:skip");
	}

	[Fact]
	public async Task EvaluateArtifact_TimestampFilename_OutputsOriginalFilename()
	{
		await WriteMetadata(DefaultMetadata(changelogFilename: "1735689600-fix-search.yaml"));
		SetupPrInfo();

		var service = CreateService();
		await service.EvaluateArtifact(Collector, DefaultArgs(), CancellationToken.None);

		VerifyOutputSet("changelog-filename", "1735689600-fix-search.yaml");
	}
}
