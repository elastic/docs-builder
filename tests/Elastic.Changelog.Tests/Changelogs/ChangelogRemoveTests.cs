// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Bundling;
using Elastic.Documentation.Diagnostics;
using FluentAssertions;

namespace Elastic.Changelog.Tests.Changelogs;

public class ChangelogRemoveTests : ChangelogTestBase
{
	private ChangelogRemoveService Service { get; }
	private ChangelogRemoveService ServiceWithConfig { get; }
	private readonly string _changelogDir;

	// language=yaml
	private const string ElasticsearchFeatureYaml =
		"""
		title: Elasticsearch feature
		type: feature
		products:
		  - product: elasticsearch
		    target: 9.3.0
		    lifecycle: ga
		prs:
		  - https://github.com/elastic/elasticsearch/pull/1001
		""";

	// language=yaml
	private const string KibanaFeatureYaml =
		"""
		title: Kibana feature
		type: feature
		products:
		  - product: kibana
		    target: 9.3.0
		    lifecycle: ga
		prs:
		  - https://github.com/elastic/kibana/pull/2001
		""";

	// language=yaml
	private const string ElasticsearchBugFixYaml =
		"""
		title: Elasticsearch bug fix
		type: bug-fix
		products:
		  - product: elasticsearch
		    target: 9.3.0
		    lifecycle: ga
		prs:
		  - https://github.com/elastic/elasticsearch/pull/1002
		issues:
		  - https://github.com/elastic/elasticsearch/issues/9999
		""";

	// language=yaml
	private const string Elasticsearch920FeatureYaml =
		"""
		title: Elasticsearch 9.2.0 feature
		type: feature
		products:
		  - product: elasticsearch
		    target: 9.2.0
		    lifecycle: ga
		prs:
		  - https://github.com/elastic/elasticsearch/pull/5001
		""";

	public ChangelogRemoveTests(ITestOutputHelper output) : base(output)
	{
		Service = new ChangelogRemoveService(LoggerFactory, null, FileSystem);
		ServiceWithConfig = new ChangelogRemoveService(LoggerFactory, ConfigurationContext, FileSystem);
		_changelogDir = CreateChangelogDir();
	}

	private string CreateChangelogDir()
	{
		var dir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(dir);
		return dir;
	}

	private async Task WriteFile(string fileName, string content)
	{
		var path = FileSystem.Path.Combine(_changelogDir, fileName);
		await FileSystem.File.WriteAllTextAsync(path, content, TestContext.Current.CancellationToken);
	}

	private bool FileExists(string fileName) =>
		FileSystem.File.Exists(FileSystem.Path.Combine(_changelogDir, fileName));

	// ------------------------------------------------------------------
	// Basic filter tests
	// ------------------------------------------------------------------

	[Fact]
	public async Task Remove_WithAll_DeletesAllFiles()
	{
		await WriteFile("1001-es-feature.yaml", ElasticsearchFeatureYaml);
		await WriteFile("2001-kibana-feature.yaml", KibanaFeatureYaml);

		var input = new ChangelogRemoveArguments { Directory = _changelogDir, All = true };

		var result = await Service.RemoveChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		FileExists("1001-es-feature.yaml").Should().BeFalse();
		FileExists("2001-kibana-feature.yaml").Should().BeFalse();
	}

	[Fact]
	public async Task Remove_WithProducts_DeletesMatchingOnly()
	{
		await WriteFile("1001-es-feature.yaml", ElasticsearchFeatureYaml);
		await WriteFile("2001-kibana-feature.yaml", KibanaFeatureYaml);

		var input = new ChangelogRemoveArguments
		{
			Directory = _changelogDir,
			Products = [new ProductArgument { Product = "elasticsearch", Target = "*", Lifecycle = "*" }]
		};

		var result = await Service.RemoveChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		FileExists("1001-es-feature.yaml").Should().BeFalse("Elasticsearch changelog should be removed");
		FileExists("2001-kibana-feature.yaml").Should().BeTrue("Kibana changelog should be kept");
	}

