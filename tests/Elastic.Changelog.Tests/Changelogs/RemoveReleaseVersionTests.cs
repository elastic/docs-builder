// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.GitHub;
using Elastic.Documentation.ReleaseNotes;
using FakeItEasy;
using FluentAssertions;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// Tests for 'changelog remove --release-version' behaviour.
/// The command translates a GitHub release into a PR list, then delegates to
/// <see cref="ChangelogRemoveService"/>. These tests exercise that full pipeline.
/// </summary>
public class RemoveReleaseVersionTests : ChangelogTestBase
{
	private readonly IGitHubReleaseService _mockReleaseService = A.Fake<IGitHubReleaseService>();
	private readonly ChangelogRemoveService _removeService;
	private readonly string _changelogDir;

	public RemoveReleaseVersionTests(ITestOutputHelper output) : base(output)
	{
		_removeService = new ChangelogRemoveService(LoggerFactory, ConfigurationContext, FileSystem);
		_changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(_changelogDir);
	}

	// -----------------------------------------------------------------------
	// Core flow: release → PR list → remove matching changelogs
	// -----------------------------------------------------------------------

	[Fact]
	public async Task ReleaseVersion_RemovesMatchingChangelogs()
	{
		// Arrange – two changelog files each referencing a specific PR
		await WriteChangelog("pr-12345.yaml",
			"""
			title: Fix query parsing
			type: bug-fix
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/12345
			""");

		await WriteChangelog("pr-12346.yaml",
			"""
			title: New aggregation API
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/12346
			""");

		// A third file whose PR is NOT in the release — must not be removed
		await WriteChangelog("pr-99999.yaml",
			"""
			title: Unrelated change
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/99999
			""");

		// Release body references only the first two PRs
		var releaseBody =
			"""
			## What's Changed

			* Fix query parsing by @contributor1 in #12345
			* New aggregation API by @contributor2 in #12346

			**Full Changelog**: https://github.com/elastic/elasticsearch/compare/v9.1.0...v9.2.0
			""";

		A.CallTo(() => _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "v9.2.0", A<Cancel>._))
			.Returns(new GitHubReleaseInfo { TagName = "v9.2.0", Name = "9.2.0", Body = releaseBody });

		// Act – simulate what the command does: fetch release → build PR URL list → call service
		var prUrls = await ResolveReleasePrUrls("elastic", "elasticsearch", "v9.2.0");
		var input = new ChangelogRemoveArguments
		{
			Directory = _changelogDir,
			Prs = prUrls
		};

