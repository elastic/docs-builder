// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.GitHub;
using Elastic.Changelog.GithubRelease;
using FakeItEasy;
using AwesomeAssertions;
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

	private GitHubReleaseChangelogService CreateService() =>
		new(LoggerFactory, ConfigurationContext, _mockReleaseService, _mockPrService, FileSystem);

	private string CreateOutputDirectory() =>
		FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

	// -----------------------------------------------------------------------
	// Validation: no PR refs in release notes
	// -----------------------------------------------------------------------

	[Fact]
	public async Task ReleaseVersion_WithNoMatchingPrs_EmitsWarningAndSucceeds()
	{
		// Arrange
		A.CallTo(() => _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "v9.2.0", A<Cancel>._))
			.Returns(new GitHubReleaseInfo
			{
				TagName = "v9.2.0",
				Name = "9.2.0",
				Body = "No pull request references in these release notes."
			});

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
		result.Should().BeTrue();
		Collector.Diagnostics.Should().Contain(d =>
			d.Message.Contains("No PR references found") && d.Severity == Documentation.Diagnostics.Severity.Warning);
	}

	// -----------------------------------------------------------------------
	// CreateBundle = false: no bundle file is written
	// -----------------------------------------------------------------------

	[Fact]
	public async Task ReleaseVersion_WithValidRelease_CreatesChangelogFiles_AndNoBundleFile()
	{
		// Arrange – GitHub Default format body with two PR references
		// Parser expects: "* Title by @author in #NNN"
		var releaseBody =
			"""
			## What's Changed

			* Fix query parsing edge case by @contributor1 in #12345
			* Resolve memory leak in shard recovery by @contributor2 in #12346

			**Full Changelog**: https://github.com/elastic/elasticsearch/compare/v9.1.0...v9.2.0
			""";

		A.CallTo(() => _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "v9.2.0", A<Cancel>._))
			.Returns(new GitHubReleaseInfo { TagName = "v9.2.0", Name = "9.2.0", Body = releaseBody });

		A.CallTo(() => _mockPrService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<Cancel>._))
			.Returns(new GitHubPrInfo { Title = "PR title", Labels = [] });

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
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var yamlFiles = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		yamlFiles.Should().HaveCount(2, "one changelog file per PR reference");

		// No bundle file in the output directory or bundles subdirectory
		var bundlesDir = FileSystem.Path.Combine(outputDir, "bundles");
		FileSystem.Directory.Exists(bundlesDir).Should().BeFalse("CreateBundle = false must not create a bundles directory");
	}

	// -----------------------------------------------------------------------
	// CreateBundle = true (default): bundle file is written
	// -----------------------------------------------------------------------

	[Fact]
	public async Task GhRelease_WithValidRelease_CreatesBundleFile()
	{
		// Arrange – GitHub Default format body with one PR reference
		var releaseBody =
			"""
			## What's Changed

			* Add new aggregation API by @contributor1 in #12345

			**Full Changelog**: https://github.com/elastic/elasticsearch/compare/v9.1.0...v9.2.0
			""";

		A.CallTo(() => _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "v9.2.0", A<Cancel>._))
			.Returns(new GitHubReleaseInfo { TagName = "v9.2.0", Name = "9.2.0", Body = releaseBody });

		A.CallTo(() => _mockPrService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<Cancel>._))
			.Returns(new GitHubPrInfo { Title = "Add aggregation API", Labels = [] });

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
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundlesDir = FileSystem.Path.Combine(outputDir, "bundles");
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
		// Arrange
		A.CallTo(() => _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "latest", A<Cancel>._))
			.Returns(new GitHubReleaseInfo
			{
				TagName = "v9.2.0",
				Name = "9.2.0",
				Body = "No PR references."
			});

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
		A.CallTo(() => _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "latest", A<Cancel>._))
			.MustHaveHappenedOnceExactly();
	}

	// -----------------------------------------------------------------------
	// Release fetch failure
	// -----------------------------------------------------------------------

	[Fact]
	public async Task ReleaseVersion_FetchFailure_ReturnsError()
	{
		// Arrange
		A.CallTo(() => _mockReleaseService.FetchReleaseAsync(A<string>._, A<string>._, A<string?>._, A<Cancel>._))
			.Returns((GitHubReleaseInfo?)null);

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
		result.Should().BeFalse();
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
		result.Should().BeFalse();
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
		// Arrange – simulates 'changelog add --release-version' with no --output and no bundle.directory in config.
		// The command passes Output = null to the service; the service must default to "./changelogs".
		A.CallTo(() => _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "v9.2.0", A<Cancel>._))
			.Returns(new GitHubReleaseInfo
			{
				TagName = "v9.2.0",
				Name = "9.2.0",
				Body = "* Fix something by @contributor in #12345"
			});

		A.CallTo(() => _mockPrService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<Cancel>._))
			.Returns(new GitHubPrInfo { Title = "Fix something", Labels = [] });

		var workDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
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
				Output = null,   // no --output CLI and no bundle.directory in config
				CreateBundle = false
			};

			// Act
			var result = await service.CreateChangelogsFromRelease(Collector, input, TestContext.Current.CancellationToken);

			// Assert – service resolves output to <cwd>/changelogs
			result.Should().BeTrue();
			var expectedOutputDir = FileSystem.Path.Combine(workDir, "changelogs");
			FileSystem.Directory.Exists(expectedOutputDir).Should().BeTrue("service defaults Output to ./changelogs when null");
			FileSystem.Directory.GetFiles(expectedOutputDir, "*.yaml").Should().HaveCount(1);
		}
		finally
		{
			FileSystem.Directory.SetCurrentDirectory(originalDir);
		}
	}
}
