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

	public ChangelogRemoveTests(ITestOutputHelper output) : base(output)
	{
		Service = new ChangelogRemoveService(LoggerFactory, null, FileSystem);
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
}
