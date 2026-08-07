// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Changelog.AllowlistIdentity;
using Elastic.Changelog.GitHub;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Changelog.Tests.AllowlistIdentity;

[SuppressMessage("Usage", "CA1001:Types that own disposable fields should be disposable")]
public class ScrubberAllowlistIdentityServiceTests(ITestOutputHelper output)
{
	private const string ValidSha = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
	private const string ValidCommit = "0123456789abcdef0123456789abcdef01234567";

	private static readonly string ValidAssetJson =
		$$"""
		{
			"schema_version": 1,
			"artifact": "scrubber-allowlist-identity",
			"allowlist_sha256": "{{ValidSha}}",
			"deployment_commit": "{{ValidCommit}}",
			"git_ref": "v1.2.3",
			"built_at": "2026-08-01T12:00:00Z"
		}
		""";

	private readonly IGitHubReleaseService _releaseService = A.Fake<IGitHubReleaseService>();
	private readonly MockFileSystem _fileSystem = new();
	private readonly TestDiagnosticsCollector _collector = new(output);

	private ScrubberAllowlistIdentityService CreateService() =>
		new(NullLoggerFactory.Instance, _releaseService, _fileSystem);

	private static GitHubReleaseInfo Release(string tag, bool withAsset, bool draft = false) => new()
	{
		TagName = tag,
		Draft = draft,
		Assets = withAsset
			? [new GitHubReleaseAsset { Name = ScrubberAllowlistIdentity.AssetName, BrowserDownloadUrl = $"https://example/{tag}" }]
			: [new GitHubReleaseAsset { Name = "docs-builder.zip", BrowserDownloadUrl = $"https://example/{tag}/zip" }]
	};

	private void AssetDownloadReturns(string? content) =>
		A.CallTo(() => _releaseService.DownloadAssetTextAsync(
				A<GitHubReleaseAsset>.That.Matches(a => a.Name == ScrubberAllowlistIdentity.AssetName), A<CancellationToken>._))
			.Returns(Task.FromResult(content));

	[Fact]
	public async Task ResolveDeployedAsync_LatestReleaseCarriesAsset_ResolvesIt()
	{
		A.CallTo(() => _releaseService.FetchReleasesAsync("elastic", "docs-builder", A<int>._, A<CancellationToken>._))
			.Returns(Task.FromResult<IReadOnlyList<GitHubReleaseInfo>>([Release("v2.0.0", withAsset: true)]));
		AssetDownloadReturns(ValidAssetJson);

		var resolved = await CreateService().ResolveDeployedAsync(_collector, new ResolveScrubberAllowlistArguments(), TestContext.Current.CancellationToken);

		resolved.Should().NotBeNull();
		resolved.ReleaseTag.Should().Be("v2.0.0");
		resolved.Identity.AllowlistSha256.Should().Be(ValidSha);
		resolved.MatchesLocal.Should().BeNull();
	}

	[Fact]
	public async Task ResolveDeployedAsync_NewestReleaseMissingAsset_FallsBackToPreviousRelease()
	{
		// The newest release exists but its scrubber deploy never completed (no asset); the one
		// before it is the most recent gated deploy and must win.
		A.CallTo(() => _releaseService.FetchReleasesAsync("elastic", "docs-builder", A<int>._, A<CancellationToken>._))
			.Returns(Task.FromResult<IReadOnlyList<GitHubReleaseInfo>>(
				[Release("v2.1.0", withAsset: false), Release("v2.0.0", withAsset: true)]));
		AssetDownloadReturns(ValidAssetJson);

		var resolved = await CreateService().ResolveDeployedAsync(_collector, new ResolveScrubberAllowlistArguments(), TestContext.Current.CancellationToken);

		resolved.Should().NotBeNull();
		resolved.ReleaseTag.Should().Be("v2.0.0");
	}

	[Fact]
	public async Task ResolveDeployedAsync_DraftReleasesAreSkipped()
	{
		A.CallTo(() => _releaseService.FetchReleasesAsync("elastic", "docs-builder", A<int>._, A<CancellationToken>._))
			.Returns(Task.FromResult<IReadOnlyList<GitHubReleaseInfo>>(
				[Release("v2.1.0", withAsset: true, draft: true), Release("v2.0.0", withAsset: true)]));
		AssetDownloadReturns(ValidAssetJson);

		var resolved = await CreateService().ResolveDeployedAsync(_collector, new ResolveScrubberAllowlistArguments(), TestContext.Current.CancellationToken);

		resolved.Should().NotBeNull();
		resolved.ReleaseTag.Should().Be("v2.0.0");
	}

