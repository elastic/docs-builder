// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Services.Changelog;
using FakeItEasy;
using FluentAssertions;

namespace Elastic.Documentation.Services.Tests.Changelogs.Create;

public class TitleProcessingTests(ITestOutputHelper output) : CreateChangelogTestBase(output)
{
	[Fact]
	public async Task CreateChangelog_WithStripTitlePrefix_RemovesSquareBracketsAndColon()
	{
		// Arrange
		var prInfo = new GitHubPrInfo
		{
			Title = "[ES|QL]: Update Vector Similarity To Support BFLOAT16",
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
			available_types:
			  - feature
			  - bug-fix
			available_subtypes: []
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			label_to_type:
			  "type:feature": feature
			""";
		var configPath = await CreateConfigDirectory(configContent);

		var service = CreateService();

		var input = new ChangelogInput
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Config = configPath,
			Output = CreateOutputDirectory(),
			StripTitlePrefix = true
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var outputDir = input.Output ?? _fileSystem.Directory.GetCurrentDirectory();
		if (!_fileSystem.Directory.Exists(outputDir))
			_fileSystem.Directory.CreateDirectory(outputDir);
		var files = _fileSystem.Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(1);

		var yamlContent = await _fileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("title: Update Vector Similarity To Support BFLOAT16");
		yamlContent.Should().NotContain("[ES|QL]");
		yamlContent.Should().NotContain("[ES|QL]:");
	}

	[Fact]
	public async Task CreateChangelog_WithStripTitlePrefix_RemovesSquareBracketsWithoutColon()
	{
		// Arrange
		var prInfo = new GitHubPrInfo
		{
			Title = "[Security] Improve authentication handling",
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
			available_types:
			  - feature
			  - bug-fix
			available_subtypes: []
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			label_to_type:
			  "type:feature": feature
			""";
		var configPath = await CreateConfigDirectory(configContent);

		var service = CreateService();

		var input = new ChangelogInput
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Config = configPath,
			Output = CreateOutputDirectory(),
			StripTitlePrefix = true
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var outputDir = input.Output ?? _fileSystem.Directory.GetCurrentDirectory();
		if (!_fileSystem.Directory.Exists(outputDir))
			_fileSystem.Directory.CreateDirectory(outputDir);
		var files = _fileSystem.Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(1);

		var yamlContent = await _fileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("title: Improve authentication handling");
		yamlContent.Should().NotContain("[Security]");
	}

	[Fact]
	public async Task CreateChangelog_WithExplicitTitle_OverridesPrTitle()
	{
		// Arrange
		var prInfo = new GitHubPrInfo
		{
			Title = "PR Title from GitHub",
			Labels = []
		};

		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(
				A<string>._,
				A<string?>._,
				A<string?>._,
				A<CancellationToken>._))
			.Returns(prInfo);

		var service = CreateService();

		var input = new ChangelogInput
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Title = "Custom Title Override",
			Type = "feature",
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0" }],
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		if (!result)
		{
			foreach (var diagnostic in _collector.Diagnostics)
				_output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
		}

		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		// Note: ChangelogService uses real FileSystem, so we need to check the actual file system
		var outputDir = input.Output ?? _fileSystem.Directory.GetCurrentDirectory();
		if (!_fileSystem.Directory.Exists(outputDir))
			_fileSystem.Directory.CreateDirectory(outputDir);
		var files = _fileSystem.Directory.GetFiles(outputDir, "*.yaml");
		var yamlContent = await _fileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("title: Custom Title Override");
		yamlContent.Should().NotContain("PR Title from GitHub");
	}

	[Fact]
	public async Task CreateChangelog_WithIssues_CreatesValidYaml()
	{
		// Arrange
		var service = CreateService();

		var input = new ChangelogInput
		{
			Title = "Fix multiple issues",
			Type = "bug-fix",
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0" }],
			Issues =
			[
				"https://github.com/elastic/elasticsearch/issues/123",
				"https://github.com/elastic/elasticsearch/issues/456"
			],
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		if (!result)
		{
			foreach (var diagnostic in _collector.Diagnostics)
				_output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
		}

		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		// Note: ChangelogService uses real FileSystem, so we need to check the actual file system
		var outputDir = input.Output ?? _fileSystem.Directory.GetCurrentDirectory();
		if (!_fileSystem.Directory.Exists(outputDir))
			_fileSystem.Directory.CreateDirectory(outputDir);
		var files = _fileSystem.Directory.GetFiles(outputDir, "*.yaml");
		var yamlContent = await _fileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("issues:");
		yamlContent.Should().Contain("- https://github.com/elastic/elasticsearch/issues/123");
		yamlContent.Should().Contain("- https://github.com/elastic/elasticsearch/issues/456");
	}
}
