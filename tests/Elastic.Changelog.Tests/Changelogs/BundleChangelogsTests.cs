// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Bundling;
using Elastic.Documentation.Diagnostics;
using FluentAssertions;

namespace Elastic.Changelog.Tests.Changelogs;

public class BundleChangelogsTests : ChangelogTestBase
{
	private ChangelogBundlingService Service { get; }
	private readonly string _changelogDir;

	public BundleChangelogsTests(ITestOutputHelper output) : base(output)
	{
		Service = new(LoggerFactory, null, FileSystem);
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
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";
		// language=yaml
		var changelog2 =
			"""
			title: Second changelog
			type: enhancement
			products:
			  - product: kibana
			    target: 9.2.0
			pr: https://github.com/elastic/kibana/pull/200
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
			pr: https://github.com/elastic/elasticsearch/pull/100
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
			pr: https://github.com/elastic/kibana/pull/200
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
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";
		// language=yaml
		var changelog2 =
			"""
			title: Second PR
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/200
			""";
		// language=yaml
		var changelog3 =
			"""
			title: Third PR
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/300
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
			pr: https://github.com/elastic/elasticsearch/pull/100
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
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";
		// language=yaml
		var changelog2 =
			"""
			title: Second PR
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/200
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
			pr: https://github.com/elastic/elasticsearch/pull/100
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
			pr: https://github.com/elastic/elasticsearch/pull/133609
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
			pr: https://github.com/elastic/elasticsearch/pull/100
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
			pr: https://github.com/elastic/kibana/pull/200
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
			pr: https://github.com/elastic/cloud-serverless/pull/100
			""";
		// language=yaml
		var changelog2 =
			"""
			title: Cloud serverless feature 2
			type: feature
			products:
			  - product: cloud-serverless
			    target: 2025-12-06
			pr: https://github.com/elastic/cloud-serverless/pull/200
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
			pr: https://github.com/elastic/elasticsearch/pull/100
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
			pr: https://github.com/elastic/kibana/pull/200
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
			pr: https://github.com/elastic/elasticsearch/pull/100
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
			pr: https://github.com/elastic/kibana/pull/200
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
			pr: https://github.com/elastic/elasticsearch/pull/100
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
			pr: https://github.com/elastic/elasticsearch/pull/200
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
			pr: https://github.com/elastic/elasticsearch/pull/300
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
			pr: https://github.com/elastic/elasticsearch/pull/123
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
			pr: https://github.com/elastic/elasticsearch/pull/123
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
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";
		// language=yaml
		var changelog2 =
			"""
			title: Kibana feature
			type: feature
			products:
			  - product: kibana
			    target: 9.2.0
			pr: https://github.com/elastic/kibana/pull/200
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
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";
		// language=yaml
		var changelog2 =
			"""
			title: Kibana feature
			type: feature
			products:
			  - product: kibana
			    target: 9.2.0
			pr: https://github.com/elastic/kibana/pull/200
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
			pr: https://github.com/elastic/elasticsearch/pull/300
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
			pr: https://github.com/elastic/elasticsearch/pull/100
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
			pr: https://github.com/elastic/elasticsearch/pull/200
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
			pr: https://github.com/elastic/elasticsearch/pull/100
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
			pr: https://github.com/elastic/elasticsearch/pull/100
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
			pr: https://github.com/elastic/elasticsearch/pull/200
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
			pr: https://github.com/elastic/elasticsearch/pull/100
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
			pr: https://github.com/elastic/elasticsearch/pull/100
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
			pr: https://github.com/elastic/elasticsearch/pull/200
			""";
		// language=yaml
		var changelog3 =
			"""
			title: Elasticsearch feature without lifecycle
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/300
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
			pr: https://github.com/elastic/elasticsearch/pull/100
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
		bundleContent.Should().Contain("pr: https://github.com/elastic/elasticsearch/pull/100");
		bundleContent.Should().Contain("areas:");
		bundleContent.Should().Contain("- Search");
		bundleContent.Should().Contain("description: This is a test feature");
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
			pr: https://github.com/elastic/elasticsearch/pull/100
			description: |
			  This feature includes special characters:
			  - Ampersand: & symbol
			  - Asterisk: * symbol
			  - Other special chars: < > " ' / \
			  - Unicode: © ® ™ € £ ¥
			""";

		var file1 = FileSystem.Path.Combine(_changelogDir, "1755268130-special-chars.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, System.Text.Encoding.UTF8, TestContext.Current.CancellationToken);

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
		var bundleContent = await FileSystem.File.ReadAllTextAsync(input.Output, System.Text.Encoding.UTF8, TestContext.Current.CancellationToken);

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
			pr: https://github.com/elastic/elasticsearch/pull/100
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
}
