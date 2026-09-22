// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using AwesomeAssertions;
using Elastic.Changelog.GitHub;
using Elastic.Changelog.GithubRelease;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using FakeItEasy;
using Xunit;

namespace Elastic.Changelog.Tests.Changelogs.Create;

/// <summary>
/// Tests for 'changelog add --release-version' behaviour, implemented via
/// <see cref="GitHubReleaseChangelogService"/> with <see cref="CreateChangelogsFromReleaseArguments.CreateBundle"/> = false.
/// </summary>
public class ReleaseVersionTests(ITestOutputHelper output) : ChangelogTestBase(output)
{
	private readonly IGitHubReleaseService _mockReleaseService = A.Fake<IGitHubReleaseService>();
	private readonly IGitHubPrService _mockPrService = A.Fake<IGitHubPrService>();
	private readonly IGitHubCommitRangeService _mockCommitRangeService = A.Fake<IGitHubCommitRangeService>();

	// CreateChangelogsFromRelease always probes the checked-in entry pool. Without a stub handler
	// the default CdnChangelogEntryFetcher hits ChangelogCdn's real production base URL — offline
	// or sandboxed test runs must never make that call, so every test here gets an all-404 handler.
	private readonly CdnChangelogEntryFetcher _offlineEntryFetcher = new(
		new TestLoggerFactory(output),
		new OfflinePoolHandler(),
		sleep: (_, _) => Task.CompletedTask
	);

	private GitHubReleaseChangelogService CreateService() =>
		new(
			LoggerFactory,
			ConfigurationContext,
			FileSystem,
			_mockReleaseService,
			_mockPrService,
			commitRangeService: _mockCommitRangeService,
			entryFetcher: _offlineEntryFetcher
		);

	private string CreateOutputDirectory() => FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

	/// <summary>Stubs the release service and commit range service for a standard elasticsearch v9.2.0 release.</summary>
	private void ArrangeRelease(string version = "v9.2.0", params int[] prNumbers)
	{
		A.CallTo(
			() => _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", version, A<Cancel>._)
		).Returns(new GitHubReleaseInfo { TagName = "v9.2.0", Name = "9.2.0", Body = "" });

		A.CallTo(() => _mockReleaseService.FetchPreviousTagAsync("elastic", "elasticsearch", "v9.2.0", A<Cancel>._)).Returns(
			PreviousTagResult.Found("v9.1.0")
		);

		var prs = prNumbers.Select(
			n => new CommitRangePullRequest
			{
				Number = n,
				Url = $"https://github.com/elastic/elasticsearch/pull/{n}",
				CommitShas = ["abc123"]
			}
		).ToList();

		A.CallTo(
			() => _mockCommitRangeService.ResolvePullRequestsAsync(
				A<IDiagnosticsCollector>._,
				A<CommitRangeArguments>.That.Matches(
					a => a.Owner == "elastic" && a.Repo == "elasticsearch" && a.StartRef == "v9.1.0" && a.EndRef == "v9.2.0"
				),
				A<Cancel>._
			)
		).Returns(new CommitRangeResolution { TotalCommits = prNumbers.Length, PullRequests = prs, CommitsWithoutPullRequest = [] });
	}

	// -----------------------------------------------------------------------
	// Validation: no PR refs in release notes
	// -----------------------------------------------------------------------

	[Fact]
	public async Task ReleaseVersion_WithNoMatchingPrs_EmitsWarningAndSucceeds()
	{
		// Arrange — commit range returns zero PRs
		ArrangeRelease("v9.2.0");

		var service = CreateService();
		var input = new CreateChangelogsFromReleaseArguments
		{
			Repository = "elastic/elasticsearch",
			Version = "v9.2.0",
			Output = CreateOutputDirectory(),
			CreateBundle = false
		};

		// Act
		var result = await service.CreateChangelogsFromRelease(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("No PR") && d.Severity == Severity.Warning);
	}

	// -----------------------------------------------------------------------
	// CreateBundle = false: no bundle file is written
	// -----------------------------------------------------------------------

	[Fact]
	public async Task ReleaseVersion_WithValidRelease_CreatesChangelogFiles_AndNoBundleFile()
	{
		// Arrange — two PRs from the commit range
		ArrangeRelease("v9.2.0", 12345, 12346);

		A.CallTo(() => _mockPrService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<Cancel>._)).Returns(new GitHubPrInfo
		{
			Title = "PR title",
			Labels = []
		});

		var outputDir = CreateOutputDirectory();
		FileSystem.Directory.CreateDirectory(outputDir);

		var service = CreateService();
		var input = new CreateChangelogsFromReleaseArguments
		{
			Repository = "elastic/elasticsearch",
			Version = "v9.2.0",
			Output = outputDir,
			CreateBundle = false
		};

		// Act
		var result = await service.CreateChangelogsFromRelease(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var yamlFiles = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		yamlFiles.Should().HaveCount(2, "one changelog file per PR reference");

		// No bundle file in the output directory or bundles subdirectory
		var bundlesDir = FileSystem.Path.Join(outputDir, "bundles");
		FileSystem.Directory.Exists(bundlesDir).Should().BeFalse("CreateBundle = false must not create a bundles directory");
	}

	// -----------------------------------------------------------------------
	// CreateBundle = true (default): bundle file is written
	// -----------------------------------------------------------------------

