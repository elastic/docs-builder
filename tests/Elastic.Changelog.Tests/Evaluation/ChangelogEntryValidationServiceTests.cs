// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using AwesomeAssertions;
using Elastic.Changelog.Evaluation;
using Elastic.Changelog.GitHub;
using Elastic.Changelog.Tests.Changelogs;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Diagnostics;
using FakeItEasy;

namespace Elastic.Changelog.Tests.Evaluation;

public class ChangelogEntryValidationServiceTests(ITestOutputHelper output) : ChangelogTestBase(output)
{
	private static readonly string Root = Paths.WorkingDirectoryRoot.FullName;

	private string ConfigPath => Path.Join(Root, "changelog.yml");

	private const string MinimalConfig =
		"""
		pivot:
		  types:
		    feature: "type:feature"
		    bug-fix:
		    breaking-change:
		""";

	private async Task WriteConfig(string content)
	{
		FileSystem.Directory.CreateDirectory(Root);
		await FileSystem.File.WriteAllTextAsync(ConfigPath, content);
	}

	private ChangelogEntryValidationService CreateService(IGitHubPrService prService, IEnvironmentVariables? env = null) =>
		new(LoggerFactory, ConfigurationContext, prService, RunnerTempFileSystem, env);

	private static ValidateEntriesArguments MakeArgs(string repo, string? configFile = null) =>
		new()
		{
			ConfigFile = configFile ?? Path.Join(Root, "changelog.yml"),
			Owner = "elastic",
			Repo = repo,
			PrNumber = 42,
			PrLabels = [],
			Files = []
		};

	[Fact]
	public async Task ValidateEntries_UnregisteredRepo_ReturnsErrorBeforeAnyGitHubCall()
	{
		await WriteConfig(MinimalConfig);
		var prService = A.Fake<IGitHubPrService>();

		var svc = CreateService(prService);
		var result = await svc.ValidateEntries(Collector, MakeArgs("unregistered-repo"), CancellationToken.None);

		result.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector
			.Diagnostics
			.Where(d => d.Severity == Severity.Error)
			.Should()
			.Contain(d => d.Message.Contains("unregistered-repo") && d.Message.Contains("products.yml"));
		A.CallTo(() => prService.FetchChangedFilesAsync(A<string>._, A<string>._, A<int>._, A<CancellationToken>._)).MustNotHaveHappened();
	}

	[Fact]
	public async Task ValidateEntries_AllProductsHaveReleaseNotesDisabled_ReturnsError()
	{
		await WriteConfig(MinimalConfig);
		var prService = A.Fake<IGitHubPrService>();

		var productsDict = new Dictionary<string, Product>
		{
			{
				"no-notes-product",
				new Product
				{
					Id = "no-notes-product",
					DisplayName = "No Notes Product",
					Repository = "silent-repo",
					Features = new ProductFeatures { ReleaseNotes = ReleaseNotesPath.None }
				}
			}
		};
		var silentConfig = new ConfigurationContext
		{
			Endpoints = ConfigurationContext.Endpoints,
			ConfigurationFileProvider = ConfigurationContext.ConfigurationFileProvider,
			VersionsConfiguration = ConfigurationContext.VersionsConfiguration,
			ProductsConfiguration = new ProductsConfiguration
			{
				Products = productsDict.ToFrozenDictionary(),
				PublicReferenceProducts = FrozenDictionary<string, Product>.Empty,
				ProductDisplayNames = productsDict.ToDictionary(p => p.Key, p => p.Value.DisplayName).ToFrozenDictionary()
			},
			SearchConfiguration = ConfigurationContext.SearchConfiguration,
			LegacyUrlMappings = ConfigurationContext.LegacyUrlMappings
		};

		var svc = new ChangelogEntryValidationService(LoggerFactory, silentConfig, prService, RunnerTempFileSystem);
		var result = await svc.ValidateEntries(Collector, MakeArgs("silent-repo"), CancellationToken.None);

		result.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector
			.Diagnostics
			.Where(d => d.Severity == Severity.Error)
			.Should()
			.Contain(d => d.Message.Contains("silent-repo") && d.Message.Contains("release notes disabled"));
		A.CallTo(() => prService.FetchChangedFilesAsync(A<string>._, A<string>._, A<int>._, A<CancellationToken>._)).MustNotHaveHappened();
	}

	[Fact]
	public async Task ValidateEntries_RegisteredRepoWithReleaseNotes_ProceedsToFileValidation()
	{
		await WriteConfig(MinimalConfig);
		var prService = A.Fake<IGitHubPrService>();

		var svc = CreateService(prService);
		var result = await svc.ValidateEntries(Collector, MakeArgs("elasticsearch"), CancellationToken.None);

		result.Should().BeTrue("a registered repo with an empty file list and no require-changelog-file should pass");
		Collector.Errors.Should().Be(0);
	}
}
