// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Creation;
using Elastic.Changelog.GitHub;
using Elastic.Changelog.Tests.Changelogs;
using Elastic.Documentation.Configuration;
using FakeItEasy;

namespace Elastic.Changelog.Tests.Creation;

public class ChangelogCreationServiceTests(ITestOutputHelper output) : ChangelogTestBase(output)
{
	private readonly IGitHubPrService _mockGitHub = A.Fake<IGitHubPrService>();

	private const string ConfigWithProductLabels = """
		pivot:
		  types:
		    feature: ">feature"
		    bug-fix: ">bug"
		    breaking-change: ">breaking"
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

	private async Task WriteConfig(string content, string? path = null)
	{
		path ??= Path.Join(Paths.WorkingDirectoryRoot.FullName, "config", "changelog.yml");
		var dir = FileSystem.Path.GetDirectoryName(path)!;
		FileSystem.Directory.CreateDirectory(dir);
		await FileSystem.File.WriteAllTextAsync(path, content);
	}

	private static IEnvironmentVariables FakeCIEnv(
		string? prNumber = null,
		string? title = null,
		string? type = null,
		string? owner = null,
		string? repo = null,
		string? products = null)
	{
		var env = A.Fake<IEnvironmentVariables>();
		A.CallTo(() => env.IsRunningOnCI).Returns(true);
		A.CallTo(() => env.GetEnvironmentVariable("CHANGELOG_PR_NUMBER")).Returns(prNumber);
		A.CallTo(() => env.GetEnvironmentVariable("CHANGELOG_TITLE")).Returns(title);
		A.CallTo(() => env.GetEnvironmentVariable("CHANGELOG_TYPE")).Returns(type);
		A.CallTo(() => env.GetEnvironmentVariable("CHANGELOG_OWNER")).Returns(owner);
		A.CallTo(() => env.GetEnvironmentVariable("CHANGELOG_REPO")).Returns(repo);
		A.CallTo(() => env.GetEnvironmentVariable("CHANGELOG_PRODUCTS")).Returns(products);
		return env;
	}

	/// <summary>
	/// When all CHANGELOG_* env vars are provided (including CHANGELOG_PRODUCTS),
	/// changelog add should succeed without making any GitHub API calls.
	/// </summary>
	[Fact]
	public async Task CreateChangelog_CIWithProducts_SkipsPrFetchAndSucceeds()
	{
		await WriteConfig(ConfigWithProductLabels);
		FileSystem.Directory.CreateDirectory(Path.Join(Paths.WorkingDirectoryRoot.FullName, "output"));

		var env = FakeCIEnv(
			prNumber: "153344",
			title: "Cache tfconsole lookups and batch terraform console calls",
			type: "enhancement",
			owner: "elastic",
			repo: "cloud",
			products: "cloud-hosted, cloud-serverless"
		);

		var service = new ChangelogCreationService(LoggerFactory, ConfigurationContext, _mockGitHub, FileSystem, env);
		var input = new CreateChangelogArguments
		{
			Products = [],
			Config = Path.Join(Paths.WorkingDirectoryRoot.FullName, "config", "changelog.yml"),
			Output = Path.Join(Paths.WorkingDirectoryRoot.FullName, "output"),
			Concise = true
		};

		var result = await service.CreateChangelog(Collector, input, CancellationToken.None);

		A.CallTo(() => _mockGitHub.FetchPrInfoAsync(A<string>._, A<string>._, A<string>._, A<CancellationToken>._))
			.MustNotHaveHappened();

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
	}

	/// <summary>
	/// When CI provides title+type but NOT products, changelog add falls back to
	/// fetching the PR from the API and resolving products from labels.
	/// </summary>
	[Fact]
	public async Task CreateChangelog_CIWithoutProducts_FallsBackToPrFetchForProducts()
	{
		await WriteConfig(ConfigWithProductLabels);
		FileSystem.Directory.CreateDirectory(Path.Join(Paths.WorkingDirectoryRoot.FullName, "output"));

		A.CallTo(() => _mockGitHub.FetchPrInfoAsync("153344", "elastic", "cloud", A<CancellationToken>._))
			.Returns(new GitHubPrInfo
			{
				Title = "Cache tfconsole lookups and batch terraform console calls",
				Labels = [">enhancement", "@Product:ECH", "@Product:ESS", "@Public"]
			});

		var env = FakeCIEnv(
			prNumber: "153344",
			title: "Cache tfconsole lookups and batch terraform console calls",
			type: "enhancement",
			owner: "elastic",
			repo: "cloud"
		);

		var service = new ChangelogCreationService(LoggerFactory, ConfigurationContext, _mockGitHub, FileSystem, env);
		var input = new CreateChangelogArguments
		{
			Products = [],
			Config = Path.Join(Paths.WorkingDirectoryRoot.FullName, "config", "changelog.yml"),
			Output = Path.Join(Paths.WorkingDirectoryRoot.FullName, "output"),
			Concise = true
		};

		var result = await service.CreateChangelog(Collector, input, CancellationToken.None);

		A.CallTo(() => _mockGitHub.FetchPrInfoAsync("153344", "elastic", "cloud", A<CancellationToken>._))
			.MustHaveHappenedOnceExactly();

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
	}

	/// <summary>
	/// When CI provides title+type but NOT products, the PR has no product labels,
	/// and the repo name doesn't match any product ID, the command fails.
	/// </summary>
	[Fact]
	public async Task CreateChangelog_CIWithoutProducts_NoPrProductLabels_FailsWithProductRequired()
	{
		await WriteConfig(ConfigWithProductLabels);
		FileSystem.Directory.CreateDirectory(Path.Join(Paths.WorkingDirectoryRoot.FullName, "output"));

		A.CallTo(() => _mockGitHub.FetchPrInfoAsync("153344", "elastic", "cloud", A<CancellationToken>._))
			.Returns(new GitHubPrInfo
			{
				Title = "Cache tfconsole lookups and batch terraform console calls",
				Labels = [">enhancement", "@Public"]
			});

		var env = FakeCIEnv(
			prNumber: "153344",
			title: "Cache tfconsole lookups and batch terraform console calls",
			type: "enhancement",
			owner: "elastic",
			repo: "cloud"
		);

		var service = new ChangelogCreationService(LoggerFactory, ConfigurationContext, _mockGitHub, FileSystem, env);
		var input = new CreateChangelogArguments
		{
			Products = [],
			Config = Path.Join(Paths.WorkingDirectoryRoot.FullName, "config", "changelog.yml"),
			Output = Path.Join(Paths.WorkingDirectoryRoot.FullName, "output"),
			Concise = true
		};

		var result = await service.CreateChangelog(Collector, input, CancellationToken.None);

		A.CallTo(() => _mockGitHub.FetchPrInfoAsync("153344", "elastic", "cloud", A<CancellationToken>._))
			.MustHaveHappenedOnceExactly();

		result.Should().BeFalse();
		Collector.Errors.Should().Be(1);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("At least one product is required"));
	}
}