	[Fact]
	public async Task Remove_WithPrs_DeletesMatchingOnly()
	{
		await WriteFile("1001-es-feature.yaml", ElasticsearchFeatureYaml);
		await WriteFile("2001-kibana-feature.yaml", KibanaFeatureYaml);

		var input = new ChangelogRemoveArguments
		{
			Directory = _changelogDir,
			Prs = ["https://github.com/elastic/elasticsearch/pull/1001"]
		};

		var result = await Service.RemoveChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		FileExists("1001-es-feature.yaml").Should().BeFalse("Matched changelog should be removed");
		FileExists("2001-kibana-feature.yaml").Should().BeTrue("Unmatched changelog should be kept");
	}

	[Fact]
	public async Task Remove_WithIssues_DeletesMatchingOnly()
	{
		await WriteFile("1001-es-feature.yaml", ElasticsearchFeatureYaml);
		await WriteFile("1002-es-bugfix.yaml", ElasticsearchBugFixYaml);

		var input = new ChangelogRemoveArguments
		{
			Directory = _changelogDir,
			Issues = ["https://github.com/elastic/elasticsearch/issues/9999"]
		};

		var result = await Service.RemoveChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		FileExists("1001-es-feature.yaml").Should().BeTrue("Non-matching changelog should be kept");
		FileExists("1002-es-bugfix.yaml").Should().BeFalse("Issue-matched changelog should be removed");
	}

	// ------------------------------------------------------------------
	// Dry-run
	// ------------------------------------------------------------------

	[Fact]
	public async Task Remove_WithDryRun_DoesNotDelete()
	{
		await WriteFile("1001-es-feature.yaml", ElasticsearchFeatureYaml);
		await WriteFile("2001-kibana-feature.yaml", KibanaFeatureYaml);

		var input = new ChangelogRemoveArguments { Directory = _changelogDir, All = true, DryRun = true };

		var result = await Service.RemoveChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		FileExists("1001-es-feature.yaml").Should().BeTrue("Dry-run must not delete files");
		FileExists("2001-kibana-feature.yaml").Should().BeTrue("Dry-run must not delete files");
	}

	// ------------------------------------------------------------------
	// Validation
	// ------------------------------------------------------------------

	[Fact]
	public async Task Remove_WithNoFilter_EmitsError()
	{
		await WriteFile("1001-es-feature.yaml", ElasticsearchFeatureYaml);

		var input = new ChangelogRemoveArguments { Directory = _changelogDir };

		var result = await Service.RemoveChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().ContainSingle(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("At least one filter option"));
	}

	[Fact]
	public async Task Remove_WithMultipleFilters_EmitsError()
	{
		await WriteFile("1001-es-feature.yaml", ElasticsearchFeatureYaml);

		var input = new ChangelogRemoveArguments
		{
			Directory = _changelogDir,
			All = true,
			Prs = ["https://github.com/elastic/elasticsearch/pull/1001"]
		};

		var result = await Service.RemoveChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().ContainSingle(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("Multiple filter options cannot be specified together"));
	}

	[Fact]
	public async Task Remove_WithNoMatchingChangelogs_EmitsError()
	{
		await WriteFile("2001-kibana-feature.yaml", KibanaFeatureYaml);

		var input = new ChangelogRemoveArguments
		{
			Directory = _changelogDir,
			Products = [new ProductArgument { Product = "elasticsearch", Target = "*", Lifecycle = "*" }]
		};

		var result = await Service.RemoveChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().ContainSingle(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("No changelog entries matched"));
	}

	// ------------------------------------------------------------------
	// Bundle dependency checks
	// ------------------------------------------------------------------

