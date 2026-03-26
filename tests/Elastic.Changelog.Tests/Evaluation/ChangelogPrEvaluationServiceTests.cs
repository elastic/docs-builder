// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Actions.Core.Services;
using AwesomeAssertions;
using Elastic.Changelog.Evaluation;
using Elastic.Changelog.GitHub;
using Elastic.Changelog.Tests.Changelogs;
using FakeItEasy;

namespace Elastic.Changelog.Tests.Evaluation;

public class ChangelogPrEvaluationServiceTests : ChangelogTestBase
{
	private readonly IGitHubPrService _mockGitHub;
	private readonly ICoreService _mockCore;

	// Minimal valid YAML config that satisfies all required types
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
		""";

	public ChangelogPrEvaluationServiceTests(ITestOutputHelper output) : base(output)
	{
		_mockGitHub = A.Fake<IGitHubPrService>();
		_mockCore = A.Fake<ICoreService>();

		// FakeItEasy returns "" for string by default; ensure GitHub methods return null
		A.CallTo(() => _mockGitHub.FetchLastFileCommitAuthorAsync(A<string>._, A<string>._, A<string>._, A<string>._, A<CancellationToken>._))
			.Returns((string?)null);
		A.CallTo(() => _mockGitHub.FetchCommitAuthorAsync(A<string>._, A<string>._, A<string>._, A<CancellationToken>._))
			.Returns((string?)null);
	}

	private ChangelogPrEvaluationService CreateService() =>
		new(LoggerFactory, ConfigurationContext, _mockGitHub, _mockCore, FileSystem);

	private EvaluatePrArguments DefaultArgs(
		string eventAction = "opened",
		bool titleChanged = false,
		string prTitle = "Fix something",
		string[]? prLabels = null,
		string? config = null
	)
	{
		config ??= "/tmp/config/changelog.yml";
		return new()
		{
			Config = config,
			Owner = "elastic",
			Repo = "test-repo",
			PrNumber = 42,
			PrTitle = prTitle,
			PrLabels = prLabels ?? ["type:feature"],
			HeadRef = "feature/test",
			HeadSha = "abc123",
			EventAction = eventAction,
			TitleChanged = titleChanged
		};
	}

	private async Task WriteMinimalConfig(string configPath = "/tmp/config/changelog.yml")
	{
		var dir = FileSystem.Path.GetDirectoryName(configPath)!;
		FileSystem.Directory.CreateDirectory(dir);
		await FileSystem.File.WriteAllTextAsync(configPath, MinimalConfig);
	}

	private void VerifyOutputSet(string name, string value) =>
		A.CallTo(() => _mockCore.SetOutputAsync(name, value)).MustHaveHappened();

	[Fact]
	public async Task EvaluatePr_BodyOnlyEdit_ReturnsSkipped()
	{
		var service = CreateService();
		var args = DefaultArgs(eventAction: "edited", titleChanged: false);

		var result = await service.EvaluatePr(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("status", "skipped");
		VerifyOutputSet("should-generate", "false");
	}

	[Fact]
	public async Task EvaluatePr_EditedWithTitleChange_DoesNotSkip()
	{
		await WriteMinimalConfig();
		var service = CreateService();
		var args = DefaultArgs(eventAction: "edited", titleChanged: true);

		var result = await service.EvaluatePr(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("status", "proceed");
		VerifyOutputSet("should-generate", "true");
	}

	[Fact]
	public async Task EvaluatePr_BotCommit_ReturnsSkipped()
	{
		A.CallTo(() => _mockGitHub.FetchCommitAuthorAsync("elastic", "test-repo", "abc123", A<CancellationToken>._))
			.Returns("github-actions[bot]");

		var service = CreateService();
		var args = DefaultArgs(eventAction: "synchronize");

		var result = await service.EvaluatePr(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("status", "skipped");
	}

	[Fact]
	public async Task EvaluatePr_ManuallyEdited_PrFilename_ReturnsManuallyEdited()
	{
		FileSystem.Directory.CreateDirectory("docs/changelog");
		await FileSystem.File.WriteAllTextAsync("docs/changelog/42.yaml", "title: test", TestContext.Current.CancellationToken);

		A.CallTo(() => _mockGitHub.FetchLastFileCommitAuthorAsync(
				"elastic", "test-repo", "docs/changelog/42.yaml", "feature/test", A<CancellationToken>._))
			.Returns("human-user");

		var service = CreateService();
		var args = DefaultArgs();

		var result = await service.EvaluatePr(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("status", "manually-edited");
	}

	[Fact]
	public async Task EvaluatePr_ManuallyEdited_TimestampFilename_ReturnsManuallyEdited()
	{
		FileSystem.Directory.CreateDirectory("docs/changelog");
		await FileSystem.File.WriteAllTextAsync("docs/changelog/1735689600-fix-something.yaml",
			"title: Fix something\nprs:\n  - \"42\"", TestContext.Current.CancellationToken);

		A.CallTo(() => _mockGitHub.FetchLastFileCommitAuthorAsync(
				"elastic", "test-repo", "docs/changelog/1735689600-fix-something.yaml", "feature/test", A<CancellationToken>._))
			.Returns("human-user");

		var service = CreateService();
		var args = DefaultArgs();

		var result = await service.EvaluatePr(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("status", "manually-edited");
	}

	[Fact]
	public async Task EvaluatePr_NoExistingFile_SkipsManualEditCheck()
	{
		await WriteMinimalConfig();
		var service = CreateService();
		var args = DefaultArgs();

		var result = await service.EvaluatePr(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("status", "proceed");
		A.CallTo(() => _mockGitHub.FetchLastFileCommitAuthorAsync(
			A<string>._, A<string>._, A<string>._, A<string>._, A<CancellationToken>._
		)).MustNotHaveHappened();
	}

	[Fact]
	public async Task EvaluatePr_NoTitle_ReturnsNoTitle()
	{
		await WriteMinimalConfig();
		var service = CreateService();
		var args = DefaultArgs(prTitle: "");

		var result = await service.EvaluatePr(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("status", "no-title");
	}

	[Fact]
	public async Task EvaluatePr_NoTypeLabel_ReturnsNoLabel()
	{
		await WriteMinimalConfig();
		var service = CreateService();
		var args = DefaultArgs(prLabels: ["unrelated-label"]);

		var result = await service.EvaluatePr(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("status", "no-label");
		VerifyOutputSet("should-generate", "false");
		A.CallTo(() => _mockCore.SetOutputAsync("label-table", A<string>.That.Contains("type:feature"))).MustHaveHappened();
	}

	[Fact]
	public async Task EvaluatePr_HappyPath_ReturnsSuccess()
	{
		await WriteMinimalConfig();
		var service = CreateService();
		var args = DefaultArgs();

		var result = await service.EvaluatePr(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("status", "proceed");
		VerifyOutputSet("should-generate", "true");
		VerifyOutputSet("title", "Fix something");
		VerifyOutputSet("type", "feature");
	}

	[Fact]
	public async Task EvaluatePr_StripTitlePrefix_RemovesBrackets()
	{
		await WriteMinimalConfig();
		var service = CreateService();
		var args = DefaultArgs(prTitle: "[Inference API] Fix timeout handling") with
		{
			StripTitlePrefix = true
		};

		var result = await service.EvaluatePr(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("title", "Fix timeout handling");
	}

	[Fact]
	public async Task EvaluatePr_NoConfig_UsesDefaults()
	{
		var service = CreateService();
		var args = DefaultArgs(config: "/nonexistent/changelog.yml");

		var result = await service.EvaluatePr(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("status", "no-label");
	}

	[Fact]
	public void BuildLabelTable_WithEntries_BuildsMarkdownTable()
	{
		var labelToType = new Dictionary<string, string>
		{
			["type:feature"] = "feature",
			["type:bug"] = "bug-fix"
		};

		var table = ChangelogPrEvaluationService.BuildLabelTable(labelToType);

		table.Should().Contain("| Label | Type |");
		table.Should().Contain("| `type:feature` | feature |");
		table.Should().Contain("| `type:bug` | bug-fix |");
	}

	[Fact]
	public void BuildLabelTable_NullOrEmpty_ReturnsEmpty()
	{
		ChangelogPrEvaluationService.BuildLabelTable(null).Should().BeEmpty();
		ChangelogPrEvaluationService.BuildLabelTable(new Dictionary<string, string>()).Should().BeEmpty();
	}

	[Fact]
	public async Task EvaluatePr_ExistingTimestampFile_OutputsFilename()
	{
		await WriteMinimalConfig();
		FileSystem.Directory.CreateDirectory("docs/changelog");
		await FileSystem.File.WriteAllTextAsync("docs/changelog/1735689600-fix-something.yaml",
			"title: Fix something\nprs:\n  - \"42\"", TestContext.Current.CancellationToken);

		var service = CreateService();
		var args = DefaultArgs();

		var result = await service.EvaluatePr(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("status", "proceed");
		VerifyOutputSet("existing-changelog-filename", "1735689600-fix-something.yaml");
	}

	[Fact]
	public async Task EvaluatePr_ExistingPrFile_OutputsFilename()
	{
		await WriteMinimalConfig();
		FileSystem.Directory.CreateDirectory("docs/changelog");
		await FileSystem.File.WriteAllTextAsync("docs/changelog/42.yaml", "title: Fix something", TestContext.Current.CancellationToken);

		var service = CreateService();
		var args = DefaultArgs();

		var result = await service.EvaluatePr(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("status", "proceed");
		VerifyOutputSet("existing-changelog-filename", "42.yaml");
	}

	[Fact]
	public void FindExistingChangelog_PrFilename_FindsByName()
	{
		FileSystem.Directory.CreateDirectory("docs/changelog");
		FileSystem.File.WriteAllText("docs/changelog/42.yaml", "title: test");

		var service = CreateService();
		var result = service.FindExistingChangelog("docs/changelog", 42);

		result.Should().Be("42.yaml");
	}

	[Fact]
	public void FindExistingChangelog_TimestampFilename_FindsByContent()
	{
		FileSystem.Directory.CreateDirectory("docs/changelog");
		FileSystem.File.WriteAllText("docs/changelog/1735689600-fix.yaml",
			"title: Fix\nprs:\n  - \"42\"");

		var service = CreateService();
		var result = service.FindExistingChangelog("docs/changelog", 42);

		result.Should().Be("1735689600-fix.yaml");
	}

	[Fact]
	public void FindExistingChangelog_GitHubUrl_FindsByContent()
	{
		FileSystem.Directory.CreateDirectory("docs/changelog");
		FileSystem.File.WriteAllText("docs/changelog/1735689600-fix.yaml",
			"title: Fix\nprs:\n  - \"https://github.com/elastic/test-repo/pull/42\"");

		var service = CreateService();
		var result = service.FindExistingChangelog("docs/changelog", 42);

		result.Should().Be("1735689600-fix.yaml");
	}

	[Fact]
	public void FindExistingChangelog_NoMatch_ReturnsNull()
	{
		FileSystem.Directory.CreateDirectory("docs/changelog");
		FileSystem.File.WriteAllText("docs/changelog/99.yaml", "title: other PR");

		var service = CreateService();
		var result = service.FindExistingChangelog("docs/changelog", 42);

		result.Should().BeNull();
	}

	[Fact]
	public void FindExistingChangelog_DirectoryMissing_ReturnsNull()
	{
		var service = CreateService();
		var result = service.FindExistingChangelog("nonexistent/path", 42);

		result.Should().BeNull();
	}

	[Theory]
	[InlineData("prs:\n  - \"42\"", true)]
	[InlineData("prs:\n  - '42'", true)]
	[InlineData("prs:\n  - \"https://github.com/elastic/repo/pull/42\"", true)]
	[InlineData("prs:\n  - \"142\"", false)]
	[InlineData("prs:\n  - \"4\"", false)]
	[InlineData("title: Issue #42 was fixed", false)]
	public void ContentReferencesPr_MatchesPrNumberCorrectly(string content, bool expected) =>
		ChangelogPrEvaluationService.ContentReferencesPr(content, "42").Should().Be(expected);
}
