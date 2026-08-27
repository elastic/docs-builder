// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// Tests for the standardized bundle output naming (B2 — elastic/docs-builder#3774):
/// explicit <c>output:</c> patterns are a hard error, names derive from the profile's primary
/// output product as <c>{product}-{version}.yaml</c>, and two profiles colliding on the same
/// conventional target are rejected.
/// </summary>
public class BundleOutputConventionTests(ITestOutputHelper output) : ChangelogTestBase(output)
{
	// language=yaml
	private const string Entry =
		"""
		title: Sample change
		type: feature
		products:
		  - product: elasticsearch
		    target: 9.3.0
		    lifecycle: ga
		""";

	private string _changelogDir = string.Empty;

	private async Task<string> WriteConfig(string configContent)
	{
		_changelogDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(_changelogDir);
		await FileSystem.File.WriteAllTextAsync(
			FileSystem.Path.Join(_changelogDir, "entry.yaml"),
			Entry,
			TestContext.Current.CancellationToken
		);

		var configPath = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(
			configPath,
			configContent.Replace("CHANGELOG_DIR", _changelogDir),
			TestContext.Current.CancellationToken
		);
		return configPath;
	}

	private ChangelogBundlingService Service() => new(LoggerFactory, FileSystem, ConfigurationContext);

	[Fact]
	public async Task ProfileWithOutputPattern_EmitsHardError()
	{
		var configPath = await WriteConfig(
			"""
			bundle:
			  directory: CHANGELOG_DIR
			  use_local_changelogs: true
			  profiles:
			    es-release:
			      products: "elasticsearch {version} *"
			      output: "elasticsearch-{version}.yaml"
			"""
		);

		var input = new BundleChangelogsArguments { Profile = "es-release", ProfileArgument = "9.3.0", Config = configPath };
		var result = await Service().BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector
			.Diagnostics
			.Should()
			.Contain(
				d => d.Severity == Severity.Error && d.Message.Contains("'output' is no longer supported") && d.Message.Contains(
					"{product}-{version}.yaml"
				)
			);
	}

	[Fact]
	public async Task OutputPatternOnAnotherProfile_AlsoErrors()
	{
		// The validation covers every profile in the file, not just the invoked one — a stale
		// output: elsewhere would silently produce an unexpected path on its next invocation.
		var configPath = await WriteConfig(
			"""
			bundle:
			  directory: CHANGELOG_DIR
			  use_local_changelogs: true
			  profiles:
			    es-release:
			      products: "elasticsearch {version} *"
			    legacy:
			      products: "cloud-hosted {version} *"
			      output: "legacy-{version}.yaml"
			"""
		);

		var input = new BundleChangelogsArguments { Profile = "es-release", ProfileArgument = "9.3.0", Config = configPath };
		var result = await Service().BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("Profile 'legacy'"));
	}

	[Fact]
	public async Task ProfilesCollidingOnPrimaryProduct_EmitError()
	{
		var configPath = await WriteConfig(
			"""
			bundle:
			  directory: CHANGELOG_DIR
			  use_local_changelogs: true
			  profiles:
			    es-ga:
			      products: "elasticsearch {version} ga"
			      output_products: "elasticsearch {version}"
			    es-all:
			      products: "elasticsearch {version} *"
			      output_products: "elasticsearch {version}"
			"""
		);

		var input = new BundleChangelogsArguments { Profile = "es-ga", ProfileArgument = "9.3.0", Config = configPath };
		var result = await Service().BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector
			.Diagnostics
			.Should()
			.Contain(
				d => d.Severity == Severity.Error && d.Message.Contains("'es-all', 'es-ga'") && d.Message.Contains(
					"elasticsearch-{version}.yaml"
				)
			);
	}

	[Fact]
	public async Task ProfileWithoutOutput_WritesConventionalName()
	{
		var configPath = await WriteConfig(
			"""
			bundle:
			  directory: CHANGELOG_DIR
			  use_local_changelogs: true
			  profiles:
			    es-release:
			      products: "elasticsearch {version} *"
			      output_products: "elasticsearch {version}"
			"""
		);

		var input = new BundleChangelogsArguments { Profile = "es-release", ProfileArgument = "9.3.0", Config = configPath };
		var result = await Service().BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue(
			$"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}"
		);
		FileSystem
			.File
			.Exists(FileSystem.Path.Join(_changelogDir, "elasticsearch-9.3.0.yaml"))
			.Should()
			.BeTrue("bundle names derive from the primary output product and version");
	}

	[Fact]
	public async Task Plan_ProfileWithOutputPattern_FailsTheSameWay()
	{
		var configPath = await WriteConfig(
			"""
			bundle:
			  directory: CHANGELOG_DIR
			  profiles:
			    es-release:
			      products: "elasticsearch {version} *"
			      output: "elasticsearch-{version}.yaml"
			"""
		);

		var input = new BundleChangelogsArguments { Profile = "es-release", ProfileArgument = "9.3.0", Config = configPath };
		var plan = await Service().PlanBundleAsync(Collector, input, hasReleaseVersion: false, TestContext.Current.CancellationToken);

		plan.Should().BeNull();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("'output' is no longer supported"));
	}
}
