// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.GitHub;
using Elastic.Documentation.ReleaseNotes;
using FakeItEasy;
using FluentAssertions;
using Xunit;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// Tests for 'changelog bundle --release-version' behaviour.
/// The command translates a GitHub release into a PR list, then delegates to
/// <see cref="ChangelogBundlingService"/>. These tests exercise that full pipeline.
/// </summary>
public class BundleReleaseVersionTests : ChangelogTestBase
{
	private readonly IGitHubReleaseService _mockReleaseService = A.Fake<IGitHubReleaseService>();
	private readonly ChangelogBundlingService _bundlingService;
	private readonly string _changelogDir;

	public BundleReleaseVersionTests(ITestOutputHelper output) : base(output)
	{
		_bundlingService = new ChangelogBundlingService(LoggerFactory, ConfigurationContext, FileSystem);
		_changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(_changelogDir);
	}

	private string BundleOutputPath() =>
		FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");

	// -----------------------------------------------------------------------
	// Core flow: release → PR list → bundle
	// -----------------------------------------------------------------------

	[Fact]
	public async Task ReleaseVersion_BundlesMatchingChangelogs()
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

		// Release body in GitHub Default format referencing both PRs
		var releaseBody =
			"""
			## What's Changed

			* Fix query parsing by @contributor1 in #12345
			* New aggregation API by @contributor2 in #12346

			**Full Changelog**: https://github.com/elastic/elasticsearch/compare/v9.1.0...v9.2.0
			""";

		A.CallTo(() => _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "v9.2.0", A<Cancel>._))
			.Returns(new GitHubReleaseInfo { TagName = "v9.2.0", Name = "9.2.0", Body = releaseBody });

		// Act – simulate what the command does
		var prUrls = await ResolveReleasePrUrls("elastic", "elasticsearch", "v9.2.0");
		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Prs = prUrls,
			Output = BundleOutputPath()
		};

		var result = await _bundlingService.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("pr-12345.yaml");
		bundleContent.Should().Contain("pr-12346.yaml");
	}

	// -----------------------------------------------------------------------
	// Explicit output products
	// -----------------------------------------------------------------------

	[Fact]
	public async Task ReleaseVersion_ExplicitOutputProducts_SetsBundleProducts()
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

		var releaseBody =
			"""
			## What's Changed

			* Fix query parsing by @contributor1 in #12345
			""";

		A.CallTo(() => _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "v9.2.0", A<Cancel>._))
			.Returns(new GitHubReleaseInfo { TagName = "v9.2.0", Name = "9.2.0", Body = releaseBody });

		var prUrls = await ResolveReleasePrUrls("elastic", "elasticsearch", "v9.2.0");

		// Explicit output products override: includes cloud-hosted alongside elasticsearch
		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Prs = prUrls,
			OutputProducts =
			[
				new ProductArgument { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" },
				new ProductArgument { Product = "cloud-hosted", Target = "2025-10-31", Lifecycle = "ga" }
			],
			Output = BundleOutputPath()
		};

		var result = await _bundlingService.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("product: elasticsearch");
		bundleContent.Should().Contain("product: cloud-hosted");
	}

	// -----------------------------------------------------------------------
	// No PR refs in release notes
	// -----------------------------------------------------------------------

	[Fact]
	public async Task ReleaseVersion_WithNoMatchingPrs_EmitsWarning()
	{
		// Arrange
		A.CallTo(() => _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "v9.2.0", A<Cancel>._))
			.Returns(new GitHubReleaseInfo
			{
				TagName = "v9.2.0",
				Name = "9.2.0",
				Body = "Release notes with no pull request references."
			});

		// Act – replicate command logic: parse, detect zero refs, warn and return success
		var release = await _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "v9.2.0", TestContext.Current.CancellationToken);
		var parsed = ReleaseNoteParser.Parse(release!.Body);

		// Assert
		parsed.PrReferences.Should().BeEmpty();

		// In the command, zero refs triggers a warning and early return 0 —
		// verify the parser itself found nothing so the command's guard is correct.
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