	[Fact]
	public async Task Remove_WhenReferencedByUnresolvedBundle_Blocks()
	{
		await WriteFile("1001-es-feature.yaml", ElasticsearchFeatureYaml);

		var bundlesDir = FileSystem.Path.Combine(_changelogDir, "bundles");
		FileSystem.Directory.CreateDirectory(bundlesDir);
		var checksum = ComputeSha1(ElasticsearchFeatureYaml);
		// language=yaml
		await FileSystem.File.WriteAllTextAsync(
			FileSystem.Path.Combine(bundlesDir, "9.3.0.yaml"),
			// language=yaml
			$"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- file:
			    name: 1001-es-feature.yaml
			    checksum: {checksum}
			""",
			TestContext.Current.CancellationToken
		);

		var input = new ChangelogRemoveArguments { Directory = _changelogDir, All = true };

		var result = await Service.RemoveChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse("Command should be blocked when a referenced bundle exists");
		Collector.Diagnostics.Should().ContainSingle(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("1001-es-feature.yaml") &&
			d.Message.Contains("unresolved bundle"));
		FileExists("1001-es-feature.yaml").Should().BeTrue("File must not be deleted when blocked");
	}

	[Fact]
	public async Task Remove_WhenReferencedByUnresolvedBundle_WithForce_Proceeds()
	{
		await WriteFile("1001-es-feature.yaml", ElasticsearchFeatureYaml);

		var bundlesDir = FileSystem.Path.Combine(_changelogDir, "bundles");
		FileSystem.Directory.CreateDirectory(bundlesDir);
		var checksum = ComputeSha1(ElasticsearchFeatureYaml);
		await FileSystem.File.WriteAllTextAsync(
			FileSystem.Path.Combine(bundlesDir, "9.3.0.yaml"),
			// language=yaml
			$"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- file:
			    name: 1001-es-feature.yaml
			    checksum: {checksum}
			""",
			TestContext.Current.CancellationToken
		);

		var input = new ChangelogRemoveArguments { Directory = _changelogDir, All = true, Force = true };

		var result = await Service.RemoveChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue("--force should allow deletion despite dependency");
		Collector.Errors.Should().Be(0, "With --force, errors become warnings");
		Collector.Diagnostics.Should().ContainSingle(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("1001-es-feature.yaml"));
		FileExists("1001-es-feature.yaml").Should().BeFalse("File should be deleted with --force");
	}

	[Fact]
	public async Task Remove_WhenReferencedByResolvedBundle_Proceeds()
	{
		await WriteFile("1001-es-feature.yaml", ElasticsearchFeatureYaml);

		var bundlesDir = FileSystem.Path.Combine(_changelogDir, "bundles");
		FileSystem.Directory.CreateDirectory(bundlesDir);

		// Bundle has ONLY inline (resolved) entries — no file references
		await FileSystem.File.WriteAllTextAsync(
			FileSystem.Path.Combine(bundlesDir, "9.3.0.yaml"),
			// language=yaml
			"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Already resolved entry
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  prs:
			  - https://github.com/elastic/elasticsearch/pull/999
			""",
			TestContext.Current.CancellationToken
		);

		var input = new ChangelogRemoveArguments { Directory = _changelogDir, All = true };

		var result = await Service.RemoveChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue("Resolved bundles do not block removal");
		Collector.Errors.Should().Be(0);
		FileExists("1001-es-feature.yaml").Should().BeFalse("File should be deleted");
	}

	[Fact]
	public async Task Remove_WithNoBundlesFound_Proceeds()
	{
		await WriteFile("1001-es-feature.yaml", ElasticsearchFeatureYaml);
		// No bundles directory created — dependency check is skipped

		var input = new ChangelogRemoveArguments { Directory = _changelogDir, All = true };

		var result = await Service.RemoveChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue("Removal should proceed when no bundles are found");
		Collector.Errors.Should().Be(0);
		FileExists("1001-es-feature.yaml").Should().BeFalse("File should be deleted");
	}

	[Fact]
	public async Task Remove_WithBundlesDirOverride_UsesSpecifiedPath()
	{
		await WriteFile("1001-es-feature.yaml", ElasticsearchFeatureYaml);

		// Create a bundles dir in a custom location
		var customBundlesDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(customBundlesDir);
		var checksum = ComputeSha1(ElasticsearchFeatureYaml);
		await FileSystem.File.WriteAllTextAsync(
			FileSystem.Path.Combine(customBundlesDir, "9.3.0.yaml"),
			// language=yaml
			$"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- file:
			    name: 1001-es-feature.yaml
			    checksum: {checksum}
			""",
			TestContext.Current.CancellationToken
		);

		var input = new ChangelogRemoveArguments
		{
			Directory = _changelogDir,
			All = true,
			BundlesDir = customBundlesDir
		};

		var result = await Service.RemoveChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse("Custom bundles dir should be scanned and dependency found");
		Collector.Diagnostics.Should().ContainSingle(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("1001-es-feature.yaml"));
	}

