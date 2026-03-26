// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.GitHub;
using Elastic.Documentation.Diagnostics;
using FakeItEasy;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// Tests for profile-based bundling using <c>source: github_release</c>.
/// </summary>
public class BundleProfileGitHubReleaseTests : ChangelogTestBase
{
	private readonly IGitHubReleaseService _mockReleaseService;
	private readonly ChangelogBundlingService _service;
	private readonly string _changelogDir;

	public BundleProfileGitHubReleaseTests(ITestOutputHelper output) : base(output)
	{
		_mockReleaseService = A.Fake<IGitHubReleaseService>();
		_service = new ChangelogBundlingService(LoggerFactory, ConfigurationContext, FileSystem, _mockReleaseService);

		_changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(_changelogDir);
	}

	private async Task<string> CreateConfigAsync(string configContent)
	{
		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);
		return configPath;
	}

	[Fact]
	public async Task ProfileGitHubRelease_BundlesMatchingChangelogs()
	{
		// Arrange — profile uses source: github_release; the release contains two PR references
		// that match changelogs already in the input directory.

		// language=yaml
		var configContent =
			"""
			bundle:
			  directory: PLACEHOLDER
			  owner: elastic
			  profiles:
			    es-gh-release:
			      source: github_release
			      repo: elasticsearch
			      output: "elasticsearch-{version}.yaml"
			      output_products: "elasticsearch {version} {lifecycle}"
			""".Replace("PLACEHOLDER", _changelogDir);

		var configPath = await CreateConfigAsync(configContent);

		// language=yaml
		var changelog1 =
			"""
			title: First feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		// language=yaml
		var changelog2 =
			"""
			title: Second feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/200
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-first-feature.yaml");
		var file2 = FileSystem.Path.Combine(_changelogDir, "1755268140-second-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		// GitHub release body in standard GitHub format
		var releaseBody =
			"""
			## What's Changed
			* First feature by @user1 in https://github.com/elastic/elasticsearch/pull/100
			* Second feature by @user2 in https://github.com/elastic/elasticsearch/pull/200
			""";

		A.CallTo(() => _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "9.2.0", TestContext.Current.CancellationToken))
			.Returns(new GitHubReleaseInfo { TagName = "v9.2.0", Name = "9.2.0", Body = releaseBody });

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);

		var input = new BundleChangelogsArguments
		{
			Profile = "es-gh-release",
			ProfileArgument = "9.2.0",
			Config = configPath,
			OutputDirectory = outputDir
		};

		// Act
		var result = await _service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var outputFiles = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		outputFiles.Should().NotBeEmpty("Expected a bundle output file");
		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputFiles[0], TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("1755268130-first-feature.yaml");
		bundleContent.Should().Contain("1755268140-second-feature.yaml");
	}

	[Fact]
	public async Task ProfileGitHubRelease_AutoInfersVersionAndLifecycle_FromReleaseTag()
	{
		// Arrange — the release tag is "v9.2.0"; the output filename and output_products should
		// use the clean version "9.2.0" and inferred lifecycle "ga".

		// language=yaml
		var configContent =
			"""
			bundle:
			  directory: PLACEHOLDER
			  owner: elastic
			  profiles:
			    es-gh-release:
			      source: github_release
			      repo: elasticsearch
			      output: "elasticsearch-{version}.yaml"
			      output_products: "elasticsearch {version} {lifecycle}"
			""".Replace("PLACEHOLDER", _changelogDir);

		var configPath = await CreateConfigAsync(configContent);

		// language=yaml
		var changelog1 =
			"""
			title: Some feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-some-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var releaseBody = "* Some feature by @user in https://github.com/elastic/elasticsearch/pull/100\n";

		// Return a tag with a "v" prefix to verify that ExtractBaseVersion strips it
		A.CallTo(() => _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "9.2.0", TestContext.Current.CancellationToken))
			.Returns(new GitHubReleaseInfo { TagName = "v9.2.0", Name = "9.2.0", Body = releaseBody });

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);

		var input = new BundleChangelogsArguments
		{
			Profile = "es-gh-release",
			ProfileArgument = "9.2.0",
			Config = configPath,
			OutputDirectory = outputDir
		};

		// Act
		var result = await _service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		// Output file should be named using the clean version
		var outputFiles = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		outputFiles.Should().NotBeEmpty();
		outputFiles.Should().Contain(f => f.EndsWith("elasticsearch-9.2.0.yaml"), "Output filename should use clean version");

		// Bundle products should use inferred lifecycle "ga"
		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputFiles[0], TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("target: 9.2.0");
		bundleContent.Should().Contain("lifecycle: ga");
	}

	[Fact]
	public async Task ProfileGitHubRelease_WithNoMatchingPrs_EmitsWarning()
	{
		// Arrange — release notes contain no PR references; expect a warning and no bundle.

		// language=yaml
		var configContent =
			"""
			bundle:
			  directory: PLACEHOLDER
			  owner: elastic
			  profiles:
			    es-gh-release:
			      source: github_release
			      repo: elasticsearch
			      output: "elasticsearch-{version}.yaml"
			""".Replace("PLACEHOLDER", _changelogDir);

		var configPath = await CreateConfigAsync(configContent);

		var releaseBody = "No pull requests in this release.";

		A.CallTo(() => _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "9.2.0", TestContext.Current.CancellationToken))
			.Returns(new GitHubReleaseInfo { TagName = "v9.2.0", Name = "9.2.0", Body = releaseBody });

		var input = new BundleChangelogsArguments
		{
			Profile = "es-gh-release",
			ProfileArgument = "9.2.0",
			Config = configPath
		};

		// Act
		var result = await _service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse("Should fail when no PR references found in the release");
		Collector.Diagnostics.Should().Contain(d =>
			d.Message.Contains("no PR references found"),
			"Should emit a warning about missing PR references"
		);
	}

	[Fact]
	public async Task ProfileGitHubRelease_FetchFailure_ReturnsError()
	{
		// Arrange — FetchReleaseAsync returns null (network failure or tag not found).

		// language=yaml
		var configContent =
			"""
			bundle:
			  owner: elastic
			  profiles:
			    es-gh-release:
			      source: github_release
			      repo: elasticsearch
			      output: "elasticsearch-{version}.yaml"
			""";

		var configPath = await CreateConfigAsync(configContent);

		A.CallTo(() => _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "9.2.0", TestContext.Current.CancellationToken))
			.Returns((GitHubReleaseInfo?)null);

		var input = new BundleChangelogsArguments
		{
			Profile = "es-gh-release",
			ProfileArgument = "9.2.0",
			Config = configPath
		};

		// Act
		var result = await _service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse("Should fail when the release cannot be fetched");
		Collector.Errors.Should().BeGreaterThan(0);
	}

	[Fact]
	public async Task ProfileGitHubRelease_Latest_CallsFetchWithLatestTag()
	{
		// Arrange — passing "latest" as the version should forward "latest" to FetchReleaseAsync.

		// language=yaml
		var configContent =
			"""
			bundle:
			  directory: PLACEHOLDER
			  owner: elastic
			  profiles:
			    es-gh-release:
			      source: github_release
			      repo: elasticsearch
			      output: "elasticsearch-{version}.yaml"
			      output_products: "elasticsearch {version} {lifecycle}"
			""".Replace("PLACEHOLDER", _changelogDir);

		var configPath = await CreateConfigAsync(configContent);

		// language=yaml
		var changelog1 =
			"""
			title: Latest feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/999
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-latest-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var releaseBody = "* Latest feature by @user in https://github.com/elastic/elasticsearch/pull/999\n";

		A.CallTo(() => _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "latest", TestContext.Current.CancellationToken))
			.Returns(new GitHubReleaseInfo { TagName = "v9.2.0", Name = "9.2.0", Body = releaseBody });

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);

		var input = new BundleChangelogsArguments
		{
			Profile = "es-gh-release",
			ProfileArgument = "latest",
			Config = configPath,
			OutputDirectory = outputDir
		};

		// Act
		var result = await _service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		A.CallTo(() => _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "latest", TestContext.Current.CancellationToken))
			.MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task ProfileGitHubRelease_RequiresRepo_ReturnsError()
	{
		// Arrange — no repo set at profile or bundle level; expect an error.

		// language=yaml
		var configContent =
			"""
			bundle:
			  profiles:
			    es-gh-release:
			      source: github_release
			      output: "elasticsearch-{version}.yaml"
			""";

		var configPath = await CreateConfigAsync(configContent);

		var input = new BundleChangelogsArguments
		{
			Profile = "es-gh-release",
			ProfileArgument = "9.2.0",
			Config = configPath
		};

		// Act
		var result = await _service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse("Should fail when no repo is configured");
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error && d.Message.Contains("requires a GitHub repository name"),
			"Should emit an error about missing repo"
		);
	}

	[Fact]
	public async Task ProfileGitHubRelease_MutuallyExclusiveWithProducts_ReturnsError()
	{
		// Arrange — combining source: github_release with a products filter is not allowed.

		// language=yaml
		var configContent =
			"""
			bundle:
			  owner: elastic
			  profiles:
			    es-gh-release:
			      source: github_release
			      repo: elasticsearch
			      products: "elasticsearch {version} {lifecycle}"
			      output: "elasticsearch-{version}.yaml"
			""";

		var configPath = await CreateConfigAsync(configContent);

		var input = new BundleChangelogsArguments
		{
			Profile = "es-gh-release",
			ProfileArgument = "9.2.0",
			Config = configPath
		};

		// Act
		var result = await _service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse("Should fail when source and products are both configured");
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error && d.Message.Contains("cannot be combined with a 'products' filter"),
			"Should emit an error about the mutual exclusivity"
		);
	}

	[Fact]
	public async Task ProfileGitHubRelease_MutuallyExclusiveWithPromotionReport_ReturnsError()
	{
		// Arrange — providing a profileReport (third argument) alongside source: github_release is not allowed.

		// language=yaml
		var configContent =
			"""
			bundle:
			  owner: elastic
			  profiles:
			    es-gh-release:
			      source: github_release
			      repo: elasticsearch
			      output: "elasticsearch-{version}.yaml"
			""";

		var configPath = await CreateConfigAsync(configContent);

		var input = new BundleChangelogsArguments
		{
			Profile = "es-gh-release",
			ProfileArgument = "9.2.0",
			ProfileReport = "./some-report.html",
			Config = configPath
		};

		// Act
		var result = await _service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse("Should fail when profileReport is provided alongside source: github_release");
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error && d.Message.Contains("does not accept a third positional argument"),
			"Should emit an error about the mutual exclusivity with hint to hardcode lifecycle"
		);
	}

	[Fact]
	public async Task ProfileGitHubRelease_InfersBetaLifecycle_FromTagSuffix()
	{
		// Arrange — release tag is "v9.2.0-beta.1"; {lifecycle} in output_products should be "beta"
		// even though ExtractBaseVersion strips the suffix to produce version "9.2.0".

		// language=yaml
		var configContent =
			"""
			bundle:
			  directory: PLACEHOLDER
			  owner: elastic
			  profiles:
			    es-gh-release:
			      source: github_release
			      repo: elasticsearch
			      output: "elasticsearch-{version}.yaml"
			      output_products: "elasticsearch {version} {lifecycle}"
			""".Replace("PLACEHOLDER", _changelogDir);

		var configPath = await CreateConfigAsync(configContent);

		// language=yaml
		var changelog1 =
			"""
			title: Beta feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: beta
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-beta-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var releaseBody = "* Beta feature by @user in https://github.com/elastic/elasticsearch/pull/100\n";

		A.CallTo(() => _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "9.2.0-beta.1", TestContext.Current.CancellationToken))
			.Returns(new GitHubReleaseInfo { TagName = "v9.2.0-beta.1", Name = "9.2.0 beta 1", Body = releaseBody });

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);

		var input = new BundleChangelogsArguments
		{
			Profile = "es-gh-release",
			ProfileArgument = "9.2.0-beta.1",
			Config = configPath,
			OutputDirectory = outputDir
		};

		// Act
		var result = await _service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		// Output filename should use the clean base version, not the full pre-release tag
		var outputFiles = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		outputFiles.Should().Contain(f => f.EndsWith("elasticsearch-9.2.0.yaml"), "Output filename should use clean base version");

		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputFiles[0], TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("target: 9.2.0", "target should be the clean base version");
		bundleContent.Should().Contain("lifecycle: beta", "lifecycle should be inferred from the pre-release tag suffix");
	}

	[Fact]
	public async Task ProfileGitHubRelease_InfersPreviewLifecycle_FromTagSuffix()
	{
		// Arrange — release tag is "v1.34.1-preview.1"; {lifecycle} should be "preview".

		// language=yaml
		var configContent =
			"""
			bundle:
			  directory: PLACEHOLDER
			  owner: elastic
			  profiles:
			    es-gh-release:
			      source: github_release
			      repo: apm-agent-dotnet
			      output: "apm-agent-dotnet-{version}.yaml"
			      output_products: "apm-agent-dotnet {version} {lifecycle}"
			""".Replace("PLACEHOLDER", _changelogDir);

		var configPath = await CreateConfigAsync(configContent);

		// language=yaml
		var changelog1 =
			"""
			title: Preview feature
			type: feature
			products:
			  - product: apm-agent-dotnet
			    target: 1.34.1
			    lifecycle: preview
			prs:
			  - https://github.com/elastic/apm-agent-dotnet/pull/42
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-preview-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var releaseBody = "* Preview feature by @user in https://github.com/elastic/apm-agent-dotnet/pull/42\n";

		A.CallTo(() => _mockReleaseService.FetchReleaseAsync("elastic", "apm-agent-dotnet", "v1.34.1-preview.1", TestContext.Current.CancellationToken))
			.Returns(new GitHubReleaseInfo { TagName = "v1.34.1-preview.1", Name = "1.34.1 preview 1", Body = releaseBody });

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);

		var input = new BundleChangelogsArguments
		{
			Profile = "es-gh-release",
			ProfileArgument = "v1.34.1-preview.1",
			Config = configPath,
			OutputDirectory = outputDir
		};

		// Act
		var result = await _service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var outputFiles = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		outputFiles.Should().Contain(f => f.EndsWith("apm-agent-dotnet-1.34.1.yaml"), "Output filename should use clean base version");

		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputFiles[0], TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("target: 1.34.1", "target should be the clean base version");
		bundleContent.Should().Contain("lifecycle: preview", "lifecycle should be inferred from the pre-release tag suffix");
	}

	[Fact]
	public async Task ProfileGitHubRelease_BundleLevelRepo_UsedWhenProfileOmitsRepo()
	{
		// Arrange — no repo at profile level; bundle.repo should be used as the fallback.

		// language=yaml
		var configContent =
			"""
			bundle:
			  directory: PLACEHOLDER
			  repo: elasticsearch
			  owner: elastic
			  profiles:
			    es-gh-release:
			      source: github_release
			      output: "elasticsearch-{version}.yaml"
			      output_products: "elasticsearch {version} {lifecycle}"
			""".Replace("PLACEHOLDER", _changelogDir);

		var configPath = await CreateConfigAsync(configContent);

		// language=yaml
		var changelog1 =
			"""
			title: Some feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-some-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var releaseBody = "* Some feature by @user in https://github.com/elastic/elasticsearch/pull/100\n";

		// Expect the call to use bundle-level repo "elasticsearch" and owner "elastic"
		A.CallTo(() => _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "9.2.0", TestContext.Current.CancellationToken))
			.Returns(new GitHubReleaseInfo { TagName = "v9.2.0", Name = "9.2.0", Body = releaseBody });

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);

		var input = new BundleChangelogsArguments
		{
			Profile = "es-gh-release",
			ProfileArgument = "9.2.0",
			Config = configPath,
			OutputDirectory = outputDir
		};

		// Act
		var result = await _service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		A.CallTo(() => _mockReleaseService.FetchReleaseAsync("elastic", "elasticsearch", "9.2.0", TestContext.Current.CancellationToken))
			.MustHaveHappenedOnceExactly();
	}
}
