// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Creation;
using Elastic.Changelog.GitHub;
using FakeItEasy;
using FluentAssertions;

namespace Elastic.Changelog.Tests.Changelogs.Create;

public class PrIntegrationTests(ITestOutputHelper output) : CreateChangelogTestBase(output)
{
	[Fact]
	public async Task CreateChangelog_WithPrOption_FetchesPrInfoAndDerivesTitle()
	{
		// Arrange
		var prInfo = new GitHubPrInfo
		{
			Title = "Implement new aggregation API",
			Labels = ["type:feature"]
		};

		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(
				"https://github.com/elastic/elasticsearch/pull/12345",
				null,
				null,
				A<CancellationToken>._))
			.Returns(prInfo);

		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature: "type:feature"
			    bug-fix:
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			""";
		var configPath = await CreateConfigDirectory(configContent);

		var service = CreateService();

		var input = new ChangelogInput
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Config = configPath,
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(
				"https://github.com/elastic/elasticsearch/pull/12345",
				null,
				null,
				A<CancellationToken>._))
			.MustHaveHappenedOnceExactly();

		// Note: ChangelogService uses real FileSystem, so we need to check the actual file system
		var outputDir = input.Output ?? FileSystem.Directory.GetCurrentDirectory();
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(1);

		var yamlContent = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("title: Implement new aggregation API");
		yamlContent.Should().Contain("type: feature");
		yamlContent.Should().Contain("pr: https://github.com/elastic/elasticsearch/pull/12345");
	}

	[Fact]
	public async Task CreateChangelog_WithUsePrNumber_CreatesFileWithPrNumberAsFilename()
	{
		// Arrange
		var prInfo = new GitHubPrInfo
		{
			Title = "Fix memory leak in search",
			Labels = ["type:bug"]
		};

		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(
				"https://github.com/elastic/elasticsearch/pull/140034",
				null,
				null,
				A<CancellationToken>._))
			.Returns(prInfo);

		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix: "type:bug"
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			""";
		var configPath = await CreateConfigDirectory(configContent);

		var service = CreateService();

		var input = new ChangelogInput
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/140034"],
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Config = configPath,
			Output = CreateOutputDirectory(),
			UsePrNumber = true
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		// Note: ChangelogService uses real FileSystem, so we need to check the actual file system
		var outputDir = input.Output ?? FileSystem.Directory.GetCurrentDirectory();
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(1);

		// Verify the filename is the PR number, not a timestamp-based name
		var fileName = Path.GetFileName(files[0]);
		fileName.Should().Be("140034.yaml", "the filename should be the PR number when UsePrNumber is true");

