// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services.Changelog;
using FluentAssertions;

namespace Elastic.Documentation.Services.Tests.Changelogs;

public class BundleChangelogsTests : ChangelogTestBase
{
	private ChangelogService Service { get; }
	private readonly string _changelogDir;

	public BundleChangelogsTests(ITestOutputHelper output) : base(output)
	{
		Service = new(_loggerFactory, _configurationContext, null, _fileSystem);
		_changelogDir = CreateChangelogDir();
	}

	private string CreateChangelogDir()
	{
		var changelogDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		_fileSystem.Directory.CreateDirectory(changelogDir);
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

		var file1 = _fileSystem.Path.Combine(_changelogDir, "1755268130-first-changelog.yaml");
		var file2 = _fileSystem.Path.Combine(_changelogDir, "1755268140-second-changelog.yaml");
		await _fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await _fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			All = true,
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await _fileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
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

		var file1 = _fileSystem.Path.Combine(_changelogDir, "1755268130-elasticsearch-feature.yaml");
		var file2 = _fileSystem.Path.Combine(_changelogDir, "1755268140-kibana-feature.yaml");
		await _fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await _fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			InputProducts = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await _fileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
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

		var file1 = _fileSystem.Path.Combine(_changelogDir, "1755268130-first-pr.yaml");
		var file2 = _fileSystem.Path.Combine(_changelogDir, "1755268140-second-pr.yaml");
		var file3 = _fileSystem.Path.Combine(_changelogDir, "1755268150-third-pr.yaml");
		await _fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await _fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);
		await _fileSystem.File.WriteAllTextAsync(file3, changelog3, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			Prs = ["https://github.com/elastic/elasticsearch/pull/100", "https://github.com/elastic/elasticsearch/pull/200"],
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await _fileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
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

		var file1 = _fileSystem.Path.Combine(_changelogDir, "1755268130-first-pr.yaml");
		await _fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			Prs =
			[
				"https://github.com/elastic/elasticsearch/pull/100",
				"https://github.com/elastic/elasticsearch/pull/200",
				"https://github.com/elastic/elasticsearch/pull/300"
			],
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		_collector.Warnings.Should().Be(2); // Two unmatched PRs
		_collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("No changelog file found for PR: https://github.com/elastic/elasticsearch/pull/200"));
		_collector.Diagnostics.Should().Contain(d =>
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

		var file1 = _fileSystem.Path.Combine(_changelogDir, "1755268130-first-pr.yaml");
		var file2 = _fileSystem.Path.Combine(_changelogDir, "1755268140-second-pr.yaml");
		await _fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await _fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		// Create PRs file
		var prsFile = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "prs.txt");
		_fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(prsFile)!);
		// language=yaml
		var prsContent =
			"""
			https://github.com/elastic/elasticsearch/pull/100
			https://github.com/elastic/elasticsearch/pull/200
			""";
		await _fileSystem.File.WriteAllTextAsync(prsFile, prsContent, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			Prs = [prsFile],
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await _fileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
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

		var file1 = _fileSystem.Path.Combine(_changelogDir, "1755268130-pr-number.yaml");
		await _fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			Prs = ["100"],
			Owner = "elastic",
			Repo = "elasticsearch",
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await _fileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
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

		var file1 = _fileSystem.Path.Combine(_changelogDir, "1755268130-short-format.yaml");
		await _fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			Prs = ["elastic/elasticsearch#133609"],
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await _fileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("name: 1755268130-short-format.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithNoMatchingFiles_ReturnsError()
	{
		// Arrange

		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			InputProducts = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("No YAML files found") || d.Message.Contains("No changelog entries matched"));
	}

	[Fact]
	public async Task BundleChangelogs_WithInvalidDirectory_ReturnsError()
	{
		// Arrange
		var invalidDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent");

		var input = new ChangelogBundleInput
		{
			Directory = invalidDir,
			All = true,
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("Directory does not exist"));
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

		var file1 = _fileSystem.Path.Combine(_changelogDir, "1755268130-first-changelog.yaml");
		var file2 = _fileSystem.Path.Combine(_changelogDir, "1755268140-second-changelog.yaml");
		await _fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await _fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("At least one filter option must be specified"));
	}

	[Fact]
	public async Task BundleChangelogs_WithMultipleFilterOptions_ReturnsError()
	{
		// Arrange

		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			All = true,
			InputProducts = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("Multiple filter options cannot be specified together"));
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

		var file1 = _fileSystem.Path.Combine(_changelogDir, "1755268130-cloud-feature1.yaml");
		var file2 = _fileSystem.Path.Combine(_changelogDir, "1755268140-cloud-feature2.yaml");
		await _fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await _fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			InputProducts =
			[
				new ProductInfo { Product = "cloud-serverless", Target = "2025-12-02", Lifecycle = "*" },
				new ProductInfo { Product = "cloud-serverless", Target = "2025-12-06", Lifecycle = "*" }
			],
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await _fileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
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

		var file1 = _fileSystem.Path.Combine(_changelogDir, "1755268130-elasticsearch-feature.yaml");
		var file2 = _fileSystem.Path.Combine(_changelogDir, "1755268140-kibana-feature.yaml");
		await _fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await _fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			InputProducts = [new ProductInfo { Product = "*", Target = "9.2.0", Lifecycle = "ga" }],
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await _fileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
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

		var file1 = _fileSystem.Path.Combine(_changelogDir, "1755268130-elasticsearch-feature.yaml");
		var file2 = _fileSystem.Path.Combine(_changelogDir, "1755268140-kibana-feature.yaml");
		await _fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await _fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			InputProducts = [new ProductInfo { Product = "*", Target = "*", Lifecycle = "*" }],
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await _fileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
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

		var file1 = _fileSystem.Path.Combine(_changelogDir, "1755268130-es-9.3.0.yaml");
		var file2 = _fileSystem.Path.Combine(_changelogDir, "1755268140-es-9.3.1.yaml");
		var file3 = _fileSystem.Path.Combine(_changelogDir, "1755268150-es-9.2.0.yaml");
		await _fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await _fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);
		await _fileSystem.File.WriteAllTextAsync(file3, changelog3, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			InputProducts = [new ProductInfo { Product = "elasticsearch", Target = "9.3.*", Lifecycle = "*" }],
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await _fileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("name: 1755268130-es-9.3.0.yaml");
		bundleContent.Should().Contain("name: 1755268140-es-9.3.1.yaml");
		bundleContent.Should().NotContain("name: 1755268150-es-9.2.0.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithNonExistentFileAsPrs_ReturnsError()
	{
		// Arrange

		// Provide a non-existent file path - should return error since there are no other PRs
		var nonexistentFile = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent.txt");
		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			Prs = [nonexistentFile],
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		// File doesn't exist and there are no other PRs, so should return error
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("File does not exist"));
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
		var changelogFile = _fileSystem.Path.Combine(_changelogDir, "1755268130-test-pr.yaml");
		await _fileSystem.File.WriteAllTextAsync(changelogFile, changelog, TestContext.Current.CancellationToken);

		// Provide a URL - should be treated as a PR identifier, not a file path
		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			Prs = ["https://github.com/elastic/elasticsearch/pull/123"],
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		// URL should be treated as PR identifier and match the changelog
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		_collector.Warnings.Should().Be(0);

		var bundleContent = await _fileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
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
		var changelogFile = _fileSystem.Path.Combine(_changelogDir, "1755268130-test-pr.yaml");
		await _fileSystem.File.WriteAllTextAsync(changelogFile, changelog, TestContext.Current.CancellationToken);

		// Provide a non-existent file path along with a valid PR - should emit warning for file but continue with PR
		var nonexistentFile = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent.txt");
		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			Prs = [nonexistentFile, "https://github.com/elastic/elasticsearch/pull/123"],
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		// Should succeed because we have a valid PR, but should emit warning for the non-existent file
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		_collector.Warnings.Should().BeGreaterThan(0);
		// Check that we have a warning about the file not existing
		var fileWarning = _collector.Diagnostics.FirstOrDefault(d => d.Message.Contains("File does not exist, skipping"));
		fileWarning.Should().NotBeNull("Expected a warning about the non-existent file being skipped");

		var bundleContent = await _fileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
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

		var file1 = _fileSystem.Path.Combine(_changelogDir, "1755268130-elasticsearch-feature.yaml");
		var file2 = _fileSystem.Path.Combine(_changelogDir, "1755268140-kibana-feature.yaml");
		await _fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await _fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			All = true,
			OutputProducts =
			[
				new ProductInfo { Product = "cloud-serverless", Target = "2025-12-02", Lifecycle = "ga" },
				new ProductInfo { Product = "cloud-serverless", Target = "2025-12-06", Lifecycle = "beta" }
			],
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await _fileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
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

		var file1 = _fileSystem.Path.Combine(_changelogDir, "1755268130-elasticsearch.yaml");
		var file2 = _fileSystem.Path.Combine(_changelogDir, "1755268140-kibana.yaml");
		var file3 = _fileSystem.Path.Combine(_changelogDir, "1755268150-multi-product.yaml");
		await _fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await _fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);
		await _fileSystem.File.WriteAllTextAsync(file3, changelog3, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			All = true,
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await _fileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
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

		var file1 = _fileSystem.Path.Combine(_changelogDir, "1755268130-elasticsearch-ga.yaml");
		var file2 = _fileSystem.Path.Combine(_changelogDir, "1755268140-elasticsearch-beta.yaml");
		await _fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await _fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			InputProducts =
			[
				new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" },
				new ProductInfo { Product = "elasticsearch", Target = "9.3.0", Lifecycle = "beta" }
			],
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await _fileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
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

		var file1 = _fileSystem.Path.Combine(_changelogDir, "1755268130-elasticsearch.yaml");
		await _fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			All = true,
			OutputProducts =
			[
				new ProductInfo { Product = "cloud-serverless", Target = "2025-12-02", Lifecycle = "ga" },
				new ProductInfo { Product = "cloud-serverless", Target = "2025-12-06", Lifecycle = "beta" }
			],
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await _fileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
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

		var file1 = _fileSystem.Path.Combine(_changelogDir, "1755268130-elasticsearch-ga.yaml");
		var file2 = _fileSystem.Path.Combine(_changelogDir, "1755268140-elasticsearch-beta.yaml");
		await _fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await _fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			All = true,
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await _fileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
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

		var file1 = _fileSystem.Path.Combine(_changelogDir, "1-feature.yaml");
		await _fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			InputProducts =
			[
				new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "*" }
			],
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await _fileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
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

		var file1 = _fileSystem.Path.Combine(_changelogDir, "1755268130-elasticsearch-ga.yaml");
		var file2 = _fileSystem.Path.Combine(_changelogDir, "1755268140-elasticsearch-beta.yaml");
		var file3 = _fileSystem.Path.Combine(_changelogDir, "1755268150-elasticsearch-no-lifecycle.yaml");
		await _fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await _fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);
		await _fileSystem.File.WriteAllTextAsync(file3, changelog3, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			All = true,
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		_collector.Warnings.Should().BeGreaterThan(0);
		// Verify warning message includes lifecycle values
		_collector.Diagnostics.Should().Contain(d =>
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

		var file1 = _fileSystem.Path.Combine(_changelogDir, "1755268130-test-feature.yaml");
		await _fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			All = true,
			Resolve = true,
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await _fileSystem.File.ReadAllTextAsync(input.Output, TestContext.Current.CancellationToken);
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

		var file1 = _fileSystem.Path.Combine(_changelogDir, "1755268130-test-feature.yaml");
		await _fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		// Use a directory path with default filename (simulating command layer processing)
		var outputDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var outputPath = _fileSystem.Path.Combine(outputDir, "changelog-bundle.yaml");

		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			All = true,
			Output = outputPath
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		_fileSystem.File.Exists(outputPath).Should().BeTrue("Output file should be created");

		var bundleContent = await _fileSystem.File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
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

		var file1 = _fileSystem.Path.Combine(_changelogDir, "1755268130-test-feature.yaml");
		await _fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			All = true,
			Resolve = true,
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("missing required field: title"));
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

		var file1 = _fileSystem.Path.Combine(_changelogDir, "1755268130-test-feature.yaml");
		await _fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			All = true,
			Resolve = true,
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("missing required field: type"));
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

		var file1 = _fileSystem.Path.Combine(_changelogDir, "1755268130-test-feature.yaml");
		await _fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			All = true,
			Resolve = true,
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("missing required field: products"));
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

		var file1 = _fileSystem.Path.Combine(_changelogDir, "1755268130-test-feature.yaml");
		await _fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = _changelogDir,
			All = true,
			Resolve = true,
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await Service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("product entry missing required field: product"));
	}
}
