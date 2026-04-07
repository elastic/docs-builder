// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Extensions;

namespace Elastic.Changelog.Tests.Changelogs;

public class BundlePlanTests : ChangelogTestBase
{
	private ChangelogBundlingService Service { get; }

	public BundlePlanTests(ITestOutputHelper output) : base(output) =>
		Service = new(LoggerFactory, ConfigurationContext, FileSystem);

	private async Task<string> CreateConfigAsync(string configContent)
	{
		var configPath = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);
		return configPath;
	}

	[Fact]
	public async Task Plan_OptionMode_ExplicitOutput_ReturnsNoNetwork()
	{
		var input = new BundleChangelogsArguments { Output = "docs/releases/my-bundle.yaml" };

		var result = await Service.PlanBundleAsync(Collector, input, hasReleaseVersion: false, TestContext.Current.CancellationToken);

		result.Should().NotBeNull();
		result.NeedsNetwork.Should().BeFalse();
		result.NeedsGithubToken.Should().BeFalse();
		result.OutputPath.Should().Be("docs/releases/my-bundle.yaml");
	}

	[Fact]
	public async Task Plan_ReleaseVersion_ReturnsNeedsNetwork()
	{
		var input = new BundleChangelogsArguments { Output = "docs/releases/bundle.yaml" };

		var result = await Service.PlanBundleAsync(Collector, input, hasReleaseVersion: true, TestContext.Current.CancellationToken);

		result.Should().NotBeNull();
		result.NeedsNetwork.Should().BeTrue();
		result.NeedsGithubToken.Should().BeTrue();
	}

	[Fact]
	public async Task Plan_ProfileMode_ResolvesOutputPath()
	{
		// language=yaml
		var configContent =
			"""
			bundle:
			  output_directory: docs/releases
			  profiles:
			    my-profile:
			      products: "elasticsearch {version} {lifecycle}"
			      output: "elasticsearch-{version}.yaml"
			""";
		var configPath = await CreateConfigAsync(configContent);

		var input = new BundleChangelogsArguments
		{
			Profile = "my-profile",
			ProfileArgument = "9.2.0",
			Config = configPath
		};

		var result = await Service.PlanBundleAsync(Collector, input, hasReleaseVersion: false, TestContext.Current.CancellationToken);

		result.Should().NotBeNull();
		result.NeedsNetwork.Should().BeFalse();
		result.NeedsGithubToken.Should().BeFalse();
		result.OutputPath.Should().EndWith(FileSystem.Path.Join("docs", "releases", "elasticsearch-9.2.0.yaml").OptionalWindowsReplace());
	}

	[Fact]
	public async Task Plan_ProfileMode_GitHubRelease_ReturnsNeedsNetwork()
	{
		// language=yaml
		var configContent =
			"""
			bundle:
			  output_directory: docs/releases
			  profiles:
			    es-release:
			      source: github_release
			      repo: elasticsearch
			      output: "elasticsearch-{version}.yaml"
			""";
		var configPath = await CreateConfigAsync(configContent);

		var input = new BundleChangelogsArguments
		{
			Profile = "es-release",
			ProfileArgument = "v9.2.0",
			Config = configPath
		};

		var result = await Service.PlanBundleAsync(Collector, input, hasReleaseVersion: false, TestContext.Current.CancellationToken);

		result.Should().NotBeNull();
		result.NeedsNetwork.Should().BeTrue();
		result.NeedsGithubToken.Should().BeTrue();
		result.OutputPath.Should().EndWith(FileSystem.Path.Join("docs", "releases", "elasticsearch-v9.2.0.yaml").OptionalWindowsReplace());
	}

	[Fact]
	public async Task Plan_ProfileMode_LifecycleSubstitution_ResolvesCorrectly()
	{
		// language=yaml
		var configContent =
			"""
			bundle:
			  output_directory: docs/releases
			  profiles:
			    dotnet-release:
			      source: github_release
			      repo: apm-agent-dotnet
			      output: "dotnet-{version}-{lifecycle}.yaml"
			""";
		var configPath = await CreateConfigAsync(configContent);

		var input = new BundleChangelogsArguments
		{
			Profile = "dotnet-release",
			ProfileArgument = "1.0.0-beta.1",
			Config = configPath
		};

		var result = await Service.PlanBundleAsync(Collector, input, hasReleaseVersion: false, TestContext.Current.CancellationToken);

		result.Should().NotBeNull();
		result.OutputPath.Should().EndWith(FileSystem.Path.Join("docs", "releases", "dotnet-1.0.0-beta.1-beta.yaml").OptionalWindowsReplace());
	}

	[Fact]
	public async Task Plan_NoOutput_FallsBackToConfigOutputDirectory()
	{
		// language=yaml
		var configContent =
			"""
			bundle:
			  output_directory: docs/releases
			""";
		var configPath = await CreateConfigAsync(configContent);

		var input = new BundleChangelogsArguments { Config = configPath };

		var result = await Service.PlanBundleAsync(Collector, input, hasReleaseVersion: false, TestContext.Current.CancellationToken);

		result.Should().NotBeNull();
		result.OutputPath.Should().EndWith(FileSystem.Path.Join("docs", "releases", "changelog-bundle.yaml").OptionalWindowsReplace());
	}

	[Fact]
	public async Task Plan_ProfileNotFound_ReturnsResultWithNeedsNetworkFalse()
	{
		// language=yaml
		var configContent =
			"""
			bundle:
			  profiles:
			    existing-profile:
			      products: "elasticsearch {version} {lifecycle}"
			""";
		var configPath = await CreateConfigAsync(configContent);

		var input = new BundleChangelogsArguments
		{
			Profile = "nonexistent-profile",
			ProfileArgument = "9.2.0",
			Config = configPath
		};

		var result = await Service.PlanBundleAsync(Collector, input, hasReleaseVersion: false, TestContext.Current.CancellationToken);

		result.Should().NotBeNull();
		result.NeedsNetwork.Should().BeFalse();
	}

}