		var yamlContent = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("type: bug-fix");
		yamlContent.Should().Contain("pr: https://github.com/elastic/elasticsearch/pull/140034");
	}

	[Fact]
	public async Task CreateChangelog_WithPrNumberAndOwnerRepo_FetchesPrInfo()
	{
		// Arrange
		var prInfo = new GitHubPrInfo
		{
			Title = "Update documentation",
			Labels = []
		};

		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(
				"12345",
				"elastic",
				"elasticsearch",
				A<CancellationToken>._))
			.Returns(prInfo);

		var service = CreateService();

		var input = new ChangelogInput
		{
			Prs = ["12345"],
			Owner = "elastic",
			Repo = "elasticsearch",
			Title = "Update documentation",
			Type = "docs",
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0" }],
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(
				"12345",
				"elastic",
				"elasticsearch",
				A<CancellationToken>._))
			.MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task CreateChangelog_WithMultiplePrs_CreatesOneFilePerPr()
	{
		// Arrange
		var pr1Info = new GitHubPrInfo
		{
			Title = "First PR feature",
			Labels = ["type:feature"]
		};
		var pr2Info = new GitHubPrInfo
		{
			Title = "Second PR bug fix",
			Labels = ["type:bug"]
		};

		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(
				A<string>.That.Contains("1234"),
				null,
				null,
				A<CancellationToken>._))
			.Returns(pr1Info);

		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(
				A<string>.That.Contains("5678"),
				null,
				null,
				A<CancellationToken>._))
			.Returns(pr2Info);

		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature: "type:feature"
			    bug-fix: "type:bug"
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			""";
		var configPath = await CreateConfigDirectory(configContent);

		var service = CreateService();

		var input = new ChangelogInput
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/1234", "https://github.com/elastic/elasticsearch/pull/5678"],
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Config = configPath,
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var outputDir = input.Output ?? FileSystem.Directory.GetCurrentDirectory();
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(2);

		var yamlContents = new List<string>();
		foreach (var file in files)
			yamlContents.Add(await FileSystem.File.ReadAllTextAsync(file, TestContext.Current.CancellationToken));

		// Verify both PRs were processed
		yamlContents.Should().Contain(c => c.Contains("title: First PR feature"));
		yamlContents.Should().Contain(c => c.Contains("title: Second PR bug fix"));
	}

	[Fact]
	public async Task CreateChangelog_WithPrsFromFile_ProcessesAllPrsFromFile()
	{
		// Arrange - Simulate what ChangelogCommand does: read PRs from a file
		var pr1Info = new GitHubPrInfo
		{
			Title = "First PR from file",
			Labels = ["type:feature"]
		};
		var pr2Info = new GitHubPrInfo
		{
			Title = "Second PR from file",
			Labels = ["type:bug"]
		};
		var pr3Info = new GitHubPrInfo
		{
			Title = "Third PR from file",
			Labels = ["type:enhancement"]
		};

		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(
				A<string>.That.Contains("1111"),
				null,
				null,
				A<CancellationToken>._))
			.Returns(pr1Info);

		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(
				A<string>.That.Contains("2222"),
				null,
				null,
				A<CancellationToken>._))
			.Returns(pr2Info);

		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(
				A<string>.That.Contains("3333"),
				null,
				null,
				A<CancellationToken>._))
			.Returns(pr3Info);

		var tempDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(tempDir);

		// Create a file with newline-delimited PRs (simulating what ChangelogCommand would read)
		var prsFile = FileSystem.Path.Combine(tempDir, "prs.txt");
		var prsFileContent =
			"""
			https://github.com/elastic/elasticsearch/pull/1111
			https://github.com/elastic/elasticsearch/pull/2222
			https://github.com/elastic/elasticsearch/pull/3333
			""";
		await FileSystem.File.WriteAllTextAsync(prsFile, prsFileContent, TestContext.Current.CancellationToken);

		// Read PRs from file (simulating ChangelogCommand behavior)
		var prsFromFile = await FileSystem.File.ReadAllLinesAsync(prsFile, TestContext.Current.CancellationToken);
		var parsedPrs = prsFromFile
			.Where(line => !string.IsNullOrWhiteSpace(line))
			.Select(line => line.Trim())
			.ToArray();

		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature: "type:feature"
			    bug-fix: "type:bug"
			    enhancement: "type:enhancement"
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			""";
		var configPath = await CreateConfigDirectory(configContent);

		var service = CreateService();

		var input = new ChangelogInput
		{
			Prs = parsedPrs, // PRs read from file
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Config = configPath,
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var outputDir = input.Output ?? FileSystem.Directory.GetCurrentDirectory();
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(3); // One file per PR

		var yamlContents = new List<string>();
		foreach (var file in files)
			yamlContents.Add(await FileSystem.File.ReadAllTextAsync(file, TestContext.Current.CancellationToken));

		// Verify all PRs were processed
		yamlContents.Should().Contain(c => c.Contains("title: First PR from file"));
		yamlContents.Should().Contain(c => c.Contains("title: Second PR from file"));
		yamlContents.Should().Contain(c => c.Contains("title: Third PR from file"));
	}

	[Fact]
	public async Task CreateChangelog_WithMixedPrsFromFileAndCommaSeparated_ProcessesAllPrs()
	{
		// Arrange - Simulate ChangelogCommand handling both file paths and comma-separated PRs
		var pr1Info = new GitHubPrInfo
		{
			Title = "PR from comma-separated",
			Labels = ["type:feature"]
		};
		var pr2Info = new GitHubPrInfo
		{
			Title = "PR from file",
			Labels = ["type:bug"]
		};

		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(
				A<string>.That.Contains("1111"),
				null,
				null,
				A<CancellationToken>._))
			.Returns(pr1Info);

		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(
				A<string>.That.Contains("2222"),
				null,
				null,
				A<CancellationToken>._))
			.Returns(pr2Info);

		var tempDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(tempDir);

		// Create a file with PRs
		var prsFile = FileSystem.Path.Combine(tempDir, "prs.txt");
		var prsFileContent =
			"""
			https://github.com/elastic/elasticsearch/pull/2222
			""";
		await FileSystem.File.WriteAllTextAsync(prsFile, prsFileContent, TestContext.Current.CancellationToken);

		// Simulate ChangelogCommand processing: comma-separated PRs + file path
		var allPrs = new List<string>();

		// Add comma-separated PRs
		var commaSeparatedPrs =
			"https://github.com/elastic/elasticsearch/pull/1111".Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		allPrs.AddRange(commaSeparatedPrs);

		// Add PRs from file
		var prsFromFile = await FileSystem.File.ReadAllLinesAsync(prsFile, TestContext.Current.CancellationToken);
		allPrs.AddRange(
			prsFromFile
				.Where(line => !string.IsNullOrWhiteSpace(line))
				.Select(line => line.Trim())
		);

		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature: "type:feature"
			    bug-fix: "type:bug"
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			""";
		var configPath = await CreateConfigDirectory(configContent);

		var service = CreateService();

		var input = new ChangelogInput
		{
			Prs = allPrs.ToArray(), // Mixed PRs from comma-separated and file
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Config = configPath,
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var outputDir = input.Output ?? FileSystem.Directory.GetCurrentDirectory();
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(2); // One file per PR

		var yamlContents = new List<string>();
		foreach (var file in files)
		{
			var content = await FileSystem.File.ReadAllTextAsync(file, TestContext.Current.CancellationToken);
			yamlContents.Add(content);
		}

		// Verify both PRs were processed
		yamlContents.Should().Contain(c => c.Contains("title: PR from comma-separated"));
		yamlContents.Should().Contain(c => c.Contains("title: PR from file"));
		yamlContents.Should().Contain(c => c.Contains("pr: https://github.com/elastic/elasticsearch/pull/1111"));
		yamlContents.Should().Contain(c => c.Contains("pr: https://github.com/elastic/elasticsearch/pull/2222"));
	}
}