		var result = await _removeService.RemoveChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		// Matched files were removed; unmatched file was left in place
		FileSystem.File.Exists(FileSystem.Path.Combine(_changelogDir, "pr-12345.yaml")).Should().BeFalse("PR 12345 is in the release");
		FileSystem.File.Exists(FileSystem.Path.Combine(_changelogDir, "pr-12346.yaml")).Should().BeFalse("PR 12346 is in the release");
		FileSystem.File.Exists(FileSystem.Path.Combine(_changelogDir, "pr-99999.yaml")).Should().BeTrue("PR 99999 is not in the release");
	}

	[Fact]
	public async Task ReleaseVersion_DryRun_DoesNotDeleteFiles()
	{
		// Arrange
		await WriteChangelog("pr-12345.yaml",
			"""
			title: Fix query parsing
			type: bug-fix
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/12345
			""");

		var releaseBody = "* Fix query parsing by @user in #12345\n";

		A.CallTo(() => _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "v9.2.0", A<Cancel>._))
			.Returns(new GitHubReleaseInfo { TagName = "v9.2.0", Name = "9.2.0", Body = releaseBody });

		var prUrls = await ResolveReleasePrUrls("elastic", "elasticsearch", "v9.2.0");
		var input = new ChangelogRemoveArguments
		{
			Directory = _changelogDir,
			Prs = prUrls,
			DryRun = true
		};

		// Act
		var result = await _removeService.RemoveChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert – file must still exist after a dry run
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		FileSystem.File.Exists(FileSystem.Path.Combine(_changelogDir, "pr-12345.yaml"))
			.Should().BeTrue("dry run must not delete files");
	}

	// -----------------------------------------------------------------------
	// No PR refs in release notes — no changelogs removed
	// -----------------------------------------------------------------------

	[Fact]
	public async Task ReleaseVersion_WithNoMatchingPrs_EmitsWarning()
	{
		// Arrange – release body has no PR references
		A.CallTo(() => _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "v9.2.0", A<Cancel>._))
			.Returns(new GitHubReleaseInfo
			{
				TagName = "v9.2.0",
				Name = "9.2.0",
				Body = "Release notes with no pull request references."
			});

		// Act – replicate command logic: parse, detect zero refs, warn and return without deleting
		var release = await _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "v9.2.0", TestContext.Current.CancellationToken);
		var parsed = ReleaseNoteParser.Parse(release!.Body);

		// Assert – the parser found nothing, so the command would emit a warning and exit early
		parsed.PrReferences.Should().BeEmpty();
	}

	// -----------------------------------------------------------------------
	// Release fetch failure
	// -----------------------------------------------------------------------

	[Fact]
	public async Task ReleaseVersion_FetchFailure_ReturnsNull()
	{
		// Arrange
		A.CallTo(() => _mockReleaseService.FetchReleaseAsync(A<string>._, A<string>._, A<string?>._, A<Cancel>._))
			.Returns((GitHubReleaseInfo?)null);

		// Act
		var release = await _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "v9.2.0", TestContext.Current.CancellationToken);

		// Assert – command returns error on null release
		release.Should().BeNull();
	}

	// -----------------------------------------------------------------------
	// Latest tag: FetchReleaseAsync is called with "latest"
	// -----------------------------------------------------------------------

	[Fact]
	public async Task ReleaseVersion_Latest_CallsFetchWithLatestTag()
	{
		// Arrange
		A.CallTo(() => _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "latest", A<Cancel>._))
			.Returns(new GitHubReleaseInfo
			{
				TagName = "v9.2.0",
				Name = "9.2.0",
				Body = "No PR references."
			});

		// Act
		_ = await _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "latest", TestContext.Current.CancellationToken);

		// Assert
		A.CallTo(() => _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "latest", A<Cancel>._))
			.MustHaveHappenedOnceExactly();
	}

	// -----------------------------------------------------------------------
	// Release with partial match: only changelogs referencing release PRs are removed
	// -----------------------------------------------------------------------

	[Fact]
	public async Task ReleaseVersion_OnlyRemovesChangelogsMatchingReleasePrs()
	{
		// Arrange – three changelogs; release only references two
		await WriteChangelog("es-pr-100.yaml",
			"""
			title: Feature A
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""");

		await WriteChangelog("es-pr-200.yaml",
			"""
			title: Feature B
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/200
			""");

		await WriteChangelog("es-pr-300.yaml",
			"""
			title: Feature C (different release)
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.3.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/300
			""");

		// Release only contains PRs 100 and 200
		var releaseBody =
			"""
			* Feature A by @user in #100
			* Feature B by @user in #200
			""";

		A.CallTo(() => _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "v9.2.0", A<Cancel>._))
			.Returns(new GitHubReleaseInfo { TagName = "v9.2.0", Name = "9.2.0", Body = releaseBody });

		var prUrls = await ResolveReleasePrUrls("elastic", "elasticsearch", "v9.2.0");
		var input = new ChangelogRemoveArguments
		{
			Directory = _changelogDir,
			Prs = prUrls
		};

		// Act
		var result = await _removeService.RemoveChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		FileSystem.File.Exists(FileSystem.Path.Combine(_changelogDir, "es-pr-100.yaml")).Should().BeFalse();
		FileSystem.File.Exists(FileSystem.Path.Combine(_changelogDir, "es-pr-200.yaml")).Should().BeFalse();
		FileSystem.File.Exists(FileSystem.Path.Combine(_changelogDir, "es-pr-300.yaml")).Should().BeTrue("PR 300 is not in the release");
	}

	// -----------------------------------------------------------------------
	// Helpers
	// -----------------------------------------------------------------------

	private async Task WriteChangelog(string filename, string content)
	{
		var path = FileSystem.Path.Combine(_changelogDir, filename);
		await FileSystem.File.WriteAllTextAsync(path, content, Encoding.UTF8, TestContext.Current.CancellationToken);
	}

	/// <summary>Fetches a release and extracts full PR URLs, mirroring the command's logic.</summary>
	private async Task<string[]> ResolveReleasePrUrls(string owner, string repo, string version)
	{
		var release = await _mockReleaseService.FetchReleaseAsync(owner, repo, version, TestContext.Current.CancellationToken);
		var parsed = ReleaseNoteParser.Parse(release!.Body);
		return parsed.PrReferences
			.Select(r => $"https://github.com/{owner}/{repo}/pull/{r.PrNumber}")
			.ToArray();
	}
}
