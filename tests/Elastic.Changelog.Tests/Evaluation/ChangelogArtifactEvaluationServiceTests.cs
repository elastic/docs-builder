// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Actions.Core.Services;
using Elastic.Changelog.Evaluation;
using Elastic.Changelog.GitHub;
using Elastic.Changelog.Tests.Changelogs;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.ReleaseNotes;
using FakeItEasy;
using FluentAssertions;

namespace Elastic.Changelog.Tests.Evaluation;

public class ChangelogArtifactEvaluationServiceTests(ITestOutputHelper output) : ChangelogTestBase(output)
{
	private readonly IGitHubPrService _mockGitHub = A.Fake<IGitHubPrService>();
	private readonly ICoreService _mockCore = A.Fake<ICoreService>();

	private ChangelogArtifactEvaluationService CreateService() =>
		new(LoggerFactory, _mockGitHub, _mockCore, FileSystem);

	private static EvaluateArtifactArguments DefaultArgs() =>
		new()
		{
			MetadataPath = "/artifact/metadata.json",
			Owner = "elastic",
			Repo = "test-repo"
		};

	private async Task WriteMetadata(ChangelogArtifactMetadata metadata, string path = "/artifact/metadata.json")
	{
		var dir = FileSystem.Path.GetDirectoryName(path)!;
		FileSystem.Directory.CreateDirectory(dir);
		var json = JsonSerializer.Serialize(metadata, ChangelogArtifactMetadataJsonContext.Default.ChangelogArtifactMetadata);
		await FileSystem.File.WriteAllTextAsync(path, json);
	}

	private static ChangelogArtifactMetadata DefaultMetadata(string status = "success") =>
		new()
		{
			PrNumber = 42,
			HeadRef = "feature/test",
			HeadSha = "abc123",
			Status = status,
			ConfigFile = "docs/changelog.yml",
			ChangelogDir = "changelogs",
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
	public async Task EvaluateArtifact_Success_SetsCommitFlag()
	{
		await WriteMetadata(DefaultMetadata());
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
	}

	[Fact]
	public async Task EvaluateArtifact_SuccessFork_SetsCommentSuccessFlag()
	{
		await WriteMetadata(DefaultMetadata());
		SetupPrInfo(isFork: true);

		var service = CreateService();
		var result = await service.EvaluateArtifact(Collector, DefaultArgs(), CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("should-commit", "false");
		VerifyOutputSet("should-comment-success", "true");
	}

	[Fact]
	public async Task EvaluateArtifact_SuccessCommentOnly_SetsCommentSuccessFlag()
	{
		await WriteMetadata(DefaultMetadata());
		SetupPrInfo();

		var service = CreateService();
		var args = DefaultArgs() with { CommentOnly = true };
		var result = await service.EvaluateArtifact(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("should-commit", "false");
		VerifyOutputSet("should-comment-success", "true");
	}

	[Fact]
	public async Task EvaluateArtifact_NoLabel_SetsCommentFailureFlag()
	{
		await WriteMetadata(DefaultMetadata(status: "no-label") with { LabelTable = "| Label | Type |" });
		SetupPrInfo();

		var service = CreateService();
		var result = await service.EvaluateArtifact(Collector, DefaultArgs(), CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("should-commit", "false");
		VerifyOutputSet("should-comment-failure", "true");
		VerifyOutputSet("label-table", "| Label | Type |");
	}

}
