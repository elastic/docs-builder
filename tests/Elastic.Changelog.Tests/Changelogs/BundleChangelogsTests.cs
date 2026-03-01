// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Diagnostics;
using FluentAssertions;

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
	public async Task BundleChangelogs_WithProfileAndCliHideFeatures_MergesBothSources()
	{
		// Arrange - Test that CLI --hide-features and profile hide_features are merged

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
			OutputDirectory = outputDir,
			HideFeatures = ["feature:from-cli"]
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

		// Verify that hide-features from BOTH sources are present
		bundleContent.Should().Contain("hide-features:");
		bundleContent.Should().Contain("- feature:from-profile");
		bundleContent.Should().Contain("- feature:from-cli");
	}

	[Fact]
	public async Task BundleChangelogs_WithProfileAndCliHideFeatures_DeduplicatesFeatureIds()
	{
		// Arrange - Test that duplicate feature IDs from CLI and profile are deduplicated

		// language=yaml
		var configContent =
			"""
			bundle:
			  profiles:
			    es-release:
			      products: "elasticsearch {version} {lifecycle}"
			      output: "elasticsearch-{version}.yaml"
			      hide_features:
			        - feature:shared
			        - feature:profile-only
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
			OutputDirectory = outputDir,
			HideFeatures = ["feature:shared", "feature:cli-only"] // "feature:shared" overlaps with profile
		};

		// Act
		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Expected bundling to succeed, but got errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var outputFiles = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		outputFiles.Should().NotBeEmpty("Expected an output file to be created");
		var bundleContent = await FileSystem.File.ReadAllTextAsync(outputFiles[0], TestContext.Current.CancellationToken);

		// Verify all unique features are present
		bundleContent.Should().Contain("- feature:shared");
		bundleContent.Should().Contain("- feature:profile-only");
		bundleContent.Should().Contain("- feature:cli-only");

		// Count occurrences of "feature:shared" - should appear exactly once (deduplicated)
		var sharedCount = bundleContent.Split("feature:shared").Length - 1;
		sharedCount.Should().Be(1, "Duplicate feature IDs should be deduplicated");
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

	private static string ExtractChecksum(string bundleContent)
	{
		var lines = bundleContent.Split('\n');
		var checksumLine = lines.FirstOrDefault(l => l.Contains("checksum:"));
		checksumLine.Should().NotBeNull("Bundle should contain a checksum line");
		return checksumLine!.Split("checksum:")[1].Trim();
	}
}
