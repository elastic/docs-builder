// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Actions.Core.Services;
using Elastic.Changelog.Evaluation;
using Elastic.Changelog.Tests.Changelogs;
using Elastic.Documentation.ReleaseNotes;
using FakeItEasy;
using FluentAssertions;

namespace Elastic.Changelog.Tests.Evaluation;

public class ChangelogPrepareArtifactServiceTests(ITestOutputHelper output) : ChangelogTestBase(output)
{
	private readonly ICoreService _mockCore = A.Fake<ICoreService>();

	private const string MinimalConfig = """
		pivot:
		  types:
		    feature: "type:feature"
		    bug-fix: "type:bug"
		    breaking-change: "type:breaking"
		    enhancement:
		    deprecation:
		    docs:
		    known-issue:
		    other:
		    regression:
		    security:
		rules:
		  create:
		    exclude: "changelog:skip"
		""";

	private ChangelogPrepareArtifactService CreateService() =>
		new(LoggerFactory, ConfigurationContext, _mockCore, FileSystem);

	private PrepareArtifactArguments DefaultArgs(
		string evaluateStatus = "proceed",
		string generateOutcome = "success",
		string? config = null
	) =>
		new()
		{
			StagingDir = "/staging",
			OutputDir = "/output",
			EvaluateStatus = evaluateStatus,
			GenerateOutcome = generateOutcome,
			PrNumber = 42,
			HeadRef = "feature/test",
			HeadSha = "abc123",
			LabelTable = null,
			Config = config ?? "/config/changelog.yml"
		};

	private async Task SetupStagingYaml(string? filename = null)
	{
		FileSystem.Directory.CreateDirectory("/staging");
		filename ??= "42.yaml";
		await FileSystem.File.WriteAllTextAsync($"/staging/{filename}", "title: test changelog");
	}

	private async Task SetupConfig(string configPath = "/config/changelog.yml")
	{
		var dir = FileSystem.Path.GetDirectoryName(configPath)!;
		FileSystem.Directory.CreateDirectory(dir);
		await FileSystem.File.WriteAllTextAsync(configPath, MinimalConfig);
	}

	private ChangelogArtifactMetadata ReadMetadata()
	{
		var json = FileSystem.File.ReadAllText("/output/metadata.json");
		return JsonSerializer.Deserialize(json, ChangelogArtifactMetadataJsonContext.Default.ChangelogArtifactMetadata)!;
	}

	[Fact]
	public async Task PrepareArtifact_GenerateSuccess_CopiesYamlAndWritesMetadata()
	{
		await SetupStagingYaml();
		await SetupConfig();
		var service = CreateService();

		var result = await service.PrepareArtifact(Collector, DefaultArgs(), CancellationToken.None);

		result.Should().BeTrue();
		FileSystem.File.Exists("/output/42.yaml").Should().BeTrue();
		var metadata = ReadMetadata();
		metadata.Status.Should().Be("success");
		metadata.PrNumber.Should().Be(42);
		metadata.HeadRef.Should().Be("feature/test");
		metadata.HeadSha.Should().Be("abc123");
		metadata.ChangelogFilename.Should().Be("42.yaml");
		A.CallTo(() => _mockCore.SetOutputAsync("status", "success")).MustHaveHappened();
	}

	[Fact]
	public async Task PrepareArtifact_ForkFields_PersistedInMetadata()
	{
		await SetupStagingYaml();
		await SetupConfig();
		var service = CreateService();
		var args = DefaultArgs() with
		{
			IsFork = true,
			HeadRepo = "contributor/repo",
			CanCommit = true,
			MaintainerCanModify = true
		};

		await service.PrepareArtifact(Collector, args, CancellationToken.None);

		var metadata = ReadMetadata();
		metadata.IsFork.Should().BeTrue();
		metadata.HeadRepo.Should().Be("contributor/repo");
		metadata.CanCommit.Should().BeTrue();
		metadata.MaintainerCanModify.Should().BeTrue();
	}

	[Fact]
	public async Task PrepareArtifact_ForkNoMaintainerEdits_CanCommitFalse()
	{
		await SetupStagingYaml();
		await SetupConfig();
		var service = CreateService();
		var args = DefaultArgs() with
		{
			IsFork = true,
			HeadRepo = "contributor/repo",
			CanCommit = false,
			MaintainerCanModify = false
		};

		await service.PrepareArtifact(Collector, args, CancellationToken.None);

		var metadata = ReadMetadata();
		metadata.IsFork.Should().BeTrue();
		metadata.CanCommit.Should().BeFalse();
		metadata.MaintainerCanModify.Should().BeFalse();
	}

