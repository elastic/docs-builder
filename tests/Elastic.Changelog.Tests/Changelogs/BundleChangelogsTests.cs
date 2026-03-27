// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Tests.Changelogs;

public class BundleChangelogsTests : ChangelogTestBase
{
	private ChangelogBundlingService Service { get; }
	private ChangelogBundlingService ServiceWithConfig { get; }
	private readonly string _changelogDir;

	public BundleChangelogsTests(ITestOutputHelper output) : base(output)
	{
		Service = new(LoggerFactory, null, FileSystem);
		ServiceWithConfig = new(LoggerFactory, ConfigurationContext, FileSystem);
		_changelogDir = CreateChangelogDir();
	}

	private string CreateChangelogDir()
	{
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);
		return changelogDir;
	}

	[Fact]
	public async Task BundleChangelogs_WithAllOption_CreatesValidBundle()
	{
		// language=yaml
		var changelog1 =
			"""
			title: First changelog
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
			title: Second changelog
			type: enhancement
			products:
			  - product: kibana
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/kibana/pull/200
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-first-changelog.yaml");
		var file2 = FileSystem.Path.Combine(_changelogDir, "1755268140-second-changelog.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			All = true,
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("products:");
		bundleContent.Should().Contain("product: elasticsearch");
		bundleContent.Should().Contain("product: kibana");
		bundleContent.Should().Contain("entries:");
		bundleContent.Should().Contain("file:");
		bundleContent.Should().Contain("name: 1755268130-first-changelog.yaml");
		bundleContent.Should().Contain("name: 1755268140-second-changelog.yaml");
		bundleContent.Should().Contain("checksum:");
	}

	[Fact]
	public async Task BundleChangelogs_WithProductsFilter_FiltersCorrectly()
	{
		// Arrange

		// language=yaml
		var changelog1 =
			"""
			title: Elasticsearch feature
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
			title: Kibana feature
			type: feature
			products:
			  - product: kibana
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/kibana/pull/200
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-elasticsearch-feature.yaml");
		var file2 = FileSystem.Path.Combine(_changelogDir, "1755268140-kibana-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			InputProducts = [new ProductArgument { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("product: elasticsearch");
		bundleContent.Should().Contain("target: 9.2.0");
		bundleContent.Should().Contain("name: 1755268130-elasticsearch-feature.yaml");
		bundleContent.Should().NotContain("name: 1755268140-kibana-feature.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithPrsFilter_FiltersCorrectly()
	{
		// Arrange

		// language=yaml
		var changelog1 =
			"""
			title: First PR
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";
		// language=yaml
		var changelog2 =
			"""
			title: Second PR
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/200
			""";
		// language=yaml
		var changelog3 =
			"""
			title: Third PR
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/300
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-first-pr.yaml");
		var file2 = FileSystem.Path.Combine(_changelogDir, "1755268140-second-pr.yaml");
		var file3 = FileSystem.Path.Combine(_changelogDir, "1755268150-third-pr.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file3, changelog3, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Prs = ["https://github.com/elastic/elasticsearch/pull/100", "https://github.com/elastic/elasticsearch/pull/200"],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("name: 1755268130-first-pr.yaml");
		bundleContent.Should().Contain("name: 1755268140-second-pr.yaml");
		bundleContent.Should().NotContain("name: 1755268150-third-pr.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithIssuesFilter_FiltersCorrectly()
	{
		// Arrange
		// language=yaml
		var changelog1 =
			"""
			title: First issue
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			issues:
			  - https://github.com/elastic/elasticsearch/issues/100
			""";
		// language=yaml
		var changelog2 =
			"""
			title: Second issue
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			issues:
			  - https://github.com/elastic/elasticsearch/issues/200
			""";
		// language=yaml
		var changelog3 =
			"""
			title: Third issue
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			issues:
			  - https://github.com/elastic/elasticsearch/issues/300
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-first-issue.yaml");
		var file2 = FileSystem.Path.Combine(_changelogDir, "1755268140-second-issue.yaml");
		var file3 = FileSystem.Path.Combine(_changelogDir, "1755268150-third-issue.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file3, changelog3, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Issues = ["https://github.com/elastic/elasticsearch/issues/100", "https://github.com/elastic/elasticsearch/issues/200"],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("name: 1755268130-first-issue.yaml");
		bundleContent.Should().Contain("name: 1755268140-second-issue.yaml");
		bundleContent.Should().NotContain("name: 1755268150-third-issue.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithOldPrFormat_StillMatchesWhenFilteringByPrs()
	{
		// Backward compat: changelog with legacy pr: (single string) should still match --prs filter
		// language=yaml
		var changelogWithOldFormat =
			"""
			title: Legacy PR changelog
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/999
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-legacy-pr.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelogWithOldFormat, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Prs = ["https://github.com/elastic/elasticsearch/pull/999"],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("name: 1755268130-legacy-pr.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithPrsFilterAndUnmatchedPrs_EmitsWarnings()
	{
		// Arrange

		// language=yaml
		var changelog1 =
			"""
			title: First PR
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-first-pr.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Prs =
			[
				"https://github.com/elastic/elasticsearch/pull/100",
				"https://github.com/elastic/elasticsearch/pull/200",
				"https://github.com/elastic/elasticsearch/pull/300"
			],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector.Warnings.Should().Be(2); // Two unmatched PRs
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("No changelog file found for PR: https://github.com/elastic/elasticsearch/pull/200"));
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("No changelog file found for PR: https://github.com/elastic/elasticsearch/pull/300"));
	}

	[Fact]
	public async Task BundleChangelogs_WithPrsFileFilter_FiltersCorrectly()
	{
		// Arrange

		// language=yaml
		var changelog1 =
			"""
			title: First PR
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";
		// language=yaml
		var changelog2 =
			"""
			title: Second PR
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/200
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-first-pr.yaml");
		var file2 = FileSystem.Path.Combine(_changelogDir, "1755268140-second-pr.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		// Create PRs file
		var prsFile = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "prs.txt");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(prsFile)!);
		// language=yaml
		var prsContent =
			"""
			https://github.com/elastic/elasticsearch/pull/100
			https://github.com/elastic/elasticsearch/pull/200
			""";
		await FileSystem.File.WriteAllTextAsync(prsFile, prsContent, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Prs = [prsFile],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("name: 1755268130-first-pr.yaml");
		bundleContent.Should().Contain("name: 1755268140-second-pr.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithPrNumberAndOwnerRepo_FiltersCorrectly()
	{
		// Arrange

		// language=yaml
		var changelog1 =
			"""
			title: PR with number
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-pr-number.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Prs = ["100"],
			Owner = "elastic",
			Repo = "elasticsearch",
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("name: 1755268130-pr-number.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithShortPrFormat_FiltersCorrectly()
	{
		// Arrange

		// language=yaml
		var changelog1 =
			"""
			title: PR with short format
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/133609
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-short-format.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Prs = ["elastic/elasticsearch#133609"],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("name: 1755268130-short-format.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithNoMatchingFiles_ReturnsError()
	{
		// Arrange

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			InputProducts = [new ProductArgument { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("No YAML files found") || d.Message.Contains("No changelog entries matched"));
	}

	[Fact]
	public async Task BundleChangelogs_WithInvalidDirectory_ReturnsError()
	{
		// Arrange
		var invalidDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent");

		var input = new BundleChangelogsArguments
		{
			Directory = invalidDir,
			All = true,
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("Directory does not exist"));
	}

	[Fact]
	public async Task BundleChangelogs_WithNoFilterOption_ReturnsError()
	{
		// Arrange

		// language=yaml
		var changelog1 =
			"""
			title: First changelog
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
			title: Second changelog
			type: enhancement
			products:
			  - product: kibana
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/kibana/pull/200
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-first-changelog.yaml");
		var file2 = FileSystem.Path.Combine(_changelogDir, "1755268140-second-changelog.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("At least one filter option must be specified"));
	}

	[Fact]
	public async Task BundleChangelogs_WithMultipleFilterOptions_ReturnsError()
	{
		// Arrange

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			All = true,
			InputProducts = [new ProductArgument { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("Multiple filter options cannot be specified together"));
	}

	[Fact]
	public async Task BundleChangelogs_WithMultipleProducts_CreatesValidBundle()
	{
		// Arrange

		// language=yaml
		var changelog1 =
			"""
			title: Cloud serverless feature 1
			type: feature
			products:
			  - product: cloud-serverless
			    target: 2025-12-02
			prs:
			  - https://github.com/elastic/cloud-serverless/pull/100
			""";
		// language=yaml
		var changelog2 =
			"""
			title: Cloud serverless feature 2
			type: feature
			products:
			  - product: cloud-serverless
			    target: 2025-12-06
			prs:
			  - https://github.com/elastic/cloud-serverless/pull/200
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-cloud-feature1.yaml");
		var file2 = FileSystem.Path.Combine(_changelogDir, "1755268140-cloud-feature2.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			InputProducts =
			[
				new ProductArgument { Product = "cloud-serverless", Target = "2025-12-02", Lifecycle = "*" },
				new ProductArgument { Product = "cloud-serverless", Target = "2025-12-06", Lifecycle = "*" }
			],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("product: cloud-serverless");
		bundleContent.Should().Contain("target: 2025-12-02");
		bundleContent.Should().Contain("target: 2025-12-06");
		bundleContent.Should().Contain("name: 1755268130-cloud-feature1.yaml");
		bundleContent.Should().Contain("name: 1755268140-cloud-feature2.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithWildcardProductFilter_MatchesAllProducts()
	{
		// Arrange

		// language=yaml
		var changelog1 =
			"""
			title: Elasticsearch feature
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
			title: Kibana feature
			type: feature
			products:
			  - product: kibana
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/kibana/pull/200
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-elasticsearch-feature.yaml");
		var file2 = FileSystem.Path.Combine(_changelogDir, "1755268140-kibana-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			InputProducts = [new ProductArgument { Product = "*", Target = "9.2.0", Lifecycle = "ga" }],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("name: 1755268130-elasticsearch-feature.yaml");
		bundleContent.Should().Contain("name: 1755268140-kibana-feature.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithWildcardAllParts_EquivalentToAll()
	{
		// Arrange

		// language=yaml
		var changelog1 =
			"""
			title: Elasticsearch feature
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
			title: Kibana feature
			type: feature
			products:
			  - product: kibana
			    target: 9.3.0
			    lifecycle: beta
			prs:
			  - https://github.com/elastic/kibana/pull/200
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-elasticsearch-feature.yaml");
		var file2 = FileSystem.Path.Combine(_changelogDir, "1755268140-kibana-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			InputProducts = [new ProductArgument { Product = "*", Target = "*", Lifecycle = "*" }],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("name: 1755268130-elasticsearch-feature.yaml");
		bundleContent.Should().Contain("name: 1755268140-kibana-feature.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithPrefixWildcardTarget_MatchesCorrectly()
	{
		// Arrange

		// language=yaml
		var changelog1 =
			"""
			title: Elasticsearch 9.3.0 feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.3.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";
		// language=yaml
		var changelog2 =
			"""
			title: Elasticsearch 9.3.1 feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.3.1
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/200
			""";
		// language=yaml
		var changelog3 =
			"""
			title: Elasticsearch 9.2.0 feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/300
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-es-9.3.0.yaml");
		var file2 = FileSystem.Path.Combine(_changelogDir, "1755268140-es-9.3.1.yaml");
		var file3 = FileSystem.Path.Combine(_changelogDir, "1755268150-es-9.2.0.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file3, changelog3, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			InputProducts = [new ProductArgument { Product = "elasticsearch", Target = "9.3.*", Lifecycle = "*" }],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("name: 1755268130-es-9.3.0.yaml");
		bundleContent.Should().Contain("name: 1755268140-es-9.3.1.yaml");
		bundleContent.Should().NotContain("name: 1755268150-es-9.2.0.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithNonExistentFileAsPrs_ReturnsError()
	{
		// Arrange

		// Provide a non-existent file path - should return error since there are no other PRs
		var nonexistentFile = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent.txt");
		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Prs = [nonexistentFile],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		// File doesn't exist and there are no other PRs, so should return error
		result.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("File does not exist"));
	}

	[Fact]
	public async Task BundleChangelogs_WithUrlAsPrs_TreatsAsPrIdentifier()
	{
		// Arrange

		// language=yaml
		var changelog =
			"""
			title: Test PR
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/123
			""";
		var changelogFile = FileSystem.Path.Combine(_changelogDir, "1755268130-test-pr.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog, TestContext.Current.CancellationToken);

		// Provide a URL - should be treated as a PR identifier, not a file path
		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Prs = ["https://github.com/elastic/elasticsearch/pull/123"],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		// URL should be treated as PR identifier and match the changelog
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector.Warnings.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("name: 1755268130-test-pr.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithNonExistentFileAndOtherPrs_EmitsWarning()
	{
		// Arrange

		// language=yaml
		var changelog =
			"""
			title: Test PR
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/123
			""";
		var changelogFile = FileSystem.Path.Combine(_changelogDir, "1755268130-test-pr.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog, TestContext.Current.CancellationToken);

		// Provide a non-existent file path along with a valid PR - should emit warning for file but continue with PR
		var nonexistentFile = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent.txt");
		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Prs = [nonexistentFile, "https://github.com/elastic/elasticsearch/pull/123"],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		// Should succeed because we have a valid PR, but should emit warning for the non-existent file
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector.Warnings.Should().BeGreaterThan(0);
		// Check that we have a warning about the file not existing
		var fileWarning = Collector.Diagnostics.FirstOrDefault(d => d.Message.Contains("File does not exist, skipping"));
		fileWarning.Should().NotBeNull("Expected a warning about the non-existent file being skipped");

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("name: 1755268130-test-pr.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithOutputProducts_OverridesChangelogProducts()
	{
		// Arrange

		// language=yaml
		var changelog1 =
			"""
			title: Elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";
		// language=yaml
		var changelog2 =
			"""
			title: Kibana feature
			type: feature
			products:
			  - product: kibana
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/kibana/pull/200
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-elasticsearch-feature.yaml");
		var file2 = FileSystem.Path.Combine(_changelogDir, "1755268140-kibana-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			All = true,
			OutputProducts =
			[
				new ProductArgument { Product = "cloud-serverless", Target = "2025-12-02", Lifecycle = "ga" },
				new ProductArgument { Product = "cloud-serverless", Target = "2025-12-06", Lifecycle = "beta" }
			],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		// Output products should override changelog products
		bundleContent.Should().Contain("product: cloud-serverless");
		bundleContent.Should().Contain("target: 2025-12-02");
		bundleContent.Should().Contain("target: 2025-12-06");
		// Lifecycle values should be included in products array
		bundleContent.Should().Contain("lifecycle: ga");
		bundleContent.Should().Contain("lifecycle: beta");
		// Should not contain products from changelogs
		bundleContent.Should().NotContain("product: elasticsearch");
		bundleContent.Should().NotContain("product: kibana");
		// But should still contain the entries
		bundleContent.Should().Contain("name: 1755268130-elasticsearch-feature.yaml");
		bundleContent.Should().Contain("name: 1755268140-kibana-feature.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithMultipleProducts_IncludesAllProducts()
	{
		// Arrange

		// language=yaml
		var changelog1 =
			"""
			title: Elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";
		// language=yaml
		var changelog2 =
			"""
			title: Kibana feature
			type: feature
			products:
			  - product: kibana
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/kibana/pull/200
			""";
		// language=yaml
		var changelog3 =
			"""
			title: Multi-product feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			  - product: kibana
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/300
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-elasticsearch.yaml");
		var file2 = FileSystem.Path.Combine(_changelogDir, "1755268140-kibana.yaml");
		var file3 = FileSystem.Path.Combine(_changelogDir, "1755268150-multi-product.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file3, changelog3, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			All = true,
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("product: elasticsearch");
		bundleContent.Should().Contain("product: kibana");
		bundleContent.Should().Contain("target: 9.2.0");
		// Should have 3 entries
		var entryCount = bundleContent.Split("file:", StringSplitOptions.RemoveEmptyEntries).Length - 1;
		entryCount.Should().Be(3);
	}

	[Fact]
	public async Task BundleChangelogs_WithInputProducts_IncludesLifecycleInProductsArray()
	{
		// Arrange

		// language=yaml
		var changelog1 =
			"""
			title: Elasticsearch GA feature
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
			title: Elasticsearch Beta feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.3.0
			    lifecycle: beta
			prs:
			  - https://github.com/elastic/elasticsearch/pull/200
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-elasticsearch-ga.yaml");
		var file2 = FileSystem.Path.Combine(_changelogDir, "1755268140-elasticsearch-beta.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			InputProducts =
			[
				new ProductArgument { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" },
				new ProductArgument { Product = "elasticsearch", Target = "9.3.0", Lifecycle = "beta" }
			],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		// Verify lifecycle is included in products array (extracted from changelog entries, not filter)
		bundleContent.Should().Contain("product: elasticsearch");
		bundleContent.Should().Contain("target: 9.2.0");
		bundleContent.Should().Contain("target: 9.3.0");
		bundleContent.Should().Contain("lifecycle: ga");
		bundleContent.Should().Contain("lifecycle: beta");
	}

	[Fact]
	public async Task BundleChangelogs_WithOutputProducts_IncludesLifecycleInProductsArray()
	{
		// Arrange

		// language=yaml
		var changelog1 =
			"""
			title: Elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-elasticsearch.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			All = true,
			OutputProducts =
			[
				new ProductArgument { Product = "cloud-serverless", Target = "2025-12-02", Lifecycle = "ga" },
				new ProductArgument { Product = "cloud-serverless", Target = "2025-12-06", Lifecycle = "beta" }
			],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		// Verify lifecycle is included in products array from --output-products
		bundleContent.Should().Contain("product: cloud-serverless");
		bundleContent.Should().Contain("target: 2025-12-02");
		bundleContent.Should().Contain("target: 2025-12-06");
		bundleContent.Should().Contain("lifecycle: ga");
		bundleContent.Should().Contain("lifecycle: beta");
	}

	[Fact]
	public async Task BundleChangelogs_ExtractsLifecycleFromChangelogEntries()
	{
		// Arrange

		// language=yaml
		var changelog1 =
			"""
			title: Elasticsearch GA feature
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
			title: Elasticsearch Beta feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.3.0
			    lifecycle: beta
			prs:
			  - https://github.com/elastic/elasticsearch/pull/200
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-elasticsearch-ga.yaml");
		var file2 = FileSystem.Path.Combine(_changelogDir, "1755268140-elasticsearch-beta.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			All = true,
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		// Verify lifecycle is included in products array extracted from changelog entries
		bundleContent.Should().Contain("product: elasticsearch");
		bundleContent.Should().Contain("target: 9.2.0");
		bundleContent.Should().Contain("target: 9.3.0");
		bundleContent.Should().Contain("lifecycle: ga");
		bundleContent.Should().Contain("lifecycle: beta");
	}

	[Fact]
	public async Task BundleChangelogs_WithInputProductsWildcardLifecycle_ExtractsActualLifecycleFromChangelogs()
	{
		// Arrange - Test the scenario where --input-products uses "*" for lifecycle,
		// but the actual lifecycle value should be extracted from the changelog entries

		// language=yaml
		var changelog1 =
			"""
			title: A new feature was added
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			InputProducts =
			[
				new ProductArgument { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "*" }
			],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		// Verify that the actual lifecycle value "ga" from the changelog is included in products array,
		// not the wildcard "*" from the filter
		bundleContent.Should().Contain("product: elasticsearch");
		bundleContent.Should().Contain("target: 9.2.0");
		bundleContent.Should().Contain("lifecycle: ga");
		// Verify wildcard "*" is not included in the products array
		bundleContent.Should().NotContain("lifecycle: *");
		bundleContent.Should().NotContain("lifecycle: '*\"");
	}

	[Fact]
	public async Task BundleChangelogs_WithMultipleTargets_WarningIncludesLifecycle()
	{
		// Arrange - Test that warning message includes lifecycle when multiple products
		// have the same target but different lifecycles

		// language=yaml
		var changelog1 =
			"""
			title: Elasticsearch GA feature
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
			title: Elasticsearch Beta feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: beta
			prs:
			  - https://github.com/elastic/elasticsearch/pull/200
			""";
		// language=yaml
		var changelog3 =
			"""
			title: Elasticsearch feature without lifecycle
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/300
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-elasticsearch-ga.yaml");
		var file2 = FileSystem.Path.Combine(_changelogDir, "1755268140-elasticsearch-beta.yaml");
		var file3 = FileSystem.Path.Combine(_changelogDir, "1755268150-elasticsearch-no-lifecycle.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file3, changelog3, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			All = true,
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector.Warnings.Should().BeGreaterThan(0);
		// Verify warning message includes lifecycle values
		Collector.Diagnostics.Should().Contain(d =>
			d.Message.Contains("Product 'elasticsearch' has multiple targets in bundle") &&
			d.Message.Contains("9.2.0") &&
			d.Message.Contains("9.2.0 beta") &&
			d.Message.Contains("9.2.0 ga"));
	}

	[Fact]
	public async Task BundleChangelogs_WithResolve_CopiesChangelogContents()
	{
		// Arrange

		// language=yaml
		var changelog1 =
			"""
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			areas:
			  - Search
			description: This is a test feature
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-test-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			All = true,
			Resolve = true,
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("file:");
		bundleContent.Should().Contain("name: 1755268130-test-feature.yaml");
		bundleContent.Should().Contain("checksum:");
		bundleContent.Should().Contain("type: feature");
		bundleContent.Should().Contain("title: Test feature");
		bundleContent.Should().Contain("product: elasticsearch");
		bundleContent.Should().Contain("target: 9.2.0");
		bundleContent.Should().Contain("prs:");
		bundleContent.Should().Contain("https://github.com/elastic/elasticsearch/pull/100");
		bundleContent.Should().Contain("areas:");
		bundleContent.Should().Contain("- Search");
		bundleContent.Should().Contain("description: This is a test feature");
	}

	[Fact]
	public async Task BundleChangelogs_WithExplicitResolveFalse_OverridesConfigResolveTrue()
	{
		// Arrange - config has resolve: true, but CLI passes Resolve = false (--no-resolve).
		// The explicit CLI value must win.

		// language=yaml
		var configContent =
			"""
			bundle:
			  resolve: true
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var changelog1 =
			"""
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-test-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			All = true,
			Resolve = false,
			Config = configPath,
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);

		// An unresolved bundle has only a file reference — no inline title/type fields
		bundleContent.Should().Contain("name: 1755268130-test-feature.yaml");
		bundleContent.Should().NotContain("title: Test feature");
		bundleContent.Should().NotContain("type: feature");
	}

	[Fact]
	public async Task BundleChangelogs_WithResolve_PreservesSpecialCharactersInUtf8()
	{
		// Arrange - Create changelog with special characters that could be corrupted
		// These characters were reported as being corrupted to "&o0" and "*o0" in the original issue

		// language=yaml
		var changelog1 =
			"""
			title: Feature with special characters & symbols
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.3.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			description: |
			  This feature includes special characters:
			  - Ampersand: & symbol
			  - Asterisk: * symbol
			  - Other special chars: < > " ' / \
			  - Unicode: © ® ™ € £ ¥
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-special-chars.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, Encoding.UTF8, TestContext.Current.CancellationToken);

		var outputPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			All = true,
			Resolve = true,
			Output = outputPath
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		// Read the bundle file with explicit UTF-8 encoding
		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, Encoding.UTF8, TestContext.Current.CancellationToken);

		// Verify special characters are preserved correctly (not corrupted)
		// The original issue reported "&o0" and "*o0" corruption, so we verify the characters are correct
		bundleContent.Should().Contain("&"); // Ampersand should be preserved
		bundleContent.Should().Contain("Feature with special characters & symbols"); // Ampersand in title
		bundleContent.Should().Contain("Ampersand: & symbol"); // Ampersand in description

		// Check that asterisk appears correctly (not corrupted to "*o0")
		bundleContent.Should().Contain("*"); // Asterisk should be preserved
		bundleContent.Should().Contain("Asterisk: * symbol"); // Asterisk in description

		// Verify the ampersand and asterisk are not corrupted
		// The corruption pattern would be "&o0" or "*o0" appearing where we expect "&" or "*"
		// We check that the title contains the correct pattern, not the corrupted one
		var titleLine = bundleContent.Split('\n').FirstOrDefault(l => l.Contains("title:"));
		titleLine.Should().NotBeNull();
		titleLine.Should().Contain("&");
		titleLine.Should().NotContain("&o0"); // Should not be corrupted in title

		// Verify no corruption patterns exist (these would indicate encoding issues)
		bundleContent.Should().NotContain("&o0"); // Should not contain corrupted ampersand
		bundleContent.Should().NotContain("*o0"); // Should not contain corrupted asterisk

		// Verify other special characters are preserved
		bundleContent.Should().Contain("<");
		bundleContent.Should().Contain(">");
		bundleContent.Should().Contain("\"");

		// Verify Unicode characters are preserved
		bundleContent.Should().Contain("©");
		bundleContent.Should().Contain("®");
		bundleContent.Should().Contain("™");
		bundleContent.Should().Contain("€");

		// Verify the content structure is correct
		bundleContent.Should().Contain("title: Feature with special characters & symbols");
		bundleContent.Should().Contain("type: feature");
		bundleContent.Should().Contain("product: elasticsearch");
		bundleContent.Should().Contain("target: 9.3.0");
		bundleContent.Should().Contain("lifecycle: ga");
	}

	[Fact]
	public async Task BundleChangelogs_WithDirectoryOutputPath_CreatesDefaultFilename()
	{
		// Arrange

		// language=yaml
		var changelog1 =
			"""
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-test-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		// Use a directory path with default filename (simulating command layer processing)
		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var outputPath = FileSystem.Path.Combine(outputDir, "changelog-bundle.yaml");

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			All = true,
			Output = outputPath
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		FileSystem.File.Exists(outputPath).Should().BeTrue("Output file should be created");

		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("products:");
		bundleContent.Should().Contain("product: elasticsearch");
		bundleContent.Should().Contain("entries:");
		bundleContent.Should().Contain("name: 1755268130-test-feature.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithResolveAndMissingTitle_ReturnsError()
	{
		// Arrange

		// language=yaml
		var changelog1 =
			"""
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-test-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			All = true,
			Resolve = true,
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("missing required field: title"));
	}

	[Fact]
	public async Task BundleChangelogs_WithResolveAndMissingType_ReturnsError()
	{
		// Arrange

		// language=yaml
		var changelog1 =
			"""
			title: Test feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-test-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			All = true,
			Resolve = true,
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("missing required field: type"));
	}

	[Fact]
	public async Task BundleChangelogs_WithResolveAndMissingProducts_ReturnsError()
	{
		// Arrange

		// language=yaml
		var changelog1 =
			"""
			title: Test feature
			type: feature
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-test-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			All = true,
			Resolve = true,
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("missing required field: products"));
	}

	[Fact]
	public async Task BundleChangelogs_WithResolveAndInvalidProduct_ReturnsError()
	{
		// Arrange

		// language=yaml
		var changelog1 =
			"""
			title: Test feature
			type: feature
			products:
			  - target: 9.2.0
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-test-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			All = true,
			Resolve = true,
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("product entry missing required field: product"));
	}

	[Fact]
	public async Task BundleChangelogs_WithHideFeaturesOption_IncludesHideFeaturesInBundle()
	{
		// Arrange - Test that --hide-features option writes feature IDs to the bundle output

		// language=yaml
		var changelog1 =
			"""
			title: Feature with hidden flag
			type: feature
			feature-id: feature:hidden-api
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			All = true,
			HideFeatures = ["feature:hidden-api", "feature:another-hidden"],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		// Verify that hide-features field is included in the bundle output
		bundleContent.Should().Contain("hide-features:");
		bundleContent.Should().Contain("- feature:hidden-api");
		bundleContent.Should().Contain("- feature:another-hidden");
	}

	[Fact]
	public async Task BundleChangelogs_WithoutHideFeaturesOption_OmitsHideFeaturesFieldInOutput()
	{
		// Arrange - Test that without --hide-features option, no hide-features field is written

		// language=yaml
		var changelog1 =
			"""
			title: Regular feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.3.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			All = true,
			// No HideFeatures
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		// Verify that hide-features field is NOT written when not specified
		bundleContent.Should().NotContain("hide-features:");
	}

	[Fact]
	public async Task BundleChangelogs_WithHideFeaturesFromFile_IncludesHideFeaturesInBundle()
	{
		// Arrange - Test that --hide-features can read feature IDs from a file

		// language=yaml
		var changelog1 =
			"""
			title: Feature with hidden flag
			type: feature
			feature-id: feature:from-file
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		// Create feature IDs file
		var featureIdsFile = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "feature-ids.txt");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(featureIdsFile)!);
		await FileSystem.File.WriteAllTextAsync(featureIdsFile, "feature:from-file\nfeature:another", TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			All = true,
			HideFeatures = [featureIdsFile],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		// Verify that hide-features field contains feature IDs from the file
		bundleContent.Should().Contain("hide-features:");
		bundleContent.Should().Contain("- feature:from-file");
		bundleContent.Should().Contain("- feature:another");
	}

	[Fact]
	public async Task BundleChangelogs_WithRepoOption_IncludesRepoInBundleProducts()
	{
		// Arrange - Test that --repo option sets the repo field in the bundle output

		// language=yaml
		var changelog1 =
			"""
			title: Serverless feature
			type: feature
			products:
			  - product: cloud-serverless
			    target: 2025-12-02
			prs:
			  - https://github.com/elastic/cloud/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-serverless-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			All = true,
			Repo = "cloud", // Set repo to "cloud" - different from product ID "cloud-serverless"
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		// Verify that repo field is included in the bundle output
		bundleContent.Should().Contain("product: cloud-serverless");
		bundleContent.Should().Contain("repo: cloud");
	}

	[Fact]
	public async Task BundleChangelogs_WithoutRepoOption_OmitsRepoFieldInOutput()
	{
		// Arrange - Test that without --repo option, no repo field is written to the bundle

		// language=yaml
		var changelog1 =
			"""
			title: Elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.3.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-es-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			All = true,
			// No --repo option
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		// Verify that no repo field is written when not specified
		bundleContent.Should().Contain("product: elasticsearch");
		bundleContent.Should().NotContain("repo:");
	}

	[Fact]
	public async Task BundleChangelogs_WithBundleLevelRepoConfig_UsesConfigRepoWhenOptionNotSpecified()
	{
		// Arrange - bundle.repo in config is used when --repo is not provided on the CLI

		// language=yaml
		var configContent =
			"""
			bundle:
			  repo: cloud
			  owner: elastic
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var changelog1 =
			"""
			title: Serverless feature
			type: feature
			products:
			  - product: cloud-serverless
			    target: 2025-06-01
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/cloud/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-serverless-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var outputPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(outputPath)!);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			All = true,
			Config = configPath,
			Output = outputPath
			// No --repo or --owner: should be picked up from bundle config
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Expected bundling to succeed, but got errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("repo: cloud", "bundle.repo config should be applied when --repo is not specified");
	}

	[Fact]
	public async Task BundleChangelogs_WithRepoOptionAndBundleLevelConfig_CliOptionTakesPrecedence()
	{
		// Arrange - explicit --repo overrides bundle.repo in config

		// language=yaml
		var configContent =
			"""
			bundle:
			  repo: wrong-repo
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var changelog1 =
			"""
			title: Serverless feature
			type: feature
			products:
			  - product: cloud-serverless
			    target: 2025-06-01
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/cloud/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-serverless-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var outputPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(outputPath)!);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			All = true,
			Config = configPath,
			Output = outputPath,
			Repo = "cloud"  // explicit CLI --repo should win over bundle.repo: wrong-repo
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Expected bundling to succeed, but got errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("repo: cloud", "explicit --repo should override bundle.repo config");
		bundleContent.Should().NotContain("repo: wrong-repo");
	}

	[Fact]
	public async Task BundleChangelogs_WithOutputProductsAndRepo_IncludesRepoInAllProducts()
	{
		// Arrange - Test that --repo option works with --output-products

		// language=yaml
		var changelog1 =
			"""
			title: Feature for serverless
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.3.0
			prs:
			  - https://github.com/elastic/cloud/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			All = true,
			Repo = "cloud",
			OutputProducts =
			[
				new ProductArgument { Product = "cloud-serverless", Target = "2025-12-02", Lifecycle = "ga" },
				new ProductArgument { Product = "elasticsearch-serverless", Target = "2025-12-02", Lifecycle = "ga" }
			],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		// Verify that repo field is included for all products
		bundleContent.Should().Contain("product: cloud-serverless");
		bundleContent.Should().Contain("product: elasticsearch-serverless");
		// The repo field should appear for each product (or at least once)
		bundleContent.Should().Contain("repo: cloud");
	}

	[Fact]
	public async Task BundleChangelogs_WithConfigOutputDirectory_WhenOutputNotSpecified_UsesConfigOutputDirectory()
	{
		// Arrange - When --output is not specified, use bundle.output_directory from config if set

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);

		// language=yaml
		var configContent =
			$"""
			bundle:
			  directory: "{_changelogDir.Replace("\\", "/")}"
			  output_directory: "{outputDir.Replace("\\", "/")}"
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), "config-output-dir", "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var changelog1 =
			"""
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Config = configPath,
			Output = null,
			All = true
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Expected bundling to succeed. Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var expectedOutputPath = FileSystem.Path.Combine(outputDir, "changelog-bundle.yaml");
		FileSystem.File.Exists(expectedOutputPath).Should().BeTrue("Bundle should be created in config output_directory");

		var bundleContent = await FileSystem.File.ReadAllTextAsync(expectedOutputPath, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("product: elasticsearch");
		bundleContent.Should().Contain("name: 1755268130-feature.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithConfigDirectory_WhenDirectoryNotSpecified_UsesConfigDirectory()
	{
		// Arrange - When --directory is not specified (null), use bundle.directory from config if set

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);

		// language=yaml
		var configContent =
			$"""
			bundle:
			  directory: "{_changelogDir.Replace("\\", "/")}"
			  output_directory: "{outputDir.Replace("\\", "/")}"
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), "config-dir", "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var changelog1 =
			"""
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = null,
			Config = configPath,
			Output = null,
			All = true
		};

		// Act - Directory not specified, so ApplyConfigDefaults uses config.Bundle.Directory
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Expected bundling to succeed. Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var expectedOutputPath = FileSystem.Path.Combine(outputDir, "changelog-bundle.yaml");
		FileSystem.File.Exists(expectedOutputPath).Should().BeTrue("Bundle should use config directory and output_directory");

		var bundleContent = await FileSystem.File.ReadAllTextAsync(expectedOutputPath, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("product: elasticsearch");
		bundleContent.Should().Contain("name: 1755268130-feature.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithExplicitDirectory_OverridesConfigDirectory()
	{
		// Arrange - config has directory pointing elsewhere, but CLI passes --directory explicitly.
		// The explicit CLI value must win (e.g. --directory . when cwd has changelogs).

		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(configDir);
		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);

		// language=yaml
		var configContent =
			$"""
			bundle:
			  directory: "{configDir.Replace("\\", "/")}"
			  output_directory: "{outputDir.Replace("\\", "/")}"
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), "config-dir-override", "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var changelog1 =
			"""
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Config = configPath,
			Output = FileSystem.Path.Combine(outputDir, "bundle.yaml"),
			All = true
		};

		// Act - Explicit Directory overrides config.Bundle.Directory
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert - used _changelogDir (CLI), not configDir (config)
		result.Should().BeTrue($"Expected bundling to succeed. Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("product: elasticsearch");
		bundleContent.Should().Contain("name: 1755268130-feature.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithProfileHideFeatures_IncludesHideFeaturesInBundle()
	{
		// Arrange - Test that hide_features in a profile config are written to the bundle output

		// language=yaml
		var configContent =
			"""
			bundle:
			  profiles:
			    es-release:
			      products: "elasticsearch {version} {lifecycle}"
			      output: "elasticsearch-{version}.yaml"
			      hide_features:
			        - feature:profile-hidden
			        - feature:another-profile-hidden
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), "config", "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var changelog1 =
			"""
			title: Elasticsearch feature
			type: feature
			feature-id: feature:profile-hidden
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Profile = "es-release",
			ProfileArgument = "9.2.0",
			Config = configPath,
			OutputDirectory = outputDir
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Expected bundling to succeed, but got errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		// Find the output file
		var outputFiles = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		outputFiles.Should().NotBeEmpty("Expected an output file to be created");
		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputFiles[0], TestContext.Current.CancellationToken);

		// Verify that hide-features from the profile are written to the bundle
		bundleContent.Should().Contain("hide-features:");
		bundleContent.Should().Contain("- feature:profile-hidden");
		bundleContent.Should().Contain("- feature:another-profile-hidden");
	}

	[Fact]
	public async Task BundleChangelogs_WithProfile_OnlyProfileHideFeaturesAreUsed()
	{
		// Arrange - In profile mode, only hide_features from the profile config are written to the bundle.
		// Any HideFeatures passed directly to the service are ignored (the CLI now rejects --hide-features
		// in profile mode, so this combination is not reachable from the command layer).

		// language=yaml
		var configContent =
			"""
			bundle:
			  profiles:
			    es-release:
			      products: "elasticsearch {version} {lifecycle}"
			      output: "elasticsearch-{version}.yaml"
			      hide_features:
			        - feature:from-profile
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), "config2", "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var changelog1 =
			"""
			title: Elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Profile = "es-release",
			ProfileArgument = "9.2.0",
			Config = configPath,
			OutputDirectory = outputDir
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Expected bundling to succeed, but got errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		// Find the output file
		var outputFiles = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		outputFiles.Should().NotBeEmpty("Expected an output file to be created");
		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputFiles[0], TestContext.Current.CancellationToken);

		// Verify that only the profile hide-features are present
		bundleContent.Should().Contain("hide-features:");
		bundleContent.Should().Contain("- feature:from-profile");
	}

	[Fact]
	public async Task BundleChangelogs_WithProfileMultipleHideFeatures_AllProfileFeaturesArePresent()
	{
		// Arrange - All hide_features from the profile are written to the bundle

		// language=yaml
		var configContent =
			"""
			bundle:
			  profiles:
			    es-release:
			      products: "elasticsearch {version} {lifecycle}"
			      output: "elasticsearch-{version}.yaml"
			      hide_features:
			        - feature:profile-one
			        - feature:profile-two
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), "config3", "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var changelog1 =
			"""
			title: Elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Profile = "es-release",
			ProfileArgument = "9.2.0",
			Config = configPath,
			OutputDirectory = outputDir
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Expected bundling to succeed, but got errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var outputFiles = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		outputFiles.Should().NotBeEmpty("Expected an output file to be created");
		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputFiles[0], TestContext.Current.CancellationToken);

		bundleContent.Should().Contain("- feature:profile-one");
		bundleContent.Should().Contain("- feature:profile-two");
	}

	[Fact]
	public async Task BundleChangelogs_WithComments_ProducesNormalizedChecksum()
	{
		// Arrange - File with comment headers should produce a normalized checksum
		// (i.e. comments are stripped before hashing)

		// language=yaml
		var changelogWithComments =
			"""
			# This is a comment header generated by changelog add
			# PR: https://github.com/elastic/elasticsearch/pull/100
			title: Feature with comments
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		// language=yaml
		var changelogWithoutComments =
			"""
			title: Feature with comments
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-with-comments.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelogWithComments, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			All = true,
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);

		// The checksum in the bundle should be the normalized hash (comments stripped)
		var expectedChecksum = ComputeSha1(changelogWithComments);
		bundleContent.Should().Contain($"checksum: {expectedChecksum}");

		// Both versions of the content should produce the same checksum (comments are normalized away)
		var checksumFromCommented = ComputeSha1(changelogWithComments);
		var checksumFromUncommented = ComputeSha1(changelogWithoutComments);
		checksumFromCommented.Should().Be(checksumFromUncommented,
			"checksums should be identical regardless of comments");
	}

	[Fact]
	public async Task BundleChangelogs_WithAndWithoutComments_ProduceSameChecksum()
	{
		// Arrange - Two separate bundles, one with and one without comments,
		// should store the same checksum for semantically identical content

		// language=yaml
		var changelogWithComments =
			"""
			# Auto-generated by changelog add
			# Do not edit this comment block
			title: Shared feature
			type: enhancement
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/500
			""";

		// language=yaml
		var changelogWithoutComments =
			"""
			title: Shared feature
			type: enhancement
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/500
			""";

		// Bundle with comments
		var dir1 = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(dir1);
		var file1 = FileSystem.Path.Combine(dir1, "1755268130-shared.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelogWithComments, TestContext.Current.CancellationToken);

		var output1 = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle1.yaml");
		var result1 = await Service.BundleChangelogs(Collector, new BundleChangelogsArguments
		{
			Directory = dir1, All = true, Output = output1
		}, TestContext.Current.CancellationToken);

		// Bundle without comments
		var dir2 = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(dir2);
		var file2 = FileSystem.Path.Combine(dir2, "1755268130-shared.yaml");
		await FileSystem.File.WriteAllTextAsync(file2, changelogWithoutComments, TestContext.Current.CancellationToken);

		var output2 = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle2.yaml");
		var result2 = await Service.BundleChangelogs(Collector, new BundleChangelogsArguments
		{
			Directory = dir2, All = true, Output = output2
		}, TestContext.Current.CancellationToken);

		// Assert
		result1.Should().BeTrue();
		result2.Should().BeTrue();

		var bundle1 = await FileSystem.File.ReadAllTextAsync(output1, TestContext.Current.CancellationToken);
		var bundle2 = await FileSystem.File.ReadAllTextAsync(output2, TestContext.Current.CancellationToken);

		// Extract checksum values from both bundles
		var checksum1 = ExtractChecksum(bundle1);
		var checksum2 = ExtractChecksum(bundle2);

		checksum1.Should().Be(checksum2,
			"bundles from files with and without comments should have the same normalized checksum");
	}

	[Fact]
	public async Task BundleChangelogs_WithDifferentData_ProducesDifferentChecksum()
	{
		// Arrange - Files with actually different data should produce different checksums

		// language=yaml
		var changelog1 =
			"""
			title: Feature A
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		// language=yaml
		var changelog2 =
			"""
			title: Feature B
			type: enhancement
			products:
			  - product: elasticsearch
			    target: 9.3.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/200
			""";

		// Bundle first file
		var dir1 = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(dir1);
		await FileSystem.File.WriteAllTextAsync(FileSystem.Path.Combine(dir1, "1755268130-a.yaml"), changelog1, TestContext.Current.CancellationToken);

		var output1 = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle1.yaml");
		await Service.BundleChangelogs(Collector, new BundleChangelogsArguments
		{
			Directory = dir1, All = true, Output = output1
		}, TestContext.Current.CancellationToken);

		// Bundle second file
		var dir2 = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(dir2);
		await FileSystem.File.WriteAllTextAsync(FileSystem.Path.Combine(dir2, "1755268130-b.yaml"), changelog2, TestContext.Current.CancellationToken);

		var output2 = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle2.yaml");
		await Service.BundleChangelogs(Collector, new BundleChangelogsArguments
		{
			Directory = dir2, All = true, Output = output2
		}, TestContext.Current.CancellationToken);

		// Assert
		var bundle1 = await FileSystem.File.ReadAllTextAsync(output1, TestContext.Current.CancellationToken);
		var bundle2 = await FileSystem.File.ReadAllTextAsync(output2, TestContext.Current.CancellationToken);

		var checksum1 = ExtractChecksum(bundle1);
		var checksum2 = ExtractChecksum(bundle2);

		checksum1.Should().NotBe(checksum2,
			"files with different data should produce different checksums");
	}

	[Fact]
	public async Task AmendBundle_WithComments_ProducesNormalizedChecksum()
	{
		// Arrange - Amend service should also use normalized checksums

		// Create a base bundle file
		var bundleDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Combine(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleContent =
			"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries: []
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Create a changelog file with comments
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// language=yaml
		var changelogWithComments =
			"""
			# This is a comment header
			# Generated by changelog add
			title: Amend feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var changelogFile = FileSystem.Path.Combine(changelogDir, "1755268130-amend-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelogWithComments, TestContext.Current.CancellationToken);

		var amendService = new ChangelogBundleAmendService(LoggerFactory, FileSystem);

		var amendInput = new AmendBundleArguments
		{
			BundlePath = bundleFile,
			AddFiles = [changelogFile],
			Resolve = false
		};

		// Act
		var result = await amendService.AmendBundle(Collector, amendInput, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		// Read the amend file
		var amendFiles = ChangelogBundleAmendService.DiscoverAmendFiles(FileSystem, bundleFile);
		amendFiles.Should().HaveCount(1);

		var amendContent = await FileSystem.File.ReadAllTextAsync(amendFiles[0], TestContext.Current.CancellationToken);

		// The checksum should be the normalized hash (comments stripped before hashing)
		var expectedChecksum = ComputeSha1(changelogWithComments);
		amendContent.Should().Contain($"checksum: {expectedChecksum}");
	}

	[Fact]
	public async Task AmendBundle_WithResolve_ProducesNormalizedChecksum()
	{
		// Arrange - Amend with --resolve should also use normalized checksums

		// Create a base bundle file
		var bundleDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Combine(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleContent =
			"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries: []
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Create a changelog file with comments
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// language=yaml
		var changelogWithComments =
			"""
			# Auto-generated comment
			title: Resolved amend feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/200
			""";

		var changelogFile = FileSystem.Path.Combine(changelogDir, "1755268140-resolved-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelogWithComments, TestContext.Current.CancellationToken);

		var amendService = new ChangelogBundleAmendService(LoggerFactory, FileSystem);

		var amendInput = new AmendBundleArguments
		{
			BundlePath = bundleFile,
			AddFiles = [changelogFile],
			Resolve = true
		};

		// Act
		var result = await amendService.AmendBundle(Collector, amendInput, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		// Read the amend file
		var amendFiles = ChangelogBundleAmendService.DiscoverAmendFiles(FileSystem, bundleFile);
		amendFiles.Should().HaveCount(1);

		var amendContent = await FileSystem.File.ReadAllTextAsync(amendFiles[0], TestContext.Current.CancellationToken);

		// The checksum should be the normalized hash
		var expectedChecksum = ComputeSha1(changelogWithComments);
		amendContent.Should().Contain($"checksum: {expectedChecksum}");

		// Resolved entries should include inline data
		amendContent.Should().Contain("title: Resolved amend feature");
		amendContent.Should().Contain("type: feature");
	}

	[Fact]
	public async Task BundleChangelogs_WithProfile_OutputProducts_OverridesProductsArray()
	{
		// Arrange - output_products overrides the products array written to the bundle.
		// The profile uses a wildcard lifecycle to match any changelog for the given version,
		// while output_products pins the lifecycle advertised in the bundle output to "ga".

		// language=yaml
		var configContent =
			"""
			bundle:
			  profiles:
			    es-release:
			      products: "elasticsearch {version} *"
			      output: "elasticsearch-{version}.yaml"
			      output_products: "elasticsearch {version} ga"
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var changelog1 =
			"""
			title: Elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: preview
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Profile = "es-release",
			ProfileArgument = "9.2.0",
			Config = configPath,
			OutputDirectory = outputDir
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Expected bundling to succeed, but got errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var outputFiles = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		outputFiles.Should().NotBeEmpty("Expected an output file to be created");
		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputFiles[0], TestContext.Current.CancellationToken);

		// output_products overrides: the products array in the bundle output should have lifecycle: ga
		// even though the matched changelog has lifecycle: preview
		bundleContent.Should().Contain("lifecycle: ga", "output_products should write lifecycle: ga to the bundle products array");
	}

	[Fact]
	public async Task BundleChangelogs_WithProfile_MalformedOutputProducts_EmitsError()
	{
		var configContent =
			"""
			bundle:
			  profiles:
			    es-release:
			      products: "elasticsearch {version} *"
			      output: "elasticsearch-{version}.yaml"
			      output_products: "elasticsearch {version} ga extra-token"
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var changelog1 =
			"""
			title: Elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Profile = "es-release",
			ProfileArgument = "9.2.0",
			Config = configPath,
			OutputDirectory = outputDir
		};

		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d =>
			d.Message.Contains("at most three space-separated fields", StringComparison.Ordinal) &&
			d.Message.Contains("elasticsearch 9.2.0 ga extra-token", StringComparison.Ordinal));
	}

	[Fact]
	public async Task BundleChangelogs_WithProfile_MalformedProductsPattern_EmitsError()
	{
		var configContent =
			"""
			bundle:
			  profiles:
			    es-release:
			      products: "elasticsearch {version} ga extra bad"
			      output: "elasticsearch-{version}.yaml"
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var changelog1 =
			"""
			title: Elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Profile = "es-release",
			ProfileArgument = "9.2.0",
			Config = configPath,
			OutputDirectory = outputDir
		};

		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d =>
			d.Message.Contains("Profile 'es-release':", StringComparison.Ordinal) &&
			d.Message.Contains("at most three space-separated fields", StringComparison.Ordinal) &&
			d.Message.Contains("elasticsearch 9.2.0 ga extra bad", StringComparison.Ordinal));
	}

	[Fact]
	public async Task BundleChangelogs_WithProfile_RepoAndOwner_WritesValuesToProductEntries()
	{
		// Arrange - repo and owner in the profile are written to each product entry in the bundle.
		// Note: date-based versions like "2025-06-01" contain dashes that InferLifecycle treats as
		// a semver prerelease, so we use a wildcard lifecycle in the products pattern to match any
		// lifecycle value present in the changelog files.

		// language=yaml
		var configContent =
			"""
			bundle:
			  profiles:
			    serverless-release:
			      products: "cloud-serverless {version} *"
			      output: "serverless-{version}.yaml"
			      repo: cloud
			      owner: elastic
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var changelog1 =
			"""
			title: Serverless feature
			type: feature
			products:
			  - product: cloud-serverless
			    target: 2025-06-01
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/cloud/pull/200
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-serverless-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Profile = "serverless-release",
			ProfileArgument = "2025-06-01",
			Config = configPath,
			OutputDirectory = outputDir
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Expected bundling to succeed, but got errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var outputFiles = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		outputFiles.Should().NotBeEmpty("Expected an output file to be created");
		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputFiles[0], TestContext.Current.CancellationToken);

		bundleContent.Should().Contain("repo: cloud", "Profile repo should be written to bundle product entries");
		bundleContent.Should().Contain("owner: elastic", "Profile owner should be written to bundle product entries");
	}

	[Fact]
	public async Task BundleChangelogs_WithProfile_BundleLevelRepo_AppliesWhenProfileOmitsRepo()
	{
		// Arrange - repo is set at bundle level, not in the profile; profile should inherit it

		// language=yaml
		var configContent =
			"""
			bundle:
			  repo: elasticsearch
			  owner: elastic
			  profiles:
			    es-release:
			      products: "elasticsearch {version} {lifecycle}"
			      output: "elasticsearch-{version}.yaml"
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var changelog1 =
			"""
			title: Elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Profile = "es-release",
			ProfileArgument = "9.2.0",
			Config = configPath,
			OutputDirectory = outputDir
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Expected bundling to succeed, but got errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var outputFiles = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		outputFiles.Should().NotBeEmpty();
		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputFiles[0], TestContext.Current.CancellationToken);

		bundleContent.Should().Contain("repo: elasticsearch", "bundle-level repo should be applied when profile omits repo");
	}

	[Fact]
	public async Task BundleChangelogs_WithProfile_ProfileRepoOverridesBundleRepo()
	{
		// Arrange - both bundle-level and profile-level repo are set; profile-level wins

		// language=yaml
		var configContent =
			"""
			bundle:
			  repo: wrong-repo
			  profiles:
			    es-release:
			      products: "elasticsearch {version} {lifecycle}"
			      output: "elasticsearch-{version}.yaml"
			      repo: elasticsearch
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var changelog1 =
			"""
			title: Elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Profile = "es-release",
			ProfileArgument = "9.2.0",
			Config = configPath,
			OutputDirectory = outputDir
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Expected bundling to succeed, but got errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var outputFiles = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		outputFiles.Should().NotBeEmpty();
		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputFiles[0], TestContext.Current.CancellationToken);

		bundleContent.Should().Contain("repo: elasticsearch", "profile-level repo should override bundle-level repo");
		bundleContent.Should().NotContain("repo: wrong-repo", "bundle-level repo should be overridden by profile-level repo");
	}

	[Fact]
	public async Task BundleChangelogs_WithProfile_NoRepoOwner_PreservesExistingFallbackBehavior()
	{
		// Arrange - when profile has no repo/owner, the bundle products have no repo field
		// (existing fallback: product ID is used at render time if no repo is present)

		// language=yaml
		var configContent =
			"""
			bundle:
			  profiles:
			    es-release:
			      products: "elasticsearch {version} {lifecycle}"
			      output: "elasticsearch-{version}.yaml"
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var changelog1 =
			"""
			title: Elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Profile = "es-release",
			ProfileArgument = "9.2.0",
			Config = configPath,
			OutputDirectory = outputDir
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert — succeeds without error; no repo field written to products
		result.Should().BeTrue($"Expected bundling to succeed, but got errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var outputFiles = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		outputFiles.Should().NotBeEmpty("Expected an output file to be created");
		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputFiles[0], TestContext.Current.CancellationToken);

		bundleContent.Should().NotContain("repo:", "No repo field should be present when profile omits repo");
		bundleContent.Should().NotContain("owner:", "No owner field should be present when profile omits owner");
	}

	[Fact]
	public async Task BundleChangelogs_WithProfileMode_MissingConfig_ReturnsErrorWithAdvice()
	{
		// Arrange - no config file exists at ./changelog.yml or ./docs/changelog.yml.
		// Use a fresh MockFileSystem with a known CWD so discovery returns no results.
		var cwdFs = new System.IO.Abstractions.TestingHelpers.MockFileSystem(
			null,
			currentDirectory: "/empty-project"
		);
		cwdFs.Directory.CreateDirectory("/empty-project");
		var service = new ChangelogBundlingService(LoggerFactory, ConfigurationContext, cwdFs);

		var input = new BundleChangelogsArguments
		{
			Profile = "es-release",
			ProfileArgument = "9.2.0"
			// Config intentionally omitted — triggers CWD discovery
		};

		// Act
		var result = await service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse("Should fail when no config file is found");
		Collector.Diagnostics.Should().ContainSingle(d =>
			d.Severity == Severity.Error &&
			(d.Message.Contains("changelog.yml") || d.Message.Contains("changelog init")),
			"Error message should mention changelog.yml or advise running changelog init"
		);
	}

	[Fact]
	public async Task BundleChangelogs_WithProfileMode_ConfigAtCurrentDir_LoadsSuccessfully()
	{
		// Arrange - changelog.yml is at ./changelog.yml (in the current working directory)
		var cwdFs = new System.IO.Abstractions.TestingHelpers.MockFileSystem(
			null,
			currentDirectory: "/test-root"
		);
		cwdFs.Directory.CreateDirectory("/test-root");
		cwdFs.Directory.CreateDirectory("/test-root/changelogs");
		cwdFs.Directory.CreateDirectory("/test-root/output");

		// language=yaml
		var configContent =
			"""
			bundle:
			  directory: /test-root/changelogs
			  profiles:
			    es-release:
			      products: "elasticsearch {version} {lifecycle}"
			      output: "elasticsearch-{version}.yaml"
			""";
		await cwdFs.File.WriteAllTextAsync("/test-root/changelog.yml", configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var changelogContent =
			"""
			title: Elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";
		await cwdFs.File.WriteAllTextAsync("/test-root/changelogs/1755268130-feature.yaml", changelogContent, TestContext.Current.CancellationToken);

		var service = new ChangelogBundlingService(LoggerFactory, ConfigurationContext, cwdFs);

		var input = new BundleChangelogsArguments
		{
			Profile = "es-release",
			ProfileArgument = "9.2.0",
			OutputDirectory = "/test-root/output"
			// Config intentionally omitted — should discover /test-root/changelog.yml
		};

		// Act
		var result = await service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Expected bundling to succeed. Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);
		cwdFs.Directory.GetFiles("/test-root/output", "*.yaml").Should().NotBeEmpty("Expected output file to be created");
	}

	[Fact]
	public async Task BundleChangelogs_WithProfileMode_ConfigAtDocsSubdir_LoadsSuccessfully()
	{
		// Arrange - changelog.yml is at ./docs/changelog.yml (the second discovery candidate)
		var cwdFs = new System.IO.Abstractions.TestingHelpers.MockFileSystem(
			null,
			currentDirectory: "/test-root"
		);
		cwdFs.Directory.CreateDirectory("/test-root");
		cwdFs.Directory.CreateDirectory("/test-root/docs");
		cwdFs.Directory.CreateDirectory("/test-root/changelogs");
		cwdFs.Directory.CreateDirectory("/test-root/output");

		// language=yaml
		var configContent =
			"""
			bundle:
			  directory: /test-root/changelogs
			  profiles:
			    es-release:
			      products: "elasticsearch {version} {lifecycle}"
			      output: "elasticsearch-{version}.yaml"
			""";
		// Config is in docs/ subdir, not in CWD directly
		await cwdFs.File.WriteAllTextAsync("/test-root/docs/changelog.yml", configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var changelogContent =
			"""
			title: Elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";
		await cwdFs.File.WriteAllTextAsync("/test-root/changelogs/1755268130-feature.yaml", changelogContent, TestContext.Current.CancellationToken);

		var service = new ChangelogBundlingService(LoggerFactory, ConfigurationContext, cwdFs);

		var input = new BundleChangelogsArguments
		{
			Profile = "es-release",
			ProfileArgument = "9.2.0",
			OutputDirectory = "/test-root/output"
			// Config intentionally omitted — should discover /test-root/docs/changelog.yml
		};

		// Act
		var result = await service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Expected bundling to succeed. Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);
		cwdFs.Directory.GetFiles("/test-root/output", "*.yaml").Should().NotBeEmpty("Expected output file to be created");
	}

	// ─── Phase 3: URL list file and combined version+report ─────────────────────────────

	[Fact]
	public async Task BundleChangelogs_WithProfile_UrlListFile_PrUrls_FiltersCorrectly()
	{
		// Arrange - profile argument is a text file containing fully-qualified PR URLs
		var configContent = $"""
			bundle:
			  directory: {_changelogDir}
			  profiles:
			    release:
			      output: "bundle.yaml"
			""";
		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var changelog1 =
			"""
			title: Matched PR
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
			title: Unmatched PR
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/999
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-matched.yaml");
		var file2 = FileSystem.Path.Combine(_changelogDir, "1755268140-unmatched.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var urlFile = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "prs.txt");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(urlFile)!);
		await FileSystem.File.WriteAllTextAsync(
			urlFile,
			"https://github.com/elastic/elasticsearch/pull/100\n",
			TestContext.Current.CancellationToken
		);

		// Profile writes to _changelogDir/bundle.yaml because bundle.directory is the fallback for output_directory
		var expectedOutputPath = FileSystem.Path.Combine(_changelogDir, "bundle.yaml");

		var input = new BundleChangelogsArguments
		{
			Config = configPath,
			Profile = "release",
			ProfileArgument = urlFile
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(expectedOutputPath, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("1755268130-matched.yaml");
		bundleContent.Should().NotContain("1755268140-unmatched.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithProfile_UrlListFile_IssueUrls_FiltersCorrectly()
	{
		// Arrange - profile argument is a text file containing fully-qualified issue URLs
		var configContent = $"""
			bundle:
			  directory: {_changelogDir}
			  profiles:
			    release:
			      output: "bundle.yaml"
			""";
		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var changelog1 =
			"""
			title: Matched Issue
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			issues:
			  - https://github.com/elastic/elasticsearch/issues/100
			""";
		// language=yaml
		var changelog2 =
			"""
			title: Unmatched Issue
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			issues:
			  - https://github.com/elastic/elasticsearch/issues/999
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-matched.yaml");
		var file2 = FileSystem.Path.Combine(_changelogDir, "1755268140-unmatched.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var urlFile = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "issues.txt");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(urlFile)!);
		await FileSystem.File.WriteAllTextAsync(
			urlFile,
			"https://github.com/elastic/elasticsearch/issues/100\n",
			TestContext.Current.CancellationToken
		);

		// Profile writes to _changelogDir/bundle.yaml (output: "bundle.yaml" + no output_directory in config)
		// Profile writes to _changelogDir/bundle.yaml because bundle.directory is the fallback for output_directory
		var expectedOutputPath = FileSystem.Path.Combine(_changelogDir, "bundle.yaml");

		var input = new BundleChangelogsArguments
		{
			Config = configPath,
			Profile = "release",
			ProfileArgument = urlFile
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(expectedOutputPath, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("1755268130-matched.yaml");
		bundleContent.Should().NotContain("1755268140-unmatched.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithProfile_UrlListFile_Numbers_ReturnsError()
	{
		// Arrange - file contains bare PR numbers (not fully-qualified URLs)
		var configContent =
			"""
			bundle:
			  profiles:
			    release:
			      output: "bundle.yaml"
			""";
		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var changelogFile = FileSystem.Path.Combine(_changelogDir, "1755268130-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile,
			"""
			title: Feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""", TestContext.Current.CancellationToken);

		var urlFile = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "prs.txt");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(urlFile)!);
		await FileSystem.File.WriteAllTextAsync(urlFile, "100\n200\n", TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Config = configPath,
			Profile = "release",
			ProfileArgument = urlFile
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse("Should fail when file contains bare numbers");
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("fully-qualified GitHub URLs"),
			"Error should mention fully-qualified URLs requirement"
		);
	}

	[Fact]
	public async Task BundleChangelogs_WithProfile_UrlListFile_MixedPrsAndIssues_ReturnsError()
	{
		// Arrange - file contains both PR and issue URLs
		var configContent =
			"""
			bundle:
			  profiles:
			    release:
			      output: "bundle.yaml"
			""";
		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var changelogFile = FileSystem.Path.Combine(_changelogDir, "1755268130-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile,
			"""
			title: Feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""", TestContext.Current.CancellationToken);

		var urlFile = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "mixed.txt");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(urlFile)!);
		await FileSystem.File.WriteAllTextAsync(
			urlFile,
			"https://github.com/elastic/elasticsearch/pull/100\nhttps://github.com/elastic/elasticsearch/issues/200\n",
			TestContext.Current.CancellationToken
		);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Config = configPath,
			Profile = "release",
			ProfileArgument = urlFile
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse("Should fail when file mixes PR and issue URLs");
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("only pull request URLs or only issue URLs"),
			"Error should mention homogeneous URL requirement"
		);
	}

	[Fact]
	public async Task BundleChangelogs_WithProfile_CombinedVersionAndReport_SubstitutesVersionCorrectly()
	{
		// Arrange - version + report: version used for {version} substitution; report used for PR filter
		var configContent =
			"""
			bundle:
			  profiles:
			    serverless-release:
			      output_products: "cloud-serverless {version}"
			      output: "serverless-{version}.yaml"
			""";
		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var changelog1 =
			"""
			title: Serverless Feb feature
			type: feature
			products:
			  - product: cloud-serverless
			    target: 2026-02-01
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/cloud/pull/100
			""";
		// language=yaml
		var changelog2 =
			"""
			title: Unmatched PR
			type: feature
			products:
			  - product: cloud-serverless
			    target: 2026-02-02
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/cloud/pull/999
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-feb.yaml");
		var file2 = FileSystem.Path.Combine(_changelogDir, "1755268140-other.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var urlFile = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "prs.txt");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(urlFile)!);
		await FileSystem.File.WriteAllTextAsync(
			urlFile,
			"https://github.com/elastic/cloud/pull/100\n",
			TestContext.Current.CancellationToken
		);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Config = configPath,
			Profile = "serverless-release",
			ProfileArgument = "2026-02",   // version string
			ProfileReport = urlFile,        // URL list file (Phase 3.4)
			OutputDirectory = outputDir
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var outputFiles = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		outputFiles.Should().NotBeEmpty();

		// Output file name should use the version (not "unknown")
		outputFiles[0].Should().Contain("2026-02", "Output file path should contain the version string");

		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputFiles[0], TestContext.Current.CancellationToken);
		// Only the matched PR should be bundled
		bundleContent.Should().Contain("1755268130-feb.yaml");
		bundleContent.Should().NotContain("1755268140-other.yaml");
		// Output products should contain the version
		bundleContent.Should().Contain("cloud-serverless");
		bundleContent.Should().Contain("2026-02");
	}

	[Fact]
	public async Task BundleChangelogs_WithProfile_CombinedVersion_ReportArgLooksLikeVersion_ReturnsError()
	{
		// If the first profile arg looks like a report but a second arg is also provided, error
		var configContent =
			"""
			bundle:
			  profiles:
			    serverless-release:
			      output: "serverless-{version}.yaml"
			""";
		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// A "fake" HTML file to act as the profile arg (simulating user accidentally reversing the order)
		var reportFile = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "report.html");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(reportFile)!);
		await FileSystem.File.WriteAllTextAsync(reportFile, "<html></html>", TestContext.Current.CancellationToken);

		var urlFile = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "prs.txt");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(urlFile)!);
		await FileSystem.File.WriteAllTextAsync(urlFile, "https://github.com/elastic/cloud/pull/100\n", TestContext.Current.CancellationToken);

		// Act: profileArg is a file (should be version), profileReport is a URL file — report arg and version arg are swapped
		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Config = configPath,
			Profile = "serverless-release",
			ProfileArgument = reportFile,  // wrong — this looks like a file, should be a version
			ProfileReport = urlFile
		};

		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse("Should fail when first arg looks like a report");
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("version string"),
			"Error should mention that the first arg should be the version"
		);
	}

	[Fact]
	public async Task BundleChangelogs_WithProfile_CombinedVersion_ProfileHasProducts_ReturnsError()
	{
		// A profile with a products pattern cannot also use a report/URL-file filter
		var configContent =
			"""
			bundle:
			  profiles:
			    release:
			      products: "elasticsearch 9.2.0 ga"
			      output: "bundle.yaml"
			""";
		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var urlFile = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "prs.txt");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(urlFile)!);
		await FileSystem.File.WriteAllTextAsync(urlFile, "https://github.com/elastic/elasticsearch/pull/100\n", TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Config = configPath,
			Profile = "release",
			ProfileArgument = "9.2.0",
			ProfileReport = urlFile
		};

		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse("Should fail when profile has products pattern and a report is also provided");
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("products"),
			"Error should mention the products pattern conflict"
		);
	}

	// ─── Phase 4: --report option (option-based mode) ─────────────────────────────────

	[Fact]
	public async Task BundleChangelogs_WithReportOption_ParsesPromotionReportAndFilters()
	{
		// Arrange - option-based mode with --report pointing to an HTML-like file
		var htmlReportContent =
			"""
			<html><body>
			  <a href="https://github.com/elastic/elasticsearch/pull/100">PR #100</a>
			  <a href="https://github.com/elastic/elasticsearch/pull/200">PR #200</a>
			</body></html>
			""";
		var reportFile = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "report.html");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(reportFile)!);
		await FileSystem.File.WriteAllTextAsync(reportFile, htmlReportContent, TestContext.Current.CancellationToken);

		// language=yaml
		var changelog1 =
			"""
			title: Matched PR 100
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
			title: Unmatched PR 999
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/999
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-matched.yaml");
		var file2 = FileSystem.Path.Combine(_changelogDir, "1755268140-unmatched.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var outputPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(outputPath)!);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Report = reportFile,
			Output = outputPath
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("1755268130-matched.yaml");
		bundleContent.Should().NotContain("1755268140-unmatched.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithReportOption_FileNotFound_ReturnsError()
	{
		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Report = "/nonexistent/path/report.html"
		};

		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse("Should fail when report file does not exist");
		Collector.Errors.Should().BeGreaterThan(0);
	}

	// ─── Phase 4.2: --prs and --issues file URL validation ───────────────────────────

	[Fact]
	public async Task BundleChangelogs_WithPrsFile_ContainingNumbers_ReturnsError()
	{
		// Arrange - prs file contains bare numbers (not fully-qualified URLs)
		var prsFile = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "prs.txt");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(prsFile)!);
		await FileSystem.File.WriteAllTextAsync(prsFile, "100\n200\n", TestContext.Current.CancellationToken);

		var changelogFile = FileSystem.Path.Combine(_changelogDir, "1755268130-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile,
			"""
			title: Feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""", TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Prs = [prsFile],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse("Should fail when prs file contains bare numbers");
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("fully-qualified GitHub URLs"),
			"Error should mention fully-qualified URL requirement"
		);
	}

	[Fact]
	public async Task BundleChangelogs_WithIssuesFile_ContainingShortForms_ReturnsError()
	{
		// Arrange - issues file contains short forms (not fully-qualified URLs)
		var issuesFile = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "issues.txt");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(issuesFile)!);
		await FileSystem.File.WriteAllTextAsync(issuesFile, "elastic/elasticsearch#100\n", TestContext.Current.CancellationToken);

		var changelogFile = FileSystem.Path.Combine(_changelogDir, "1755268130-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile,
			"""
			title: Feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			issues:
			  - https://github.com/elastic/elasticsearch/issues/100
			""", TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Issues = [issuesFile],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse("Should fail when issues file contains short forms");
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("fully-qualified GitHub URLs"),
			"Error should mention fully-qualified URL requirement"
		);
	}

	[Fact]
	public async Task BundleChangelogs_WithPrsFile_ContainingValidUrls_FiltersCorrectly()
	{
		// Verify that a prs file with valid fully-qualified URLs still works correctly
		var prsFile = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "prs.txt");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(prsFile)!);
		await FileSystem.File.WriteAllTextAsync(
			prsFile,
			"https://github.com/elastic/elasticsearch/pull/100\n",
			TestContext.Current.CancellationToken
		);

		// language=yaml
		var changelog =
			"""
			title: Matched Feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";
		var file = FileSystem.Path.Combine(_changelogDir, "1755268130-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file, changelog, TestContext.Current.CancellationToken);

		var outputPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(outputPath)!);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Prs = [prsFile],
			Output = outputPath
		};

		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);
	}

	[Fact]
	public async Task BundleChangelogs_WithRulesBundleExclude_ExcludesMatchingProducts()
	{
		// Arrange
		// language=yaml
		var configContent =
			"""
			rules:
			  bundle:
			    exclude_products: cloud-hosted
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var elasticsearchChangelog =
			"""
			title: Elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";
		// language=yaml
		var cloudChangelog =
			"""
			title: Cloud feature
			type: feature
			products:
			  - product: cloud-hosted
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/cloud-hosted/pull/200
			""";

		var changelogDir = CreateChangelogDir();
		var file1 = FileSystem.Path.Combine(changelogDir, "1755268130-elasticsearch-feature.yaml");
		var file2 = FileSystem.Path.Combine(changelogDir, "1755268140-cloud-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, elasticsearchChangelog, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, cloudChangelog, TestContext.Current.CancellationToken);

		var outputPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(outputPath)!);

		var input = new BundleChangelogsArguments
		{
			Directory = changelogDir,
			All = true,
			Config = configPath,
			Output = outputPath
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("name: 1755268130-elasticsearch-feature.yaml");
		bundleContent.Should().NotContain("name: 1755268140-cloud-feature.yaml");
		// Verify warning was emitted for the excluded entry
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("[-bundle-exclude]"));
	}

	[Fact]
	public async Task BundleChangelogs_WithRulesBundleInclude_IncludesOnlyMatchingProducts()
	{
		// Arrange
		// language=yaml
		var configContent =
			"""
			rules:
			  bundle:
			    include_products: elasticsearch
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var elasticsearchChangelog =
			"""
			title: Elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/300
			""";
		// language=yaml
		var kibanaChangelog =
			"""
			title: Kibana feature
			type: feature
			products:
			  - product: kibana
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/kibana/pull/400
			""";

		var changelogDir = CreateChangelogDir();
		var file1 = FileSystem.Path.Combine(changelogDir, "1755268130-elasticsearch-feature.yaml");
		var file2 = FileSystem.Path.Combine(changelogDir, "1755268140-kibana-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, elasticsearchChangelog, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, kibanaChangelog, TestContext.Current.CancellationToken);

		var outputPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(outputPath)!);

		var input = new BundleChangelogsArguments
		{
			Directory = changelogDir,
			All = true,
			Config = configPath,
			Output = outputPath
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("name: 1755268130-elasticsearch-feature.yaml");
		bundleContent.Should().NotContain("name: 1755268140-kibana-feature.yaml");
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("[-bundle-include]"));
	}

	[Fact]
	public async Task BundleChangelogs_WithAllFilter_AppliesRulesBundle()
	{
		// Arrange - rules.bundle applies to --all primary filter too
		// language=yaml
		var configContent =
			"""
			rules:
			  bundle:
			    exclude_products: kibana
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var elasticsearchChangelog =
			"""
			title: Elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/500
			""";
		// language=yaml
		var kibanaChangelog =
			"""
			title: Kibana feature
			type: feature
			products:
			  - product: kibana
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/kibana/pull/600
			""";

		var changelogDir = CreateChangelogDir();
		var file1 = FileSystem.Path.Combine(changelogDir, "1755268130-elasticsearch-feature.yaml");
		var file2 = FileSystem.Path.Combine(changelogDir, "1755268140-kibana-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, elasticsearchChangelog, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, kibanaChangelog, TestContext.Current.CancellationToken);

		var outputPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(outputPath)!);

		var input = new BundleChangelogsArguments
		{
			Directory = changelogDir,
			All = true,
			Config = configPath,
			Output = outputPath
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("name: 1755268130-elasticsearch-feature.yaml");
		bundleContent.Should().NotContain("name: 1755268140-kibana-feature.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithGlobalExcludeProductsMatchConjunction_ExcludesOnlyWhenAllListedProductsOnEntry()
	{
		var configContent =
			"""
			rules:
			  bundle:
			    exclude_products:
			      - elasticsearch
			      - kibana
			    match_products: conjunction
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var kibanaOnly = """
			title: Kibana only
			type: feature
			products:
			  - product: kibana
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/kibana/pull/100
			""";
		var esAndKibana = """
			title: Elasticsearch and Kibana
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			  - product: kibana
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/kibana/pull/200
			""";
		var changelogDir = CreateChangelogDir();
		await FileSystem.File.WriteAllTextAsync(
			FileSystem.Path.Combine(changelogDir, "1755268001-kibana-only.yaml"),
			kibanaOnly,
			TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(
			FileSystem.Path.Combine(changelogDir, "1755268002-es-kibana.yaml"),
			esAndKibana,
			TestContext.Current.CancellationToken);

		var outputPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(outputPath)!);

		var input = new BundleChangelogsArguments
		{
			Directory = changelogDir,
			All = true,
			Config = configPath,
			Output = outputPath
		};

		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("1755268001-kibana-only.yaml");
		bundleContent.Should().NotContain("1755268002-es-kibana.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithGlobalIncludeProductsMatchConjunction_RequiresAllListedProductsOnEntry()
	{
		var configContent =
			"""
			rules:
			  bundle:
			    include_products:
			      - elasticsearch
			      - security
			    match_products: conjunction
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var esOnly = """
			title: ES only
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/400
			""";
		var esSec = """
			title: ES and security
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			  - product: security
			    target: 9.2.0
			prs:
			  - https://github.com/elastic/elasticsearch/pull/500
			""";

		var changelogDir = CreateChangelogDir();
		await FileSystem.File.WriteAllTextAsync(
			FileSystem.Path.Combine(changelogDir, "1755268011-es-only.yaml"),
			esOnly,
			TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(
			FileSystem.Path.Combine(changelogDir, "1755268012-es-sec.yaml"),
			esSec,
			TestContext.Current.CancellationToken);

		var outputPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(outputPath)!);

		var input = new BundleChangelogsArguments
		{
			Directory = changelogDir,
			All = true,
			Config = configPath,
			Output = outputPath
		};

		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("1755268012-es-sec.yaml");
		bundleContent.Should().NotContain("1755268011-es-only.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithInputProducts_AppliesBundleRules()
	{
		// Arrange - rules.bundle always applies regardless of input method
		// language=yaml
		var configContent =
			"""
			rules:
			  bundle:
			    exclude_products: elasticsearch
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var elasticsearchChangelog =
			"""
			title: Elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/700
			""";

		var changelogDir = CreateChangelogDir();
		var file1 = FileSystem.Path.Combine(changelogDir, "1755268130-elasticsearch-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, elasticsearchChangelog, TestContext.Current.CancellationToken);

		var outputPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(outputPath)!);

		// Use InputProducts as primary filter — rules.bundle.exclude_products should still apply
		var input = new BundleChangelogsArguments
		{
			Directory = changelogDir,
			InputProducts = [new ProductArgument { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Config = configPath,
			Output = outputPath
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert - elasticsearch entry is excluded by exclude_products rule even with InputProducts
		result.Should().BeFalse("Bundle should fail because all entries are excluded by rules.bundle");
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("[-bundle-exclude]") && d.Message.Contains("1755268130-elasticsearch-feature.yaml"));
		Collector.Errors.Should().BeGreaterThan(0, "Should have error about no entries remaining");
	}

	[Fact]
	public async Task BundleChangelogs_WithRulesBundleExcludeType_ExcludesMatchingType()
	{
		// Arrange
		// language=yaml
		var configContent =
			"""
			rules:
			  bundle:
			    exclude_types: enhancement
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var featureChangelog =
			"""
			title: Feature entry
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";
		// language=yaml
		var enhancementChangelog =
			"""
			title: Enhancement entry
			type: enhancement
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/200
			""";

		var changelogDir = CreateChangelogDir();
		var file1 = FileSystem.Path.Combine(changelogDir, "1755268130-feature.yaml");
		var file2 = FileSystem.Path.Combine(changelogDir, "1755268140-enhancement.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, featureChangelog, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, enhancementChangelog, TestContext.Current.CancellationToken);

		var outputPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(outputPath)!);

		var input = new BundleChangelogsArguments
		{
			Directory = changelogDir,
			All = true,
			Config = configPath,
			Output = outputPath
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("name: 1755268130-feature.yaml");
		bundleContent.Should().NotContain("name: 1755268140-enhancement.yaml");
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("[-bundle-type-area]"));
	}

	[Fact]
	public async Task BundleChangelogs_WithRulesBundleIncludeArea_ExcludesNonMatchingArea()
	{
		// Arrange
		// language=yaml
		var configContent =
			"""
			rules:
			  bundle:
			    include_areas: "Search"
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var searchChangelog =
			"""
			title: Search feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			areas:
			  - Search
			prs:
			  - https://github.com/elastic/elasticsearch/pull/300
			""";
		// language=yaml
		var internalChangelog =
			"""
			title: Internal fix
			type: bug-fix
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			areas:
			  - Internal
			prs:
			  - https://github.com/elastic/elasticsearch/pull/400
			""";

		var changelogDir = CreateChangelogDir();
		var file1 = FileSystem.Path.Combine(changelogDir, "1755268130-search-feature.yaml");
		var file2 = FileSystem.Path.Combine(changelogDir, "1755268140-internal-fix.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, searchChangelog, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, internalChangelog, TestContext.Current.CancellationToken);

		var outputPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(outputPath)!);

		var input = new BundleChangelogsArguments
		{
			Directory = changelogDir,
			All = true,
			Config = configPath,
			Output = outputPath
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("name: 1755268130-search-feature.yaml");
		bundleContent.Should().NotContain("name: 1755268140-internal-fix.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithRulesBundlePerProductOverride_AppliesProductSpecificFilter()
	{
		// Arrange — global rule excludes "enhancement", but cloud-serverless overrides to allow all types
		// language=yaml
		var configContent =
			"""
			rules:
			  bundle:
			    exclude_types: enhancement
			    products:
			      cloud-serverless:
			        include_areas: "Search"
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var esEnhancement =
			"""
			title: ES enhancement (excluded by global rule)
			type: enhancement
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/500
			""";
		// language=yaml
		var serverlessSearch =
			"""
			title: Serverless search feature
			type: feature
			products:
			  - product: cloud-serverless
			    target: 9.2.0
			    lifecycle: ga
			areas:
			  - Search
			prs:
			  - https://github.com/elastic/cloud-serverless/pull/600
			""";
		// language=yaml
		var serverlessOther =
			"""
			title: Serverless other feature (excluded by per-product include_areas)
			type: feature
			products:
			  - product: cloud-serverless
			    target: 9.2.0
			    lifecycle: ga
			areas:
			  - Internal
			prs:
			  - https://github.com/elastic/cloud-serverless/pull/700
			""";

		var changelogDir = CreateChangelogDir();
		var file1 = FileSystem.Path.Combine(changelogDir, "1755268130-es-enhancement.yaml");
		var file2 = FileSystem.Path.Combine(changelogDir, "1755268140-serverless-search.yaml");
		var file3 = FileSystem.Path.Combine(changelogDir, "1755268150-serverless-other.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, esEnhancement, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, serverlessSearch, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file3, serverlessOther, TestContext.Current.CancellationToken);

		var outputPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(outputPath)!);

		var input = new BundleChangelogsArguments
		{
			Directory = changelogDir,
			All = true,
			Config = configPath,
			Output = outputPath
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
		bundleContent.Should().NotContain("name: 1755268130-es-enhancement.yaml");
		bundleContent.Should().Contain("name: 1755268140-serverless-search.yaml");
		bundleContent.Should().NotContain("name: 1755268150-serverless-other.yaml");
	}

	// ── Multi-product rule resolution: intersection + alphabetical first-match ────────────────────────

	[Fact]
	public async Task BundleChangelogs_WithOutputProducts_SingleProductEntry_UsesMatchingProductRule()
	{
		// Arrange — output_products has two products; rule context = "kibana" (first alphabetically).
		// Only entries containing "kibana" product use kibana rules; others are disjoint → excluded.
		// kibana rule: exclude_types: docs
		// language=yaml
		var configContent =
			"""
			rules:
			  bundle:
			    products:
			      kibana:
			        exclude_types: docs
			      security:
			        match_areas: any
			        include_areas: "Detection rules and alerts"
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// kibana-docs entry: kibana rule says exclude docs → excluded
		// language=yaml
		var kibanaDoc =
			"""
			title: Kibana docs entry
			type: docs
			products:
			  - product: kibana
			    target: 9.3.0
			prs:
			  - "100"
			""";

		// security entry with the right area → security rule passes → included
		// language=yaml
		var securityEntry =
			"""
			title: Security detection feature
			type: feature
			products:
			  - product: security
			    target: 9.3.0
			areas:
			  - Detection rules and alerts
			prs:
			  - "200"
			""";

		// security entry with wrong area → security include_areas rule fails → excluded
		// language=yaml
		var securityOtherArea =
			"""
			title: Security unrelated feature
			type: feature
			products:
			  - product: security
			    target: 9.3.0
			areas:
			  - Unrelated area
			prs:
			  - "300"
			""";

		var changelogDir = CreateChangelogDir();
		var file1 = FileSystem.Path.Combine(changelogDir, "1755268130-kibana-doc.yaml");
		var file2 = FileSystem.Path.Combine(changelogDir, "1755268140-security-entry.yaml");
		var file3 = FileSystem.Path.Combine(changelogDir, "1755268150-security-other.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, kibanaDoc, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, securityEntry, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file3, securityOtherArea, TestContext.Current.CancellationToken);

		var outputPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(outputPath)!);

		var input = new BundleChangelogsArguments
		{
			Directory = changelogDir,
			All = true,
			Config = configPath,
			Output = outputPath,
			OutputProducts = [new ProductArgument { Product = "kibana", Target = "9.3.0" }, new ProductArgument { Product = "security", Target = "9.3.0" }]
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		// Rule context = "kibana" (first alphabetically from output products)
		// All security entries are disjoint from kibana context → excluded
		// Kibana entry uses kibana rules (exclude docs) → excluded  
		// Result: No entries remain → bundle should fail
		result.Should().BeFalse($"Expected bundle to fail when no entries remain. Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().BeGreaterThan(0, "Should have error when no entries remain after filtering");
	}

	[Fact]
	public async Task BundleChangelogs_WithOutputProducts_SharedProductEntry_UsesAlphabeticalFirstMatch()
	{
		// Arrange — entry belongs to both kibana and security (shared entry).
		// kibana rule: exclude_areas: "Detection rules and alerts" (alphabetically first → wins)
		// security rule: include_areas: "Detection rules and alerts"
		// Expected: kibana rule fires (k < s alphabetically) → shared entry excluded.
		// A second kibana entry with a different area is included so the bundle succeeds.
		// language=yaml
		var configContent =
			"""
			rules:
			  bundle:
			    products:
			      kibana:
			        match_areas: any
			        exclude_areas: "Detection rules and alerts"
			      security:
			        match_areas: any
			        include_areas: "Detection rules and alerts"
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var sharedEntry =
			"""
			title: Shared detection rule entry
			type: feature
			products:
			  - product: kibana
			    target: 9.3.0
			  - product: security
			    target: 9.3.0
			areas:
			  - Detection rules and alerts
			prs:
			  - "400"
			""";

		// language=yaml
		var kibanaOtherEntry =
			"""
			title: Kibana core feature
			type: feature
			products:
			  - product: kibana
			    target: 9.3.0
			areas:
			  - Core
			prs:
			  - "401"
			""";

		var changelogDir = CreateChangelogDir();
		var file1 = FileSystem.Path.Combine(changelogDir, "1755268160-shared.yaml");
		var file2 = FileSystem.Path.Combine(changelogDir, "1755268161-kibana-other.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, sharedEntry, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, kibanaOtherEntry, TestContext.Current.CancellationToken);

		var outputPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(outputPath)!);

		var input = new BundleChangelogsArguments
		{
			Directory = changelogDir,
			All = true,
			Config = configPath,
			Output = outputPath,
			OutputProducts = [new ProductArgument { Product = "kibana", Target = "9.3.0" }, new ProductArgument { Product = "security", Target = "9.3.0" }]
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert — kibana wins alphabetically; its exclude_areas rule fires for the shared entry
		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);
		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
		bundleContent.Should().NotContain("name: 1755268160-shared.yaml", "kibana rule (alphabetically first) should exclude the shared entry");
		bundleContent.Should().Contain("name: 1755268161-kibana-other.yaml", "kibana entry with a different area should pass the exclude_areas rule");
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("[-bundle-type-area]"));
	}

	[Fact]
	public async Task BundleChangelogs_WithoutOutputProducts_FallsBackToEntryProducts()
	{
		// Arrange — no output_products; fallback uses entry's own product list (alphabetical first-match).
		// Only kibana has a per-product rule; elasticsearch entry should use global blocker (none here).
		// language=yaml
		var configContent =
			"""
			rules:
			  bundle:
			    products:
			      kibana:
			        exclude_types: docs
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var kibanaDoc =
			"""
			title: Kibana docs entry
			type: docs
			products:
			  - product: kibana
			    target: 9.3.0
			prs:
			  - "500"
			""";

		// language=yaml
		var esDoc =
			"""
			title: Elasticsearch docs entry
			type: docs
			products:
			  - product: elasticsearch
			    target: 9.3.0
			prs:
			  - "600"
			""";

		var changelogDir = CreateChangelogDir();
		var file1 = FileSystem.Path.Combine(changelogDir, "1755268170-kibana-doc.yaml");
		var file2 = FileSystem.Path.Combine(changelogDir, "1755268180-es-doc.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, kibanaDoc, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, esDoc, TestContext.Current.CancellationToken);

		var outputPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(outputPath)!);

		var input = new BundleChangelogsArguments
		{
			Directory = changelogDir,
			All = true,
			Config = configPath,
			Output = outputPath
			// no OutputProducts set — fallback to entry products
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
		bundleContent.Should().NotContain("name: 1755268170-kibana-doc.yaml", "kibana docs should be excluded by its per-product rule");
		bundleContent.Should().Contain("name: 1755268180-es-doc.yaml", "elasticsearch docs entry has no per-product rule and no global rule, so it is included");
	}

	[Fact]
	public async Task BundleChangelogs_WithOutputProducts_EntryNotInContext_FallsBackToEntryProducts()
	{
		// Arrange — output_products is [kibana]; entry belongs to [elasticsearch] only (disjoint).
		// Intersection is empty → fall back to entry's own products; elasticsearch has no rule → global blocker (none) → included.
		// language=yaml
		var configContent =
			"""
			rules:
			  bundle:
			    products:
			      kibana:
			        exclude_types: feature
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var esFeature =
			"""
			title: Elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.3.0
			prs:
			  - "700"
			""";

		var changelogDir = CreateChangelogDir();
		var file1 = FileSystem.Path.Combine(changelogDir, "1755268190-es-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, esFeature, TestContext.Current.CancellationToken);

		var outputPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(outputPath)!);

		var input = new BundleChangelogsArguments
		{
			Directory = changelogDir,
			All = true,
			Config = configPath,
			Output = outputPath,
			OutputProducts = [new ProductArgument { Product = "kibana", Target = "9.3.0" }]
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert — disjoint entry excluded entirely (new single-product rule resolution behavior)
		result.Should().BeFalse($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(1, "system reports error when no entries remain after filtering");

		var errorMessages = string.Join("; ", Collector.Diagnostics.Select(d => d.Message));
		errorMessages.Should().Contain("disjoint from rule context 'kibana'", "elasticsearch entry should be excluded as disjoint from kibana context");
		errorMessages.Should().Contain("No changelog entries remained", "system should report empty bundle error");
	}

	[Fact]
	public async Task BundleChangelogs_WithPerProductIncludeProducts_IncludesOnlyContextMatchingProducts()
	{
		// language=yaml
		var configContent =
			"""
			rules:
			  bundle:
			    include_products:
			      - elasticsearch
			      - kibana
			    products:
			      security:
			        include_products:
			          - security
			          - kibana
			        match_products: any
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// Create test entries
		var changelogDir = CreateChangelogDir();
		await CreateTestEntry(changelogDir, "kibana-feature.yaml", "Kibana feature", "kibana");
		await CreateTestEntry(changelogDir, "security-feature.yaml", "Security feature", "security");
		await CreateTestEntry(changelogDir, "elasticsearch-feature.yaml", "Elasticsearch feature", "elasticsearch");

		var outputPath = CreateTempFilePath("bundle.yaml");

		var input = new BundleChangelogsArguments
		{
			Directory = changelogDir,
			All = true,
			Config = configPath,
			Output = outputPath,
			OutputProducts = [new ProductArgument { Product = "security", Target = "9.3.0" }]
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert - single-product rule resolution: only security changelog matches bundle context
		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);

		// Only security changelog should be included (it matches the bundle context "security")
		bundleContent.Should().Contain("security-feature.yaml", "security entry matches bundle context and should be included by security-specific rules");

		// Disjoint changelogs are excluded entirely (not included via global fallback)
		bundleContent.Should().NotContain("kibana-feature.yaml", "kibana entry is disjoint from security context and should be excluded");
		bundleContent.Should().NotContain("elasticsearch-feature.yaml", "elasticsearch entry is disjoint from security context and should be excluded");
	}

	[Fact]
	public async Task BundleChangelogs_WithPerProductExcludeProducts_ExcludesContextMatchingProducts()
	{
		// language=yaml
		var configContent =
			"""
			rules:
			  bundle:
			    products:
			      security:
			        exclude_products:
			          - kibana
			        match_products: any
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// Create test entries
		var changelogDir = CreateChangelogDir();
		await CreateTestEntry(changelogDir, "kibana-feature.yaml", "Kibana feature", "kibana");
		await CreateTestEntry(changelogDir, "security-feature.yaml", "Security feature", "security");
		await CreateTestEntry(changelogDir, "elasticsearch-feature.yaml", "Elasticsearch feature", "elasticsearch");

		// Create multi-product entry that should be excluded by security context rule
		var multiProductContent = """
			title: Security+Kibana feature
			type: feature
			products:
			  - product: security
			    target: 9.3.0
			  - product: kibana
			    target: 9.3.0
			prs:
			  - "123"
			""";
		var multiProductFile = FileSystem.Path.Combine(changelogDir, "security-kibana-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(multiProductFile, multiProductContent, TestContext.Current.CancellationToken);

		var outputPath = CreateTempFilePath("bundle.yaml");

		var input = new BundleChangelogsArguments
		{
			Directory = changelogDir,
			All = true,
			Config = configPath,
			Output = outputPath,
			OutputProducts = [new ProductArgument { Product = "security", Target = "9.3.0" }]
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert - single-product rule resolution: disjoint entries are excluded entirely
		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);

		// Rule context = "security" (from output products)
		// Disjoint entries (kibana, elasticsearch) are excluded entirely
		bundleContent.Should().NotContain("kibana-feature.yaml", "kibana entry is disjoint from security context");
		bundleContent.Should().NotContain("elasticsearch-feature.yaml", "elasticsearch entry is disjoint from security context");

		// security entry (matches security context) uses context rule: exclude_products=[kibana] (security not excluded) → INCLUDED
		bundleContent.Should().Contain("security-feature.yaml", "security entry should be included (not in context exclude list)");

		// Multi-product entry (security + kibana) matches security context and gets excluded by exclude_products=[kibana] → EXCLUDED
		bundleContent.Should().NotContain("security-kibana-feature.yaml", "security+kibana entry should be excluded by security context rule");
	}

	[Fact]
	public async Task BundleChangelogs_WithPerProductRules_FallsBackToGlobalWhenNoContextRule()
	{
		// language=yaml
		var configContent =
			"""
			rules:
			  bundle:
			    include_products:
			      - elasticsearch
			      - security
			    products:
			      cloud-hosted:
			        include_products:
			          - security
			          - kibana
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// Create test entries
		var changelogDir = CreateChangelogDir();
		await CreateTestEntry(changelogDir, "kibana-feature.yaml", "Kibana feature", "kibana");
		await CreateTestEntry(changelogDir, "security-feature.yaml", "Security feature", "security");
		await CreateTestEntry(changelogDir, "elasticsearch-feature.yaml", "Elasticsearch feature", "elasticsearch");

		var outputPath = CreateTempFilePath("bundle.yaml");

		var input = new BundleChangelogsArguments
		{
			Directory = changelogDir,
			All = true,
			Config = configPath,
			Output = outputPath,
			OutputProducts = [new ProductArgument { Product = "security", Target = "9.3.0" }] // No context rule for security, should fall back to global
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		// Rule context = "security" (from output products)
		// No per-product rule for "security" → falls back to global rules
		// elasticsearch and kibana entries are disjoint from security context → excluded
		// Only security entry remains and uses global rules (include elasticsearch, security) → included
		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("security-feature.yaml", "security entry should be included by global rule");
		// Disjoint entries are excluded entirely in single-product rule resolution
		bundleContent.Should().NotContain("elasticsearch-feature.yaml", "elasticsearch entry is disjoint from security context");
		bundleContent.Should().NotContain("kibana-feature.yaml", "kibana entry is disjoint from security context");
	}

	[Fact]
	public async Task BundleChangelogs_WithPerProductRules_ContextRulesTakePrecedenceOverGlobal()
	{
		// language=yaml
		var configContent =
			"""
			rules:
			  bundle:
			    include_products:
			      - elasticsearch
			    products:
			      security:
			        include_products:
			          - security
			          - kibana
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// Create test entries
		var changelogDir = CreateChangelogDir();
		await CreateTestEntry(changelogDir, "kibana-feature.yaml", "Kibana feature", "kibana");
		await CreateTestEntry(changelogDir, "security-feature.yaml", "Security feature", "security");
		await CreateTestEntry(changelogDir, "elasticsearch-feature.yaml", "Elasticsearch feature", "elasticsearch");

		var outputPath = CreateTempFilePath("bundle.yaml");

		var input = new BundleChangelogsArguments
		{
			Directory = changelogDir,
			InputProducts = [new ProductArgument { Product = "*" }], // Input method should not affect bundle filtering
			Config = configPath,
			Output = outputPath,
			OutputProducts = [new ProductArgument { Product = "security", Target = "9.3.0" }]
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert - single-product rule resolution: disjoint entries are excluded entirely
		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);

		// Rule context = "security" (from output products)
		// Disjoint entries (kibana, elasticsearch) are excluded entirely
		bundleContent.Should().NotContain("kibana-feature.yaml", "kibana entry is disjoint from security context");
		bundleContent.Should().NotContain("elasticsearch-feature.yaml", "elasticsearch entry is disjoint from security context");

		// security entry (matches security context) uses context rule: include_products=[security, kibana] → INCLUDED
		bundleContent.Should().Contain("security-feature.yaml", "security entry should be included by context rule");
	}

	private async Task CreateTestEntry(string changelogDir, string filename, string title, string product)
	{
		var content = $"""
			title: {title}
			type: feature
			products:
			  - product: {product}
			    target: 9.3.0
			prs:
			  - "123"
			""";
		var filePath = FileSystem.Path.Combine(changelogDir, filename);
		await FileSystem.File.WriteAllTextAsync(filePath, content, TestContext.Current.CancellationToken);
	}

	private string CreateTempFilePath(string filename)
	{
		var outputPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), filename);
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(outputPath)!);
		return outputPath;
	}

	private static string ExtractChecksum(string bundleContent)
	{
		var lines = bundleContent.Split('\n');
		var checksumLine = lines.FirstOrDefault(l => l.Contains("checksum:"));
		checksumLine.Should().NotBeNull("Bundle should contain a checksum line");
		return checksumLine.Split("checksum:")[1].Trim();
	}

	[Fact]
	public async Task BundleChangelogs_WithNoProductsField_FallsBackToGlobalRules()
	{
		// Arrange — global-only rules.bundle (Mode 2): entries with no products get a warning; product filters are skipped;
		// type/area blocker still applies.
		// language=yaml
		var configContent =
			"""
			rules:
			  bundle:
			    exclude_types:
			      - "docs"
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var noProductsEntry =
			"""
			title: Entry with no products field
			type: docs
			prs:
			  - "500"
			""";

		var changelogDir = CreateChangelogDir();
		var file1 = FileSystem.Path.Combine(changelogDir, "1755268200-no-products.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, noProductsEntry, TestContext.Current.CancellationToken);

		var outputPath = CreateTempFilePath("no-products-bundle.yaml");

		var input = new BundleChangelogsArguments
		{
			All = true,
			Directory = changelogDir,
			Config = configPath,
			Output = outputPath
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert — docs entry excluded by global type filter; bundle fails with no entries remained
		result.Should().BeFalse("bundling should fail when all entries are filtered out");

		Collector.Errors.Should().Be(1, "no entries remained after rules.bundle filter");
		var errorMessages = string.Join("; ", Collector.Diagnostics.Select(d => d.Message));
		errorMessages.Should().Contain("No changelog entries remained after applying rules.bundle filter");
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("[-bundle-global]") && d.Message.Contains("no products"));
	}

	[Fact]
	public async Task BundleChangelogs_GlobalMode_IncludeProductsAny_IncludesEntryMatchingAnyListedProduct()
	{
		// Mode 2 — global rules only: match_products: any with include_products lists means OR over changelog products.
		// language=yaml
		var configContent =
			"""
			rules:
			  bundle:
			    match_products: any
			    include_products:
			      - kibana
			      - elasticsearch
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var esOnly =
			"""
			title: ES only
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/1
			""";
		// language=yaml
		var kibanaOnly =
			"""
			title: Kibana only
			type: feature
			products:
			  - product: kibana
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/kibana/pull/2
			""";

		var changelogDir = CreateChangelogDir();
		var file1 = FileSystem.Path.Combine(changelogDir, "1755268205-es.yaml");
		var file2 = FileSystem.Path.Combine(changelogDir, "1755268206-kibana.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, esOnly, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, kibanaOnly, TestContext.Current.CancellationToken);

		var outputPath = CreateTempFilePath("global-or-bundle.yaml");
		var input = new BundleChangelogsArguments
		{
			All = true,
			Directory = changelogDir,
			Config = configPath,
			Output = outputPath
		};

		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);
		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("1755268205-es.yaml");
		bundleContent.Should().Contain("1755268206-kibana.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_GlobalMode_EmptyProducts_IncludesFeatureEntryWithWarning()
	{
		// Mode 2 — missing/empty changelog products: include with warning; product include/exclude lists are skipped for that entry.
		// language=yaml
		var configContent =
			"""
			rules:
			  bundle:
			    include_products:
			      - elasticsearch
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var noProductsEntry =
			"""
			title: No products
			type: feature
			prs:
			  - "901"
			""";

		var changelogDir = CreateChangelogDir();
		var file1 = FileSystem.Path.Combine(changelogDir, "1755268207-no-products-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, noProductsEntry, TestContext.Current.CancellationToken);

		var outputPath = CreateTempFilePath("global-empty-products.yaml");
		var input = new BundleChangelogsArguments
		{
			All = true,
			Directory = changelogDir,
			Config = configPath,
			Output = outputPath
		};

		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);
		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("1755268207-no-products-feature.yaml");
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("[-bundle-global]") && d.Message.Contains("no products"));
	}

	[Fact]
	public async Task BundleChangelogs_WithEmptyProductsYamlMap_UsesGlobalRulesWhenGlobalFiltersPresent()
	{
		// rules.bundle.products: {} — no per-product rules; same as omitting products (Mode 2 when global filters exist).
		// language=yaml
		var configContent =
			"""
			rules:
			  bundle:
			    products: {}
			    exclude_products: kibana
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var es =
			"""
			title: ES
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/1
			""";
		var kibana =
			"""
			title: Kibana
			type: feature
			products:
			  - product: kibana
			    target: 9.2.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/kibana/pull/2
			""";

		var changelogDir = CreateChangelogDir();
		await FileSystem.File.WriteAllTextAsync(FileSystem.Path.Combine(changelogDir, "1755268208-es.yaml"), es, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(FileSystem.Path.Combine(changelogDir, "1755268209-kibana.yaml"), kibana, TestContext.Current.CancellationToken);

		var outputPath = CreateTempFilePath("empty-products-map-bundle.yaml");
		var input = new BundleChangelogsArguments
		{
			All = true,
			Directory = changelogDir,
			Config = configPath,
			Output = outputPath
		};

		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);
		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("1755268208-es.yaml");
		bundleContent.Should().NotContain("1755268209-kibana.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithEmptyProductsList_FallsBackToGlobalRules()
	{
		// Arrange - changelog with empty products list should use global rules only
		// language=yaml
		var configContent =
			"""
			rules:
			  bundle:
			    exclude_types:
			      - "feature"
			    products:
			      kibana:
			        include_types:
			          - "feature"
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var emptyProductsEntry =
			"""
			title: Entry with empty products list
			type: feature
			products: []
			prs:
			  - "501"
			""";

		var changelogDir = CreateChangelogDir();
		var file1 = FileSystem.Path.Combine(changelogDir, "1755268201-empty-products.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, emptyProductsEntry, TestContext.Current.CancellationToken);

		var outputPath = CreateTempFilePath("empty-products-bundle.yaml");

		var input = new BundleChangelogsArguments
		{
			All = true,
			Directory = changelogDir,
			Config = configPath,
			Output = outputPath
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert - entry excluded by global type rule (empty products list)
		// Since all entries are filtered out, the bundling should fail
		result.Should().BeFalse("bundling should fail when all entries are filtered out");

		// With single-product rule resolution: bundle has no product context (early validation) + no entries remained
		Collector.Errors.Should().Be(2, "bundle validation error + no entries remained error");
		// No warnings expected since early validation prevents processing individual entries

		var errorMessages = string.Join("; ", Collector.Diagnostics.Select(d => d.Message));
		errorMessages.Should().Contain("Bundle has no product context", "bundle validation should report lack of product context");
		errorMessages.Should().Contain("No changelog entries remained after applying rules.bundle filter", "system should report empty bundle error");
	}

	[Fact]
	public async Task BundleChangelogs_WithMultipleProducts_UnifiedProductFiltering_AlphabeticalFirstMatch()
	{
		// Arrange - entry belongs to both kibana and security; test product filtering uses same resolution as type/area
		// kibana rule: exclude_products: [security] (alphabetically first → wins)
		// security rule: include_products: [security]
		// Expected: kibana rule fires (k < s alphabetically) → entry excluded by product filter
		// language=yaml
		var configContent =
			"""
			rules:
			  bundle:
			    products:
			      kibana:
			        exclude_products:
			          - "security"
			      security:
			        include_products:
			          - "security"
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var multiProductEntry =
			"""
			title: Multi-product entry for unified filtering test
			type: feature
			products:
			  - product: kibana
			    target: 9.3.0
			  - product: security
			    target: 9.3.0
			prs:
			  - "502"
			""";

		var changelogDir = CreateChangelogDir();
		var file1 = FileSystem.Path.Combine(changelogDir, "1755268202-multi-product.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, multiProductEntry, TestContext.Current.CancellationToken);

		var outputPath = CreateTempFilePath("unified-product-filtering-bundle.yaml");

		var input = new BundleChangelogsArguments
		{
			All = true,
			Directory = changelogDir,
			Config = configPath,
			Output = outputPath,
			OutputProducts = [new ProductArgument { Product = "kibana", Target = "9.3.0" }, new ProductArgument { Product = "security", Target = "9.3.0" }]
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert - entry excluded by product filter (kibana rule wins alphabetically)
		// Since all entries are filtered out, the bundling should fail
		result.Should().BeFalse("bundling should fail when all entries are filtered out");

		// When all entries are filtered out, the system reports this as an error
		Collector.Errors.Should().Be(1, "system reports error when no entries remain after filtering");
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("No changelog entries remained after applying rules.bundle filter"));

		// Should be excluded by kibana product rule (alphabetical first-match)
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("[-bundle-context-exclude]") && d.Message.Contains("multi-product"));
	}

	[Fact]
	public async Task BundleChangelogs_DisjointBundleContext_ProductFilteringFollowsSameLogicAsTypeArea()
	{
		// Arrange - bundle context [kibana]; entry products [elasticsearch] (disjoint)
		// Should use global rules, NOT elasticsearch-specific rules
		// This tests that product filtering uses same disjoint fallback as type/area
		// language=yaml
		var configContent =
			"""
			rules:
			  bundle:
			    include_products:
			      - "elasticsearch"
			      - "kibana"
			    products:
			      kibana:
			        exclude_products:
			          - "elasticsearch"  # This should NOT apply to disjoint entry
			      elasticsearch:
			        exclude_products:
			          - "elasticsearch"  # This should NOT apply to disjoint entry (would exclude if used)
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var elasticsearchEntry =
			"""
			title: Elasticsearch entry disjoint from kibana context
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.3.0
			prs:
			  - "503"
			""";

		var changelogDir = CreateChangelogDir();
		var file1 = FileSystem.Path.Combine(changelogDir, "1755268203-elasticsearch-disjoint.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, elasticsearchEntry, TestContext.Current.CancellationToken);

		var outputPath = CreateTempFilePath("disjoint-context-bundle.yaml");

		var input = new BundleChangelogsArguments
		{
			All = true,
			Directory = changelogDir,
			Config = configPath,
			Output = outputPath,
			OutputProducts = [new ProductArgument { Product = "kibana", Target = "9.3.0" }]
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert - disjoint entry excluded entirely (new single-product rule resolution behavior)
		// The entry has products=[elasticsearch] but bundle context is kibana, so it's disjoint and excluded
		result.Should().BeFalse($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(1, "system reports error when no entries remain after filtering");

		var errorMessages = string.Join("; ", Collector.Diagnostics.Select(d => d.Message));
		errorMessages.Should().Contain("disjoint from rule context 'kibana'", "disjoint entry should be excluded with informative message");
		errorMessages.Should().Contain("No changelog entries remained", "system should report empty bundle error");
	}

	[Fact]
	public async Task BundleChangelogs_MultiProductDisjoint_UsesGlobalRules()
	{
		// Arrange - bundle context [security]; entry products [kibana, elasticsearch] (both disjoint)
		// Should use global rules, NOT elasticsearch-specific rules (even though elasticsearch is alphabetically first)
		// This tests the key fix: multi-product disjoint entries don't get unrelated product-specific rules
		// language=yaml
		var configContent =
			"""
			rules:
			  bundle:
			    include_products:
			      - "security"
			      - "kibana"
			      - "elasticsearch"
			    products:
			      security:
			        exclude_products:
			          - "cloud-hosted"  # This should NOT apply (security not in entry products)
			      elasticsearch:
			        exclude_products:
			          - "kibana"
			          - "elasticsearch"  # This would exclude the entry if applied, but should NOT apply
			      kibana:
			        exclude_products:
			          - "cloud-serverless"  # This should NOT apply either
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var multiProductEntry =
			"""
			title: Multi-product entry disjoint from security context
			type: feature
			products:
			  - product: kibana
			    target: 9.3.0
			  - product: elasticsearch
			    target: 9.3.0
			prs:
			  - "504"
			""";

		var changelogDir = CreateChangelogDir();
		var file1 = FileSystem.Path.Combine(changelogDir, "1755268204-multiproduct-disjoint.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, multiProductEntry, TestContext.Current.CancellationToken);

		var outputPath = CreateTempFilePath("multiproduct-disjoint-bundle.yaml");

		var input = new BundleChangelogsArguments
		{
			All = true,
			Directory = changelogDir,
			Config = configPath,
			Output = outputPath,
			OutputProducts = [new ProductArgument { Product = "security", Target = "9.3.0" }]
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert - multi-product disjoint entry excluded entirely (new single-product rule resolution behavior)
		// The entry has products=[kibana, elasticsearch] but bundle context is security, so it's disjoint and excluded
		result.Should().BeFalse($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(1, "system reports error when no entries remain after filtering");

		var errorMessages = string.Join("; ", Collector.Diagnostics.Select(d => d.Message));
		errorMessages.Should().Contain("disjoint from rule context 'security'", "disjoint entry should be excluded with informative message");
		errorMessages.Should().Contain("No changelog entries remained", "system should report empty bundle error");
	}

	[Fact]
	public async Task BundleChangelogs_BundleAll_DisjointUsesOwnProductRules()
	{
		// Arrange - bundling ALL changelogs (no OutputProducts specified)
		// Single-product rule resolution: rule context = first alphabetically from all entry products = "elasticsearch"
		// Only entries containing "elasticsearch" use elasticsearch rules; others are disjoint → excluded
		// language=yaml
		var configContent =
			"""
			rules:
			  bundle:
			    include_products:
			      - "security"
			      - "kibana"
			      - "elasticsearch"
			    products:
			      elasticsearch:
			        exclude_products:
			          - "elasticsearch"  # This SHOULD apply (excludes elasticsearch entries)
			      kibana:
			        exclude_products:
			          - "kibana"  # This SHOULD apply (excludes kibana entries)
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// Single-product elasticsearch entry - should be excluded by elasticsearch rule
		// language=yaml
		var elasticsearchEntry =
			"""
			title: Single-product elasticsearch entry
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.3.0
			prs:
			  - "505"
			""";

		// Multi-product entry - should be excluded by elasticsearch rule (alphabetically first)
		// language=yaml  
		var multiProductEntry =
			"""
			title: Multi-product entry with elasticsearch
			type: feature
			products:
			  - product: kibana
			    target: 9.3.0
			  - product: elasticsearch
			    target: 9.3.0
			prs:
			  - "506"
			""";

		// Security entry - should be included (no security-specific exclude rule)
		// language=yaml
		var securityEntry =
			"""
			title: Security entry
			type: feature
			products:
			  - product: security
			    target: 9.3.0
			prs:
			  - "507"
			""";

		var changelogDir = CreateChangelogDir();
		var file1 = FileSystem.Path.Combine(changelogDir, "1755268205-elasticsearch-single.yaml");
		var file2 = FileSystem.Path.Combine(changelogDir, "1755268206-multiproduct-elasticsearch.yaml");
		var file3 = FileSystem.Path.Combine(changelogDir, "1755268207-security-included.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, elasticsearchEntry, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, multiProductEntry, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file3, securityEntry, TestContext.Current.CancellationToken);

		var outputPath = CreateTempFilePath("bundle-all-product-rules.yaml");

		var input = new BundleChangelogsArguments
		{
			All = true,
			Directory = changelogDir,
			Config = configPath,
			Output = outputPath
			// No OutputProducts specified - this is the key difference
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert - rule context = "elasticsearch" (first alphabetically from aggregated products)
		// Security entry is disjoint from elasticsearch context → excluded
		// All elasticsearch entries are excluded by elasticsearch rule → no entries remain → bundle fails  
		result.Should().BeFalse($"Expected bundle to fail when no entries remain. Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().BeGreaterThan(0, "Should have error when no entries remain after filtering");
	}

	[Fact]
	public async Task BundleChangelogs_PartialPerProductRules_AllOrNothingReplacement()
	{
		// Arrange - kibana rule has product filters but no type/area filters
		// With all-or-nothing replacement, entry uses kibana rule entirely (global type rules ignored)
		// language=yaml
		var configContent =
			"""
			rules:
			  bundle:
			    exclude_types:
			      - "docs"  # global type rule - will be ignored for kibana entries
			    products:
			      kibana:
			        include_products:
			          - "kibana"
			        # No type/area rules here - global type exclusions are NOT inherited
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// language=yaml
		var docsEntry =
			"""
			title: Docs entry with partial per-product rule
			type: docs
			products:
			  - product: kibana
			    target: 9.3.0
			prs:
			  - "504"
			""";

		var changelogDir = CreateChangelogDir();
		var file1 = FileSystem.Path.Combine(changelogDir, "1755268204-partial-rule.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, docsEntry, TestContext.Current.CancellationToken);

		var outputPath = CreateTempFilePath("partial-rule-bundle.yaml");

		var input = new BundleChangelogsArguments
		{
			All = true,
			Directory = changelogDir,
			Config = configPath,
			Output = outputPath
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert - entry included because kibana rule completely replaces global rules
		// Global exclude_types is ignored when per-product rule applies (all-or-nothing replacement)
		result.Should().BeTrue("bundling should succeed - kibana rule allows the entry");

		Collector.Errors.Should().Be(0, "no errors expected when entry is included");

		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("1755268204-partial-rule.yaml", "entry should be included - per-product rule ignores global type exclusions");
	}
}
