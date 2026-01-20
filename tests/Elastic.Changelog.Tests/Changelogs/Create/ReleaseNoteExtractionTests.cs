// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.GitHub;
using Elastic.Changelog;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.Creation;
using Elastic.Changelog.Configuration;
using Elastic.Changelog.Rendering;
using FakeItEasy;
using FluentAssertions;

namespace Elastic.Changelog.Tests.Changelogs.Create;

public class ReleaseNoteExtractionTests(ITestOutputHelper output) : CreateChangelogTestBase(output)
{
	[Fact]
	public async Task CreateChangelog_WithExtractReleaseNotes_ShortReleaseNote_UsesAsTitle()
	{
		// Arrange
		var prInfo = new GitHubPrInfo
		{
			Title = "Implement new aggregation API",
			Body = "## Summary\n\nThis PR adds a new feature.\n\nRelease Notes: Adds support for new aggregation types",
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
			ExtractReleaseNotes = true
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
		files.Should().HaveCount(1);

		var yamlContent = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("title: Adds support for new aggregation types");
		// Description should not be set when release note is used as title
		if (yamlContent.Contains("description:"))
		{
			// If description field exists, it should be empty or commented out
			var descriptionLine = yamlContent.Split('\n').FirstOrDefault(l => l.Contains("description:"));
			descriptionLine.Should().MatchRegex(@"description:\s*(#|$)");
		}
	}

	[Fact]
	public async Task CreateChangelog_WithExtractReleaseNotes_LongReleaseNote_UsesAsDescription()
	{
		// Arrange
		var longReleaseNote =
			"Adds support for new aggregation types including date histogram, range aggregations, and nested aggregations with improved performance";
		var prInfo = new GitHubPrInfo
		{
			Title = "Implement new aggregation API",
			Body = $"## Summary\n\nThis PR adds a new feature.\n\nRelease Notes: {longReleaseNote}",
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
			ExtractReleaseNotes = true
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
		files.Should().HaveCount(1);

		var yamlContent = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("title: Implement new aggregation API");
		yamlContent.Should().Contain($"description: {longReleaseNote}");
	}

	[Fact]
	public async Task CreateChangelog_WithExtractReleaseNotes_MultiLineReleaseNote_UsesAsDescription()
	{
		// Arrange
		// The regex stops at double newline, so we need a release note that spans multiple lines without double newline
		var multiLineReleaseNote = "Adds support for new aggregation types\nThis includes date histogram and range aggregations\nwith improved performance";
		var prInfo = new GitHubPrInfo
		{
			Title = "Implement new aggregation API",
			Body = $"## Summary\n\nThis PR adds a new feature.\n\nRelease Notes: {multiLineReleaseNote}",
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
			ExtractReleaseNotes = true
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
		files.Should().HaveCount(1);

		var yamlContent = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("title: Implement new aggregation API");
		yamlContent.Should().Contain("description:");
		yamlContent.Should().Contain("Adds support for new aggregation types");
		yamlContent.Should().Contain("date histogram");
	}

	[Fact]
	public async Task CreateChangelog_WithExtractReleaseNotes_NoReleaseNote_UsesPrTitle()
	{
		// Arrange
		var prInfo = new GitHubPrInfo
		{
			Title = "Implement new aggregation API",
			Body = "## Summary\n\nThis PR adds a new feature but has no release notes section.",
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
			ExtractReleaseNotes = true
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
		files.Should().HaveCount(1);

		var yamlContent = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("title: Implement new aggregation API");
		// Description should not be set when no release note is found
		if (yamlContent.Contains("description:"))
		{
			// If description field exists, it should be empty or commented out
			var descriptionLine = yamlContent.Split('\n').FirstOrDefault(l => l.Contains("description:"));
			descriptionLine.Should().MatchRegex(@"description:\s*(#|$)");
		}
	}

	[Fact]
	public async Task CreateChangelog_WithExtractReleaseNotes_ExplicitTitle_TakesPrecedence()
	{
		// Arrange
		var prInfo = new GitHubPrInfo
		{
			Title = "Implement new aggregation API",
			Body = "Release Notes: Adds support for new aggregation types",
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
			ExtractReleaseNotes = true,
			Title = "Custom title"
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
		files.Should().HaveCount(1);

		var yamlContent = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("title: Custom title");
		yamlContent.Should().NotContain("Adds support for new aggregation types");
	}

	[Fact]
	public async Task CreateChangelog_WithExtractReleaseNotes_ExplicitDescription_TakesPrecedence()
	{
		// Arrange
		var longReleaseNote =
			"Adds support for new aggregation types including date histogram, range aggregations, and nested aggregations with improved performance";
		var prInfo = new GitHubPrInfo
		{
			Title = "Implement new aggregation API",
			Body = $"Release Notes: {longReleaseNote}",
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
			ExtractReleaseNotes = true,
			Description = "Custom description"
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
		files.Should().HaveCount(1);

		var yamlContent = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("description: Custom description");
		yamlContent.Should().NotContain(longReleaseNote);
	}
}