	[Fact]
	public async Task PrepareArtifact_ProductLabelTableAndSkipLabels_PersistedInMetadata()
	{
		await SetupStagingYaml();
		await SetupConfig();
		var service = CreateService();
		var args = DefaultArgs() with
		{
			ProductLabelTable = "| Label | Product |\n| --- | --- |",
			SkipLabels = "changelog:skip,skip-ci"
		};

		await service.PrepareArtifact(Collector, args, CancellationToken.None);

		var metadata = ReadMetadata();
		metadata.ProductLabelTable.Should().Contain("Product");
		metadata.SkipLabels.Should().Be("changelog:skip,skip-ci");
	}

	[Fact]
	public async Task PrepareArtifact_GenerateFailure_StatusError()
	{
		await SetupConfig();
		var service = CreateService();
		var args = DefaultArgs(evaluateStatus: "proceed", generateOutcome: "failure");

		var result = await service.PrepareArtifact(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		FileSystem.File.Exists("/output/42.yaml").Should().BeFalse();
		var metadata = ReadMetadata();
		metadata.Status.Should().Be("error");
	}

	[Fact]
	public async Task PrepareArtifact_NoLabel_WritesMetadataWithoutYaml()
	{
		await SetupConfig();
		var service = CreateService();
		var args = DefaultArgs(evaluateStatus: "no-label") with { LabelTable = "| Label | Type |\n| --- | --- |" };

		var result = await service.PrepareArtifact(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		FileSystem.File.Exists("/output/42.yaml").Should().BeFalse();
		var metadata = ReadMetadata();
		metadata.Status.Should().Be("no-label");
		metadata.LabelTable.Should().Contain("Label");
		metadata.ChangelogFilename.Should().BeNull();
	}

	[Fact]
	public async Task PrepareArtifact_MetadataContainsCreateRules()
	{
		await SetupStagingYaml();
		await SetupConfig();
		var service = CreateService();

		await service.PrepareArtifact(Collector, DefaultArgs(), CancellationToken.None);

		var metadata = ReadMetadata();
		metadata.CreateRules.Should().NotBeNull();
		metadata.CreateRules!.Labels.Should().Contain("changelog:skip");
		metadata.CreateRules.Mode.Should().Be(FieldMode.Exclude);
	}

	[Fact]
	public async Task PrepareArtifact_ExistingFilename_RenamesStagingFileToMatch()
	{
		await SetupStagingYaml("1735700000-new-title.yaml");
		await SetupConfig();
		var service = CreateService();
		var args = DefaultArgs() with { ExistingChangelogFilename = "1735689600-old-title.yaml" };

		var result = await service.PrepareArtifact(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		FileSystem.File.Exists("/output/1735689600-old-title.yaml").Should().BeTrue();
		FileSystem.File.Exists("/output/1735700000-new-title.yaml").Should().BeFalse();
		var metadata = ReadMetadata();
		metadata.ChangelogFilename.Should().Be("1735689600-old-title.yaml");
	}

	[Fact]
	public async Task PrepareArtifact_MissingStagingYaml_StatusError()
	{
		await SetupConfig();
		var service = CreateService();

		await service.PrepareArtifact(Collector, DefaultArgs(), CancellationToken.None);

		var metadata = ReadMetadata();
		metadata.Status.Should().Be("error");
	}

	[Theory]
	[InlineData("proceed", "success", PrEvaluationResult.Success)]
	[InlineData("proceed", "failure", PrEvaluationResult.Error)]
	[InlineData("no-label", "success", PrEvaluationResult.NoLabel)]
	[InlineData("no-title", "success", PrEvaluationResult.NoTitle)]
	[InlineData("skipped", "success", PrEvaluationResult.Skipped)]
	[InlineData("manually-edited", "success", PrEvaluationResult.ManuallyEdited)]
	[InlineData("unknown", "success", PrEvaluationResult.Error)]
	public void ResolveStatus_ReturnsExpected(string evaluateStatus, string generateOutcome, PrEvaluationResult expected)
	{
		var result = ChangelogPrepareArtifactService.ResolveStatus(evaluateStatus, generateOutcome);
		result.Should().Be(expected);
	}
}
