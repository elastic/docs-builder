// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Extensions;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// Tests for the standardized bundle output naming (B2 — elastic/docs-builder#3774):
/// explicit profile <c>output:</c> patterns are a hard error, names derive as
/// <c>{repo}-{product}-{version}.yaml</c> when a repo resolves (else unprefixed with a warning)
/// in both profile and option mode, and two profiles colliding on the same conventional
/// target are rejected.
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
					"{repo}-{product}-{version}.yaml"
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
					"{repo}-elasticsearch-{version}.yaml"
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
			.BeTrue("when no authoring repo resolves, names fall back to the unprefixed product-version.yaml convention");
		Collector
			.Diagnostics
			.Should()
			.Contain(d => d.Severity == Severity.Warning && d.Message.Contains("Could not resolve a repository name"));
	}

	[Fact]
	public async Task ProfileWithBundleRepo_PrefixesFileName()
	{
		var configPath = await WriteConfig(
			"""
			bundle:
			  directory: CHANGELOG_DIR
			  use_local_changelogs: true
			  repo: kibana
			  profiles:
			    serverless-release:
			      products: "elasticsearch {version} *"
			      output_products: "cloud-serverless {version}"
			"""
		);

		var input = new BundleChangelogsArguments { Profile = "serverless-release", ProfileArgument = "9.3.0", Config = configPath };
		var result = await Service().BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue(
			$"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}"
		);
		FileSystem
			.File
			.Exists(FileSystem.Path.Join(_changelogDir, "kibana-cloud-serverless-9.3.0.yaml"))
			.Should()
			.BeTrue("authoring repo prefixes the conventional product-version name");
	}

	[Fact]
	public async Task Plan_ProfileWithBundleRepo_PrefixesFileName()
	{
		var configPath = await WriteConfig(
			"""
			bundle:
			  directory: CHANGELOG_DIR
			  repo: kibana
			  profiles:
			    serverless-release:
			      products: "elasticsearch {version} *"
			      output_products: "cloud-serverless {version}"
			"""
		);

		var input = new BundleChangelogsArguments { Profile = "serverless-release", ProfileArgument = "9.3.0", Config = configPath };
		var plan = await Service().PlanBundleAsync(Collector, input, hasReleaseVersion: false, TestContext.Current.CancellationToken);

		plan.Should().NotBeNull();
		FileSystem.Path.GetFileName(plan.OutputPath).Should().Be("kibana-cloud-serverless-9.3.0.yaml");
	}

	[Fact]
	public async Task CliRepo_OverridesBundleRepo()
	{
		var configPath = await WriteConfig(
			"""
			bundle:
			  directory: CHANGELOG_DIR
			  use_local_changelogs: true
			  repo: elasticsearch
			  profiles:
			    es-release:
			      products: "elasticsearch {version} *"
			      output_products: "elasticsearch {version}"
			"""
		);

		var input = new BundleChangelogsArguments
		{
			Profile = "es-release",
			ProfileArgument = "9.3.0",
			Config = configPath,
			Repo = "kibana"
		};
		var result = await Service().BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue(
			$"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}"
		);
		FileSystem.File.Exists(FileSystem.Path.Join(_changelogDir, "kibana-elasticsearch-9.3.0.yaml")).Should().BeTrue();
	}

	[Fact]
	public async Task ProfileRepo_OverridesBundleRepo()
	{
		var configPath = await WriteConfig(
			"""
			bundle:
			  directory: CHANGELOG_DIR
			  use_local_changelogs: true
			  repo: elasticsearch
			  profiles:
			    es-release:
			      products: "elasticsearch {version} *"
			      output_products: "elasticsearch {version}"
			      repo: kibana
			"""
		);

		var input = new BundleChangelogsArguments { Profile = "es-release", ProfileArgument = "9.3.0", Config = configPath };
		var result = await Service().BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue(
			$"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}"
		);
		FileSystem.File.Exists(FileSystem.Path.Join(_changelogDir, "kibana-elasticsearch-9.3.0.yaml")).Should().BeTrue();
	}

	[Fact]
	public async Task CombinedOwnerRepo_UsesRepoSegmentOnly()
	{
		var configPath = await WriteConfig(
			"""
			bundle:
			  directory: CHANGELOG_DIR
			  use_local_changelogs: true
			  repo: elastic/kibana
			  profiles:
			    serverless-release:
			      products: "elasticsearch {version} *"
			      output_products: "cloud-serverless {version}"
			"""
		);

		var input = new BundleChangelogsArguments { Profile = "serverless-release", ProfileArgument = "9.3.0", Config = configPath };
		var result = await Service().BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue(
			$"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}"
		);
		FileSystem.File.Exists(FileSystem.Path.Join(_changelogDir, "kibana-cloud-serverless-9.3.0.yaml")).Should().BeTrue();
	}

	[Fact]
	public async Task GitOrigin_UsedWhenRepoUnset()
	{
		var gitRoot = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(FileSystem.Path.Join(gitRoot, ".git"));
		await FileSystem.File.WriteAllTextAsync(
			FileSystem.Path.Join(gitRoot, ".git", "config"),
			"""
			[remote "origin"]
				url = https://github.com/elastic/kibana.git
			""",
			TestContext.Current.CancellationToken
		);

		var changelogDir = FileSystem.Path.Join(gitRoot, "changelog");
		FileSystem.Directory.CreateDirectory(changelogDir);
		await FileSystem.File.WriteAllTextAsync(
			FileSystem.Path.Join(changelogDir, "entry.yaml"),
			Entry,
			TestContext.Current.CancellationToken
		);
		_changelogDir = changelogDir;

		var configPath = FileSystem.Path.Join(gitRoot, "docs", "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(
			configPath,
			"""
			bundle:
			  directory: CHANGELOG_DIR
			  use_local_changelogs: true
			  profiles:
			    serverless-release:
			      products: "elasticsearch {version} *"
			      output_products: "cloud-serverless {version}"
			""".Replace(
				"CHANGELOG_DIR",
				changelogDir
			),
			TestContext.Current.CancellationToken
		);

		var input = new BundleChangelogsArguments { Profile = "serverless-release", ProfileArgument = "9.3.0", Config = configPath };
		var result = await Service().BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue(
			$"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}"
		);
		FileSystem.File.Exists(FileSystem.Path.Join(changelogDir, "kibana-cloud-serverless-9.3.0.yaml")).Should().BeTrue();
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

	[Fact]
	public async Task OptionMode_OutputProductsAndBundleRepo_WritesPrefixedName()
	{
		var configPath = await WriteConfig(
			"""
			bundle:
			  directory: CHANGELOG_DIR
			  use_local_changelogs: true
			  repo: kibana
			"""
		);

		var input = new BundleChangelogsArguments
		{
			All = true,
			Config = configPath,
			OutputProducts = [new ProductArgument { Product = "cloud-serverless", Target = "2026-08-27" }]
		};
		var result = await Service().BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue(
			$"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}"
		);
		FileSystem
			.File
			.Exists(FileSystem.Path.Join(_changelogDir, "kibana-cloud-serverless-2026-08-27.yaml"))
			.Should()
			.BeTrue("option mode without --output uses the same repo-product-version convention as profile mode");
	}

	[Fact]
	public async Task OptionMode_ExplicitYamlOutput_Unchanged()
	{
		var configPath = await WriteConfig(
			"""
			bundle:
			  directory: CHANGELOG_DIR
			  use_local_changelogs: true
			  repo: kibana
			"""
		);

		var custom = FileSystem.Path.Join(_changelogDir, "custom.yaml");
		var input = new BundleChangelogsArguments
		{
			All = true,
			Config = configPath,
			Output = custom,
			OutputProducts = [new ProductArgument { Product = "cloud-serverless", Target = "2026-08-27" }]
		};
		var result = await Service().BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue(
			$"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}"
		);
		FileSystem.File.Exists(custom).Should().BeTrue("an explicit yaml --output path is used as-is");
		FileSystem.File.Exists(FileSystem.Path.Join(_changelogDir, "kibana-cloud-serverless-2026-08-27.yaml")).Should().BeFalse();
	}

	[Fact]
	public async Task OptionMode_DirectoryOutput_JoinsConventionalName()
	{
		var configPath = await WriteConfig(
			"""
			bundle:
			  directory: CHANGELOG_DIR
			  use_local_changelogs: true
			  repo: kibana
			"""
		);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);

		var input = new BundleChangelogsArguments
		{
			All = true,
			Config = configPath,
			Output = outputDir,
			OutputProducts = [new ProductArgument { Product = "cloud-serverless", Target = "2026-08-27" }]
		};
		var result = await Service().BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue(
			$"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}"
		);
		FileSystem
			.File
			.Exists(FileSystem.Path.Join(outputDir, "kibana-cloud-serverless-2026-08-27.yaml"))
			.Should()
			.BeTrue("a directory --output joins the conventional file name");
	}

	[Fact]
	public async Task OptionMode_MissingProductAndVersion_WarnsAndUsesFallbackName()
	{
		var configPath = await WriteConfig(
			"""
			bundle:
			  directory: CHANGELOG_DIR
			  use_local_changelogs: true
			  repo: kibana
			"""
		);

		var input = new BundleChangelogsArguments { All = true, Config = configPath };
		var result = await Service().BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue(
			$"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}"
		);
		FileSystem
			.File
			.Exists(FileSystem.Path.Join(_changelogDir, BundleOutputNaming.FallbackFileName))
			.Should()
			.BeTrue("option mode without a concrete product and version keeps the legacy fallback file name");
		Collector
			.Diagnostics
			.Should()
			.Contain(d => d.Severity == Severity.Warning && d.Message.Contains("Could not resolve a product and version"));
	}

	[Fact]
	public async Task OptionMode_PlanMatchesRunPath()
	{
		var configPath = await WriteConfig(
			"""
			bundle:
			  directory: CHANGELOG_DIR
			  use_local_changelogs: true
			  output_directory: CHANGELOG_DIR
			  repo: kibana
			"""
		);

		var input = new BundleChangelogsArguments
		{
			All = true,
			Config = configPath,
			OutputProducts = [new ProductArgument { Product = "cloud-serverless", Target = "2026-08-27" }]
		};

		var plan = await Service().PlanBundleAsync(Collector, input, hasReleaseVersion: false, TestContext.Current.CancellationToken);
		plan.Should().NotBeNull();
		plan!
			.OutputPath
			.Should()
			.Be(FileSystem.Path.Join(_changelogDir, "kibana-cloud-serverless-2026-08-27.yaml").OptionalWindowsReplace());

		var result = await Service().BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);
		result.Should().BeTrue(
			$"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}"
		);
		FileSystem.File.Exists(plan.OutputPath).Should().BeTrue("--plan output_path matches the file bundle writes");
	}

	[Fact]
	public void ResolveVersion_PrefersOutputProductsThenInputThenReleaseTag()
	{
		BundleOutputNaming
			.ResolveVersion(
				[new ProductArgument { Product = "cloud-serverless", Target = "2026-08-27" }],
				[new ProductArgument { Product = "elasticsearch", Target = "9.3.0" }],
				"v9.2.0"
			)
			.Should()
			.Be("2026-08-27");

		BundleOutputNaming
			.ResolveVersion(null, [new ProductArgument { Product = "elasticsearch", Target = "9.3.0" }], "v9.2.0")
			.Should()
			.Be("9.3.0");

		BundleOutputNaming
			.ResolveVersion(null, [new ProductArgument { Product = "elasticsearch", Target = "*" }], "v9.2.0-beta.1")
			.Should()
			.Be("9.2.0");

		BundleOutputNaming.ResolveVersion(null, null, "latest").Should().BeNull();
	}
}