	[Fact]
	public async Task Remove_WithDryRun_ShowsDependencyConflicts()
	{
		await WriteFile("1001-es-feature.yaml", ElasticsearchFeatureYaml);

		var bundlesDir = FileSystem.Path.Combine(_changelogDir, "bundles");
		FileSystem.Directory.CreateDirectory(bundlesDir);
		var checksum = ComputeSha1(ElasticsearchFeatureYaml);
		await FileSystem.File.WriteAllTextAsync(
			FileSystem.Path.Combine(bundlesDir, "9.3.0.yaml"),
			// language=yaml
			$"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- file:
			    name: 1001-es-feature.yaml
			    checksum: {checksum}
			""",
			TestContext.Current.CancellationToken
		);

		// Dry-run WITH dependency — should report error but not delete
		var input = new ChangelogRemoveArguments { Directory = _changelogDir, All = true, DryRun = true };

		var result = await Service.RemoveChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse("Dependency conflict should still block dry-run result");
		Collector.Diagnostics.Should().ContainSingle(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("1001-es-feature.yaml"));
		FileExists("1001-es-feature.yaml").Should().BeTrue("Dry-run must not delete files");
	}

	// ------------------------------------------------------------------
	// Profile-based removal tests
	// ------------------------------------------------------------------

	[Fact]
	public async Task Remove_WithProfileAndVersion_DeletesMatchingProducts()
	{
		// Arrange — two changelogs for elasticsearch 9.3.0 ga, one for elasticsearch 9.2.0 ga.
		// The profile targets "elasticsearch {version} {lifecycle}"; passing "9.2.0" should only
		// delete the 9.2.0 file.
		await WriteFile("1001-es-feature.yaml", ElasticsearchFeatureYaml);
		await WriteFile("1002-es-bugfix.yaml", ElasticsearchBugFixYaml);
		await WriteFile("5001-es-920-feature.yaml", Elasticsearch920FeatureYaml);

		// language=yaml
		var configContent =
			"""
			bundle:
			  profiles:
			    es-release:
			      products: "elasticsearch {version} {lifecycle}"
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var input = new ChangelogRemoveArguments
		{
			Directory = _changelogDir,
			Profile = "es-release",
			ProfileArgument = "9.2.0",
			Config = configPath
		};

		var result = await ServiceWithConfig.RemoveChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Expected removal to succeed, but got errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);
		// 9.2.0 file removed
		FileExists("5001-es-920-feature.yaml").Should().BeFalse("Profile-matched file should be removed");
		// 9.3.0 files untouched
		FileExists("1001-es-feature.yaml").Should().BeTrue("Non-matching file should remain");
		FileExists("1002-es-bugfix.yaml").Should().BeTrue("Non-matching file should remain");
	}