	[Fact]
	public async Task GhRelease_WithValidRelease_CreatesBundleFile()
	{
		// Arrange — one PR from the commit range
		ArrangeRelease("v9.2.0", 12345);

		A.CallTo(() => _mockPrService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<Cancel>._)).Returns(new GitHubPrInfo
		{
			Title = "Add aggregation API",
			Labels = []
		});

		var outputDir = CreateOutputDirectory();
		FileSystem.Directory.CreateDirectory(outputDir);

		var service = CreateService();
		var input = new CreateChangelogsFromReleaseArguments
		{
			Repository = "elastic/elasticsearch",
			Version = "v9.2.0",
			Output = outputDir,
			CreateBundle = true
		};

		// Act
		var result = await service.CreateChangelogsFromRelease(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundlesDir = FileSystem.Path.Join(outputDir, "bundles");
		FileSystem.Directory.Exists(bundlesDir).Should().BeTrue();
		var bundleFiles = FileSystem.Directory.GetFiles(bundlesDir, "*.yml");
		bundleFiles.Should().HaveCount(1, "a bundle file should be created when CreateBundle = true");
	}

	// -----------------------------------------------------------------------
	// Latest tag: FetchReleaseAsync is called with "latest"
	// -----------------------------------------------------------------------

	[Fact]
	public async Task ReleaseVersion_Latest_CallsFetchWithLatestTag()
	{
		// Arrange — "latest" resolves to v9.2.0; commit range returns zero PRs
		ArrangeRelease("latest");

		var service = CreateService();
		var input = new CreateChangelogsFromReleaseArguments
		{
			Repository = "elastic/elasticsearch",
			Version = "latest",
			Output = CreateOutputDirectory(),
			CreateBundle = false
		};

		// Act
		_ = await service.CreateChangelogsFromRelease(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		A.CallTo(
			() => _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "latest", A<Cancel>._)
		).MustHaveHappenedOnceExactly();
	}

	// -----------------------------------------------------------------------
	// Release fetch failure
	// -----------------------------------------------------------------------

	[Fact]
	public async Task ReleaseVersion_FetchFailure_ReturnsError()
	{
		// Arrange
		A.CallTo(() => _mockReleaseService.FetchReleaseAsync(A<string>._, A<string>._, A<string?>._, A<Cancel>._)).Returns(
			(GitHubReleaseInfo?)null
		);

		var service = CreateService();
		var input = new CreateChangelogsFromReleaseArguments
		{
			Repository = "elastic/elasticsearch",
			Version = "v9.2.0",
			Output = CreateOutputDirectory(),
			CreateBundle = false
		};

		// Act
		var result = await service.CreateChangelogsFromRelease(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
	}

	// -----------------------------------------------------------------------
	// Unknown repository: no product found
	// -----------------------------------------------------------------------

	[Fact]
	public async Task ReleaseVersion_UnknownRepo_ReturnsError()
	{
		// Arrange – "unknown-repo" is not registered in ConfigurationContext.ProductsConfiguration
		var service = CreateService();
		var input = new CreateChangelogsFromReleaseArguments
		{
			Repository = "elastic/unknown-repo",
			Version = "v9.2.0",
			Output = CreateOutputDirectory(),
			CreateBundle = false
		};

		// Act
		var result = await service.CreateChangelogsFromRelease(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Success.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("unknown-repo"));
	}

	// -----------------------------------------------------------------------
	// Config fallback: Output = null → service defaults to ./changelogs
	// When the command resolves no --output and no bundle.directory from config,
	// it passes Output = null to the service, which then uses "./changelogs".
	// -----------------------------------------------------------------------

	[Fact]
	public async Task ReleaseVersion_OutputNull_ServiceUsesChangelogsDefault()
	{
		// Arrange — one PR, no explicit output dir
		ArrangeRelease("v9.2.0", 12345);

		A.CallTo(() => _mockPrService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<Cancel>._)).Returns(new GitHubPrInfo
		{
			Title = "Fix something",
			Labels = []
		});

		var workDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(workDir);
		var originalDir = FileSystem.Directory.GetCurrentDirectory();
		try
		{
			FileSystem.Directory.SetCurrentDirectory(workDir);

			var service = CreateService();
			var input = new CreateChangelogsFromReleaseArguments
			{
				Repository = "elastic/elasticsearch",
				Version = "v9.2.0",
				Output = null, // no --output CLI and no bundle.directory in config

				CreateBundle = false
			};

			// Act
			var result = await service.CreateChangelogsFromRelease(Collector, input, TestContext.Current.CancellationToken);

			// Assert – service resolves output to <cwd>/changelogs
			result.Success.Should().BeTrue();
			var expectedOutputDir = FileSystem.Path.Join(workDir, "changelogs");
			FileSystem.Directory.Exists(expectedOutputDir).Should().BeTrue("service defaults Output to ./changelogs when null");
			FileSystem.Directory.GetFiles(expectedOutputDir, "*.yaml").Should().HaveCount(1);
		}
		finally
		{
			FileSystem.Directory.SetCurrentDirectory(originalDir);
		}
	}

	/// <summary>Answers every checked-in entry pool probe as absent, so tests degrade to PR-metadata synthesis.</summary>
	private sealed class OfflinePoolHandler : HttpMessageHandler
	{
		protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) =>
			new(HttpStatusCode.NotFound);

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(Send(request, cancellationToken));
	}
}
