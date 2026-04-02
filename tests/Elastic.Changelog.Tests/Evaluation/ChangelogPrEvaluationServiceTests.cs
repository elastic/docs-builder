// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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

	private const string ConfigWithProducts = """
		pivot:
		  types:
		    feature: "type:feature"
		    bug-fix: "type:bug"
		    breaking-change: "type:breaking"
		    enhancement: ">enhancement"
		    deprecation:
		    docs:
		    known-issue:
		    other:
		    regression:
		    security:
		  products:
		    cloud-hosted: "@Product:ECH"
		    cloud-serverless: "@Product:ESS"
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
		bool bodyChanged = false,
		string prTitle = "Fix something",
		string? prBody = null,
		string[]? prLabels = null,
		string? config = null
	)
	{
		config ??= Path.Join(Paths.WorkingDirectoryRoot.FullName, "config", "changelog.yml");
		return new()
		{
			Config = config,
			Owner = "elastic",
			Repo = "test-repo",
			PrNumber = 42,
			PrTitle = prTitle,
			PrBody = prBody,
			PrLabels = prLabels ?? ["type:feature"],
			HeadRef = "feature/test",
			HeadSha = "abc123",
			EventAction = eventAction,
			TitleChanged = titleChanged,
			BodyChanged = bodyChanged
		};
	}

	private async Task WriteMinimalConfig(string? configPath = null, string? content = null)
	{
		configPath ??= Path.Join(Paths.WorkingDirectoryRoot.FullName, "config", "changelog.yml");
		var dir = FileSystem.Path.GetDirectoryName(configPath)!;
		FileSystem.Directory.CreateDirectory(dir);
		await FileSystem.File.WriteAllTextAsync(configPath, content ?? MinimalConfig);
	}

	private void VerifyOutputSet(string name, string value) =>
		A.CallTo(() => _mockCore.SetOutputAsync(name, value)).MustHaveHappened();

	[Fact]
	public async Task EvaluatePr_EditedNoRelevantChange_ReturnsSkipped()
	{
		var service = CreateService();
		var args = DefaultArgs(eventAction: "edited", titleChanged: false, bodyChanged: false);

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
	public async Task EvaluatePr_EditedWithBodyChange_DoesNotSkip()
	{
		await WriteMinimalConfig();
		var service = CreateService();
		var args = DefaultArgs(eventAction: "edited", bodyChanged: true);

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
		FileSystem.Directory.CreateDirectory(Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs/changelog"));
		await FileSystem.File.WriteAllTextAsync(Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs/changelog/42.yaml"), "title: test", TestContext.Current.CancellationToken);

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
		FileSystem.Directory.CreateDirectory(Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs/changelog"));
		await FileSystem.File.WriteAllTextAsync(Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs/changelog/1735689600-fix-something.yaml"),
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
	public async Task EvaluatePr_NoTypeLabel_WithProductConfig_OutputsProductLabelTable()
	{
		await WriteMinimalConfig(Path.Join(Paths.WorkingDirectoryRoot.FullName, "config", "changelog.yml"), ConfigWithProducts);
		var service = CreateService();
		var args = DefaultArgs(prLabels: ["unrelated-label"], config: Path.Join(Paths.WorkingDirectoryRoot.FullName, "config", "changelog.yml"));

		var result = await service.EvaluatePr(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("status", "no-label");
		A.CallTo(() => _mockCore.SetOutputAsync("product-label-table", A<string>.That.Contains("@Product:ECH"))).MustHaveHappened();
		A.CallTo(() => _mockCore.SetOutputAsync("product-label-table", A<string>.That.Contains("cloud-hosted"))).MustHaveHappened();
	}

	[Fact]
	public async Task EvaluatePr_NoTypeLabel_WithProductLabels_DoesNotOutputProductLabelTable()
	{
		await WriteMinimalConfig(Path.Join(Paths.WorkingDirectoryRoot.FullName, "config", "changelog.yml"), ConfigWithProducts);
		var service = CreateService();
		var args = DefaultArgs(prLabels: ["@Product:ECH"], config: Path.Join(Paths.WorkingDirectoryRoot.FullName, "config", "changelog.yml"));

		var result = await service.EvaluatePr(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("status", "no-label");
		A.CallTo(() => _mockCore.SetOutputAsync("product-label-table", A<string>._)).MustNotHaveHappened();
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
	public void BuildProductLabelTable_WithEntries_BuildsMarkdownTable()
	{
		var labelToProducts = new Dictionary<string, string>
		{
			["@Product:ECH"] = "cloud-hosted",
			["@Product:ESS"] = "cloud-serverless"
		};

		var table = ChangelogPrEvaluationService.BuildProductLabelTable(labelToProducts);

		table.Should().Contain("| Label | Product |");
		table.Should().Contain("| `@Product:ECH` | cloud-hosted |");
		table.Should().Contain("| `@Product:ESS` | cloud-serverless |");
	}

	[Fact]
	public void BuildProductLabelTable_NullOrEmpty_ReturnsEmpty()
	{
		ChangelogPrEvaluationService.BuildProductLabelTable(null).Should().BeEmpty();
		ChangelogPrEvaluationService.BuildProductLabelTable(new Dictionary<string, string>()).Should().BeEmpty();
	}

	[Fact]
	public void BuildMappingTable_UsesCustomHeaders()
	{
		var mapping = new Dictionary<string, string> { ["key1"] = "value1" };

		var table = ChangelogPrEvaluationService.BuildMappingTable(mapping, "Custom Key", "Custom Value");

		table.Should().Contain("| Custom Key | Custom Value |");
		table.Should().Contain("| `key1` | value1 |");
	}

	[Fact]
	public async Task EvaluatePr_ExistingTimestampFile_OutputsFilename()
	{
		await WriteMinimalConfig();
		FileSystem.Directory.CreateDirectory(Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs/changelog"));
		await FileSystem.File.WriteAllTextAsync(Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs/changelog/1735689600-fix-something.yaml"),
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
		FileSystem.Directory.CreateDirectory(Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs/changelog"));
		await FileSystem.File.WriteAllTextAsync(Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs/changelog/42.yaml"), "title: Fix something", TestContext.Current.CancellationToken);

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
		var dir = Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs/changelog");
		FileSystem.Directory.CreateDirectory(dir);
		FileSystem.File.WriteAllText(Path.Join(dir, "42.yaml"), "title: test");

		var service = CreateService();
		var result = service.FindExistingChangelog(dir, 42);

		result.Should().Be("42.yaml");
	}

	[Fact]
	public void FindExistingChangelog_TimestampFilename_FindsByContent()
	{
		var dir = Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs/changelog");
		FileSystem.Directory.CreateDirectory(dir);
		FileSystem.File.WriteAllText(Path.Join(dir, "1735689600-fix.yaml"),
			"title: Fix\nprs:\n  - \"42\"");

		var service = CreateService();
		var result = service.FindExistingChangelog(dir, 42);

		result.Should().Be("1735689600-fix.yaml");
	}

	[Fact]
	public void FindExistingChangelog_GitHubUrl_FindsByContent()
	{
		var dir = Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs/changelog");
		FileSystem.Directory.CreateDirectory(dir);
		FileSystem.File.WriteAllText(Path.Join(dir, "1735689600-fix.yaml"),
			"title: Fix\nprs:\n  - \"https://github.com/elastic/test-repo/pull/42\"");

		var service = CreateService();
		var result = service.FindExistingChangelog(dir, 42);

		result.Should().Be("1735689600-fix.yaml");
	}

	[Fact]
	public void FindExistingChangelog_NoMatch_ReturnsNull()
	{
		var dir = Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs/changelog");
		FileSystem.Directory.CreateDirectory(dir);
		FileSystem.File.WriteAllText(Path.Join(dir, "99.yaml"), "title: other PR");

		var service = CreateService();
		var result = service.FindExistingChangelog(dir, 42);

		result.Should().BeNull();
	}

	[Fact]
	public void FindExistingChangelog_DirectoryMissing_ReturnsNull()
	{
		var service = CreateService();
		var result = service.FindExistingChangelog(Path.Join(Paths.WorkingDirectoryRoot.FullName, "nonexistent/path"), 42);

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

	[Fact]
	public async Task EvaluatePr_WithProductLabels_OutputsProductsAndNoTable()
	{
		await WriteMinimalConfig(Path.Join(Paths.WorkingDirectoryRoot.FullName, "config", "changelog.yml"), ConfigWithProducts);
		var service = CreateService();
		var args = DefaultArgs(
			prLabels: [">enhancement", "@Product:ECH", "@Product:ESS"],
			config: Path.Join(Paths.WorkingDirectoryRoot.FullName, "config", "changelog.yml")
		);

		var result = await service.EvaluatePr(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("status", "proceed");
		VerifyOutputSet("type", "enhancement");
		VerifyOutputSet("products", "cloud-hosted, cloud-serverless");
		A.CallTo(() => _mockCore.SetOutputAsync("product-label-table", A<string>._)).MustNotHaveHappened();
	}

	[Fact]
	public async Task EvaluatePr_WithoutProductLabels_OutputsProductLabelTable()
	{
		await WriteMinimalConfig(Path.Join(Paths.WorkingDirectoryRoot.FullName, "config", "changelog.yml"), ConfigWithProducts);
		var service = CreateService();
		var args = DefaultArgs(
			prLabels: ["type:feature"],
			config: Path.Join(Paths.WorkingDirectoryRoot.FullName, "config", "changelog.yml")
		);

		var result = await service.EvaluatePr(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("status", "proceed");
		A.CallTo(() => _mockCore.SetOutputAsync("products", A<string>._)).MustNotHaveHappened();
		A.CallTo(() => _mockCore.SetOutputAsync("product-label-table", A<string>.That.Contains("@Product:ECH"))).MustHaveHappened();
		A.CallTo(() => _mockCore.SetOutputAsync("product-label-table", A<string>.That.Contains("cloud-hosted"))).MustHaveHappened();
	}

	[Fact]
	public async Task EvaluatePr_ShortReleaseNote_OverridesPrTitle()
	{
		await WriteMinimalConfig();
		var service = CreateService();
		var args = DefaultArgs(
			prTitle: "Some PR title",
			prBody: "Release Notes: Added new search API endpoint"
		);

		var result = await service.EvaluatePr(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("title", "Added new search API endpoint");
		A.CallTo(() => _mockCore.SetOutputAsync("description", A<string>._)).MustNotHaveHappened();
	}

	[Fact]
	public async Task EvaluatePr_LongReleaseNote_UsedAsDescription_PrTitleAsTitle()
	{
		await WriteMinimalConfig();
		var service = CreateService();
		var longNote = new string('x', 130);
		var args = DefaultArgs(
			prTitle: "Some PR title",
			prBody: $"Release Notes: {longNote}"
		);

		var result = await service.EvaluatePr(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("title", "Some PR title");
		VerifyOutputSet("description", longNote);
	}

	[Fact]
	public async Task EvaluatePr_NoReleaseNote_FallsBackToPrTitle()
	{
		await WriteMinimalConfig();
		var service = CreateService();
		var args = DefaultArgs(
			prTitle: "Fix something",
			prBody: "This PR fixes a bug in the search API."
		);

		var result = await service.EvaluatePr(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("title", "Fix something");
		A.CallTo(() => _mockCore.SetOutputAsync("description", A<string>._)).MustNotHaveHappened();
	}

	[Fact]
	public async Task EvaluatePr_NullBody_FallsBackToPrTitle()
	{
		await WriteMinimalConfig();
		var service = CreateService();
		var args = DefaultArgs(prTitle: "Fix something", prBody: null);

		var result = await service.EvaluatePr(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("title", "Fix something");
	}

	[Fact]
	public async Task EvaluatePr_ExtractionDisabled_IgnoresReleaseNote()
	{
		var configWithExtractionDisabled = """
			extract:
			  release_notes: false
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
		await WriteMinimalConfig(content: configWithExtractionDisabled);
		var service = CreateService();
		var args = DefaultArgs(
			prTitle: "Original PR title",
			prBody: "Release Notes: Should be ignored"
		);

		var result = await service.EvaluatePr(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("title", "Original PR title");
	}

	[Fact]
	public async Task EvaluatePr_ReleaseNoteHeader_ExtractedAsTitle()
	{
		await WriteMinimalConfig();
		var service = CreateService();
		var args = DefaultArgs(
			prTitle: "Some PR title",
			prBody: """
				## Description
				This PR adds a new feature.

				## Release Note
				New aggregation pipeline support
				"""
		);

		var result = await service.EvaluatePr(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("title", "New aggregation pipeline support");
	}

	// --- CollectExcludeLabels unit tests ---

	[Fact]
	public void CollectExcludeLabels_Null_ReturnsNull() =>
		ChangelogPrEvaluationService.CollectExcludeLabels(null).Should().BeNull();

	[Fact]
	public void CollectExcludeLabels_NoLabels_ReturnsNull() =>
		ChangelogPrEvaluationService.CollectExcludeLabels(new CreateRules()).Should().BeNull();

	[Fact]
	public void CollectExcludeLabels_GlobalExcludeLabels_ReturnsCommaSeparated()
	{
		var rules = new CreateRules
		{
			Mode = FieldMode.Exclude,
			Labels = [">non-issue", ">test"]
		};

		var result = ChangelogPrEvaluationService.CollectExcludeLabels(rules);

		result.Should().NotBeNull();
		result!.Split(',').Should().BeEquivalentTo([">non-issue", ">test"]);
	}

	[Fact]
	public void CollectExcludeLabels_IncludeMode_ReturnsNull()
	{
		var rules = new CreateRules
		{
			Mode = FieldMode.Include,
			Labels = [">non-issue"]
		};

		ChangelogPrEvaluationService.CollectExcludeLabels(rules).Should().BeNull();
	}

	[Fact]
	public void CollectExcludeLabels_PerProductExcludeOnly_ReturnsLabels()
	{
		var rules = new CreateRules
		{
			ByProduct = new Dictionary<string, CreateRules>
			{
				["cloud-hosted"] = new() { Mode = FieldMode.Exclude, Labels = [">skip-ech"] },
				["cloud-serverless"] = new() { Mode = FieldMode.Exclude, Labels = [">skip-ess"] }
			}
		};

		var result = ChangelogPrEvaluationService.CollectExcludeLabels(rules);

		result.Should().NotBeNull();
		result!.Split(',').Should().BeEquivalentTo([">skip-ech", ">skip-ess"]);
	}

	[Fact]
	public void CollectExcludeLabels_GlobalAndPerProduct_MergesUniqueLabels()
	{
		var rules = new CreateRules
		{
			Mode = FieldMode.Exclude,
			Labels = [">skip-all", ">shared"],
			ByProduct = new Dictionary<string, CreateRules>
			{
				["cloud-hosted"] = new() { Mode = FieldMode.Exclude, Labels = [">shared", ">skip-ech"] }
			}
		};

		var result = ChangelogPrEvaluationService.CollectExcludeLabels(rules);

		result.Should().NotBeNull();
		result!.Split(',').Should().BeEquivalentTo([">skip-all", ">shared", ">skip-ech"]);
	}

	[Fact]
	public void CollectExcludeLabels_PerProductIncludeMode_IgnoresIncludeProducts()
	{
		var rules = new CreateRules
		{
			Mode = FieldMode.Exclude,
			Labels = [">global"],
			ByProduct = new Dictionary<string, CreateRules>
			{
				["cloud-hosted"] = new() { Mode = FieldMode.Include, Labels = [">include-only"] }
			}
		};

		var result = ChangelogPrEvaluationService.CollectExcludeLabels(rules);

		result.Should().NotBeNull();
		result!.Split(',').Should().BeEquivalentTo([">global"]);
	}

	// --- skip-labels output integration tests ---

	private const string ConfigWithExcludeRules = """
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
		    exclude: ">non-issue, >test"
		""";

	[Fact]
	public async Task EvaluatePr_WithExcludeRules_AllBlocked_OutputsSkipLabels()
	{
		await WriteMinimalConfig(content: ConfigWithExcludeRules);
		var service = CreateService();
		var args = DefaultArgs(prLabels: [">non-issue"]);

		var result = await service.EvaluatePr(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("status", "skipped");
		A.CallTo(() => _mockCore.SetOutputAsync("skip-labels", A<string>.That.Contains(">non-issue"))).MustHaveHappened();
		A.CallTo(() => _mockCore.SetOutputAsync("skip-labels", A<string>.That.Contains(">test"))).MustHaveHappened();
	}

	[Fact]
	public async Task EvaluatePr_WithExcludeRules_NoLabel_OutputsSkipLabels()
	{
		await WriteMinimalConfig(content: ConfigWithExcludeRules);
		var service = CreateService();
		var args = DefaultArgs(prLabels: ["unrelated-label"]);

		var result = await service.EvaluatePr(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("status", "no-label");
		A.CallTo(() => _mockCore.SetOutputAsync("skip-labels", A<string>.That.Contains(">non-issue"))).MustHaveHappened();
	}

	[Fact]
	public async Task EvaluatePr_WithoutExcludeRules_DoesNotOutputSkipLabels()
	{
		await WriteMinimalConfig();
		var service = CreateService();
		var args = DefaultArgs();

		var result = await service.EvaluatePr(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("status", "proceed");
		A.CallTo(() => _mockCore.SetOutputAsync("skip-labels", A<string>._)).MustNotHaveHappened();
	}

	[Fact]
	public async Task EvaluatePr_HappyPath_WithExcludeRules_DoesNotOutputSkipLabels()
	{
		await WriteMinimalConfig(content: ConfigWithExcludeRules);
		var service = CreateService();
		var args = DefaultArgs(prLabels: ["type:feature"]);

		var result = await service.EvaluatePr(Collector, args, CancellationToken.None);

		result.Should().BeTrue();
		VerifyOutputSet("status", "proceed");
		A.CallTo(() => _mockCore.SetOutputAsync("skip-labels", A<string>._)).MustNotHaveHappened();
	}
}