	[Fact]
	public async Task Remove_WithProfileAndPromotionReport_DeletesMatchingPrs()
	{
		// Arrange — write two changelogs and a promotion report file that mentions only the first PR
		await WriteFile("1001-es-feature.yaml", ElasticsearchFeatureYaml);
		await WriteFile("2001-kibana-feature.yaml", KibanaFeatureYaml);

		var reportContent = "<html><body>https://github.com/elastic/elasticsearch/pull/1001</body></html>";
		var reportPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), "report.html");
		await FileSystem.File.WriteAllTextAsync(reportPath, reportContent, TestContext.Current.CancellationToken);

		// language=yaml
		var configContent =
			"""
			bundle:
			  profiles:
			    es-release:
			      products: "elasticsearch {version} {lifecycle}"
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var input = new ChangelogRemoveArguments
		{
			Directory = _changelogDir,
			Profile = "es-release",
			ProfileArgument = reportPath,
			Config = configPath
		};

		var result = await ServiceWithConfig.RemoveChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Expected removal to succeed, but got errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);
		FileExists("1001-es-feature.yaml").Should().BeFalse("PR-matched file should be removed");
		FileExists("2001-kibana-feature.yaml").Should().BeTrue("Non-matched file should remain");
	}

	[Fact]
	public async Task Remove_WithProfile_UnknownProfile_ReturnsError()
	{
		await WriteFile("1001-es-feature.yaml", ElasticsearchFeatureYaml);

		// language=yaml
		var configContent =
			"""
			bundle:
			  profiles:
			    es-release:
			      products: "elasticsearch {version} {lifecycle}"
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var input = new ChangelogRemoveArguments
		{
			Directory = _changelogDir,
			Profile = "nonexistent-profile",
			ProfileArgument = "9.2.0",
			Config = configPath
		};

		var result = await ServiceWithConfig.RemoveChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().ContainSingle(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("nonexistent-profile") &&
			d.Message.Contains("not found"));
	}

	[Fact]
	public async Task Remove_WithProfile_MissingProfileArg_ReturnsError()
	{
		await WriteFile("1001-es-feature.yaml", ElasticsearchFeatureYaml);

		// language=yaml
		var configContent =
			"""
			bundle:
			  profiles:
			    es-release:
			      products: "elasticsearch {version} {lifecycle}"
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var input = new ChangelogRemoveArguments
		{
			Directory = _changelogDir,
			Profile = "es-release",
			ProfileArgument = null,
			Config = configPath
		};

		var result = await ServiceWithConfig.RemoveChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().ContainSingle(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("es-release") &&
			d.Message.Contains("requires a version number"));
	}

	[Fact]
	public async Task Remove_WithProfileMode_MissingConfig_ReturnsErrorWithAdvice()
	{
		// Arrange - no config file exists at ./changelog.yml or ./docs/changelog.yml.
		// Use a fresh MockFileSystem with a known CWD so discovery returns no results.
		var cwdFs = new System.IO.Abstractions.TestingHelpers.MockFileSystem(
			null,
			currentDirectory: "/empty-project"
		);
		cwdFs.Directory.CreateDirectory("/empty-project");
		var service = new ChangelogRemoveService(LoggerFactory, ConfigurationContext, cwdFs);

		var input = new ChangelogRemoveArguments
		{
			Profile = "es-release",
			ProfileArgument = "9.2.0"
			// Config intentionally omitted — triggers CWD discovery
		};

		// Act
		var result = await service.RemoveChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse("Should fail when no config file is found");
		Collector.Diagnostics.Should().ContainSingle(d =>
			d.Severity == Severity.Error &&
			(d.Message.Contains("changelog.yml") || d.Message.Contains("changelog init")),
			"Error message should mention changelog.yml or advise running changelog init"
		);
	}

	[Fact]
	public async Task Remove_WithProfile_NoProductsAndVersionArg_ReturnsSpecificError()
	{
		// Profile has no products pattern; passing a version (not a promotion report) should emit
		// the specific "no products pattern" error rather than the generic filter-missing error.
		await WriteFile("1001-es-feature.yaml", ElasticsearchFeatureYaml);

		// language=yaml
		var configContent =
			"""
			bundle:
			  profiles:
			    no-products-profile:
			      output: "release-{version}.yaml"
			""";

		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var input = new ChangelogRemoveArguments
		{
			Directory = _changelogDir,
			Profile = "no-products-profile",
			ProfileArgument = "9.2.0",
			Config = configPath
		};

		var result = await ServiceWithConfig.RemoveChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().ContainSingle(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("no-products-profile") &&
			d.Message.Contains("no 'products' pattern"));
	}

	// ─── Phase 3: URL list file support for remove ──────────────────────────────────

	[Fact]
	public async Task Remove_WithProfile_UrlListFile_PrUrls_RemovesMatchedFiles()
	{
		await WriteFile("1001-es-feature.yaml", ElasticsearchFeatureYaml);
		await WriteFile("2001-kibana-feature.yaml", KibanaFeatureYaml);

		// language=yaml
		var configContent =
			"""
			bundle:
			  profiles:
			    release:
			""";
		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// URL file contains only the ES PR
		var urlFile = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "prs.txt");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(urlFile)!);
		await FileSystem.File.WriteAllTextAsync(
			urlFile,
			"https://github.com/elastic/elasticsearch/pull/1001\n",
			TestContext.Current.CancellationToken
		);

		var input = new ChangelogRemoveArguments
		{
			Directory = _changelogDir,
			Profile = "release",
			ProfileArgument = urlFile,
			Config = configPath,
			DryRun = true
		};

		var result = await ServiceWithConfig.RemoveChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		// Dry-run: files still exist but the matched one should have been identified
		FileSystem.File.Exists(FileSystem.Path.Combine(_changelogDir, "1001-es-feature.yaml")).Should().BeTrue("dry-run should not delete files");
	}

	[Fact]
	public async Task Remove_WithProfile_CombinedVersionAndReport_UsesReportForFiltering()
	{
		await WriteFile("1001-es-feature.yaml", ElasticsearchFeatureYaml);
		await WriteFile("2001-kibana-feature.yaml", KibanaFeatureYaml);

		// language=yaml
		var configContent =
			"""
			bundle:
			  profiles:
			    release:
			""";
		var configPath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var urlFile = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "prs.txt");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(urlFile)!);
		await FileSystem.File.WriteAllTextAsync(
			urlFile,
			"https://github.com/elastic/elasticsearch/pull/1001\n",
			TestContext.Current.CancellationToken
		);

		var input = new ChangelogRemoveArguments
		{
			Directory = _changelogDir,
			Profile = "release",
			ProfileArgument = "9.3.0",   // version string
			ProfileReport = urlFile,      // URL list file (Phase 3.4)
			Config = configPath,
			DryRun = true
		};

		var result = await ServiceWithConfig.RemoveChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);
	}

	// ─── Phase 4: --report option for option-based remove ────────────────────────────

	[Fact]
	public async Task Remove_WithReportOption_ParsesPromotionReportAndFilters()
	{
		await WriteFile("1001-es-feature.yaml", ElasticsearchFeatureYaml);
		await WriteFile("2001-kibana-feature.yaml", KibanaFeatureYaml);

		var htmlReport =
			"""
			<html><body>
			  <a href="https://github.com/elastic/elasticsearch/pull/1001">PR 1001</a>
			</body></html>
			""";
		var reportFile = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "report.html");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(reportFile)!);
		await FileSystem.File.WriteAllTextAsync(reportFile, htmlReport, TestContext.Current.CancellationToken);

		var input = new ChangelogRemoveArguments
		{
			Directory = _changelogDir,
			Report = reportFile,
			DryRun = true
		};

		var result = await Service.RemoveChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);
	}
}