	[Fact]
	public async Task ResolveDeployedAsync_NoReleaseCarriesAsset_FailsWithError()
	{
		A.CallTo(() => _releaseService.FetchReleasesAsync("elastic", "docs-builder", A<int>._, A<CancellationToken>._))
			.Returns(Task.FromResult<IReadOnlyList<GitHubReleaseInfo>>([Release("v2.1.0", withAsset: false)]));

		var resolved = await CreateService().ResolveDeployedAsync(_collector, new ResolveScrubberAllowlistArguments(), TestContext.Current.CancellationToken);

		resolved.Should().BeNull();
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("cannot be resolved"));
	}

	[Fact]
	public async Task ResolveDeployedAsync_ExplicitTagWithoutAsset_FailsWithError()
	{
		A.CallTo(() => _releaseService.FetchReleaseAsync("elastic", "docs-builder", "v1.0.0", A<CancellationToken>._))
			.Returns(Task.FromResult<GitHubReleaseInfo?>(Release("v1.0.0", withAsset: false)));

		var resolved = await CreateService().ResolveDeployedAsync(_collector,
			new ResolveScrubberAllowlistArguments { Tag = "v1.0.0" }, TestContext.Current.CancellationToken);

		resolved.Should().BeNull();
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("predates"));
	}

	[Fact]
	public async Task ResolveDeployedAsync_ExplicitTagNotFound_FailsWithError()
	{
		A.CallTo(() => _releaseService.FetchReleaseAsync("elastic", "docs-builder", "v9.9.9", A<CancellationToken>._))
			.Returns(Task.FromResult<GitHubReleaseInfo?>(null));

		var resolved = await CreateService().ResolveDeployedAsync(_collector,
			new ResolveScrubberAllowlistArguments { Tag = "v9.9.9" }, TestContext.Current.CancellationToken);

		resolved.Should().BeNull();
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("was not found"));
	}

	[Fact]
	public async Task ResolveDeployedAsync_MalformedAsset_FailsWithError()
	{
		A.CallTo(() => _releaseService.FetchReleasesAsync("elastic", "docs-builder", A<int>._, A<CancellationToken>._))
			.Returns(Task.FromResult<IReadOnlyList<GitHubReleaseInfo>>([Release("v2.0.0", withAsset: true)]));
		AssetDownloadReturns(/*lang=json,strict*/ """{ "schema_version": 1, "artifact": "scrubber-allowlist-identity", "allowlist_sha256": "nope", "deployment_commit": "nope" }""");

		var resolved = await CreateService().ResolveDeployedAsync(_collector, new ResolveScrubberAllowlistArguments(), TestContext.Current.CancellationToken);

		resolved.Should().BeNull();
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("Invalid allowlist identity"));
	}

	[Fact]
	public async Task ResolveDeployedAsync_LocalAssemblerMatches_ReportsMatch()
	{
		// sha256 of "hello\n"
		const string helloSha = "sha256:5891b5b522d5df086d0ff0b110fbd9d21bb4fc7163af34d08286a2e846f6be03";
		A.CallTo(() => _releaseService.FetchReleasesAsync("elastic", "docs-builder", A<int>._, A<CancellationToken>._))
			.Returns(Task.FromResult<IReadOnlyList<GitHubReleaseInfo>>([Release("v2.0.0", withAsset: true)]));
		AssetDownloadReturns(ValidAssetJson.Replace(ValidSha, helloSha));
		_fileSystem.AddFile("/repo/config/assembler.yml", new MockFileData("hello\n"));

		var resolved = await CreateService().ResolveDeployedAsync(_collector,
			new ResolveScrubberAllowlistArguments { AssemblerPath = "/repo/config/assembler.yml" }, TestContext.Current.CancellationToken);

		resolved.Should().NotBeNull();
		resolved.LocalSha256.Should().Be(helloSha);
		resolved.MatchesLocal.Should().BeTrue();
	}

	[Fact]
	public async Task ResolveDeployedAsync_LocalAssemblerDiffers_WarnsButResolves()
	{
		A.CallTo(() => _releaseService.FetchReleasesAsync("elastic", "docs-builder", A<int>._, A<CancellationToken>._))
			.Returns(Task.FromResult<IReadOnlyList<GitHubReleaseInfo>>([Release("v2.0.0", withAsset: true)]));
		AssetDownloadReturns(ValidAssetJson);
		_fileSystem.AddFile("/repo/config/assembler.yml", new MockFileData("different content\n"));

		var resolved = await CreateService().ResolveDeployedAsync(_collector,
			new ResolveScrubberAllowlistArguments { AssemblerPath = "/repo/config/assembler.yml" }, TestContext.Current.CancellationToken);

		resolved.Should().NotBeNull();
		resolved.MatchesLocal.Should().BeFalse();
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("differs from the deployed scrubber allowlist"));
	}
}
