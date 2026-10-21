// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// Pins the diagnostic each single-fault <c>changelog.yml</c> produces, whether the loader or the model constraints report it.
/// An error fails the load; a warning or hint does not.
/// </summary>
public class ChangelogConfigurationDiagnosticsTests : ChangelogTestBase
{
	private const string RequiredTypes = "pivot:\n  types:\n    feature:\n    bug-fix:\n    breaking-change:\n";

	[Test]
	[Arguments(RequiredTypes + "    nope:\n", Severity.Error, "Type 'nope' in pivot.types is not a valid type. Valid types: *")]
	[Arguments("pivot:\n  types:\n    feature:\n    breaking-change:\n", Severity.Error, "Required type 'bug-fix' is missing from pivot.types. *")]
	[Arguments("pivot:\n  types:\n    feature:\n      subtypes:\n        api: \">api\"\n    bug-fix:\n    breaking-change:\n", Severity.Error, "Type 'feature' has subtypes defined, but subtypes are only allowed for 'breaking-change' type.")]
	[Arguments("pivot:\n  types:\n    feature:\n    bug-fix:\n    breaking-change:\n      subtypes:\n        nope: \">nope\"\n", Severity.Error, "Subtype 'nope' in pivot.types.breaking-change.subtypes is not a valid subtype. *")]
	[Arguments("pivot:\n  subtypes:\n    nope: \">nope\"\n", Severity.Error, "Subtype 'nope' in pivot.subtypes is not a valid subtype. *")]
	[Arguments("pivot:\n  products:\n    nope: \">nope\"\n", Severity.Error, "Product 'nope' in pivot.products is not in the list of available products *")]
	[Arguments("products:\n  default:\n    - lifecycle: ga\n", Severity.Error, "Default product in products_config.default must have a product ID")]
	[Arguments("products:\n  default:\n    - product: nope\n", Severity.Error, "Product 'nope' in products_config.default is not in the list of available products from config/products.yml. *")]
	[Arguments("bundle:\n  repo: elasticsearch+kibana\n", Severity.Error, "bundle.repo must name a single GitHub repository. Remove '+' merged-repo syntax from bundle.repo.")]
	[Arguments("bundle:\n  profiles:\n    release:\n      repo: elasticsearch+kibana\n", Severity.Error, "bundle.profiles.release.repo must name a single GitHub repository. Remove '+' merged-repo syntax.")]
	[Arguments("bundle:\n  link_allow_repos:\n    - elastic\n", Severity.Error, "bundle.link_allow_repos: each entry must be exactly 'owner/repo' (one slash). Invalid: 'elastic'.")]
	[Arguments("bundle:\n  profiles:\n    release:\n      source: github_release\n", Severity.Warning, "bundle.profiles.release.source is deprecated. Use bundle.releases.github to map release tags to profiles instead.")]
	[Arguments("bundle:\n  profiles:\n    release:\n      product: elasticsearch\n  releases:\n    github:\n      \"v*\": \"\"\n", Severity.Error, "bundle.releases.github['v*']: must map to a non-empty profile name.")]
	[Arguments("bundle:\n  profiles:\n    release:\n      product: elasticsearch\n  releases:\n    github:\n      \"v*\": missing\n", Severity.Error, "bundle.releases.github['v*']: profile 'missing' is not defined in bundle.profiles.")]
	[Arguments("rules:\n  bundle:\n    include_products: elasticsearch\n    exclude_products: kibana\n", Severity.Error, "rules.bundle: cannot have both 'exclude_products' and 'include_products'. Use one or the other.")]
	[Arguments("rules:\n  bundle:\n    products:\n      elasticsearch:\n        match_products: all\n        include_products: elasticsearch\n        exclude_products: kibana\n", Severity.Error, "rules.bundle.products.elasticsearch: cannot have both 'exclude_products' and 'include_products'. Use one or the other.")]
	[Arguments("rules:\n  bundle:\n    products:\n      elasticsearch:\n        include_products: elasticsearch\n", Severity.Warning, "Configuration pattern 'match_products: any' with 'include_products' in per-product rule 'elasticsearch' provides no selective filtering. *")]
	[Arguments("rules:\n  bundle:\n    products:\n      elasticsearch:\n        match_products: all\n        include_products: [elasticsearch, kibana]\n", Severity.Hint, "Per-product rule 'elasticsearch' includes disjoint products [kibana] *")]
	[Arguments("filename: timestamp\n", Severity.Error, "filename: 'timestamp' is no longer supported. *")]
	[Arguments("filename: issue\n", Severity.Error, "filename: 'issue' is no longer supported. *")]
	public async Task LoadChangelogConfiguration_SingleFault_ReportsItsDiagnostic(string yaml, Severity severity, string message)
	{
		var ct = TestContext.Current!.Execution.CancellationToken;
		var configPath = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, yaml, ct);
		var loader = new ChangelogConfigurationLoader(LoggerFactory, ConfigurationContext, FileSystem);

		var config = (await loader.LoadRequired(configPath, ct)).EmitTo(Collector);

		Collector.Diagnostics.Should().ContainSingle();
		var diagnostic = Collector.Diagnostics.Single();
		diagnostic.Severity.Should().Be(severity);
		diagnostic.Message.Should().Match(message);
		diagnostic.File.Should().Be(configPath);
		if (severity == Severity.Error)
			config.Should().BeNull();
		else
			config.Should().NotBeNull();
	}

	private async Task<string> WriteConfig(string yaml)
	{
		var configPath = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, yaml, TestContext.Current!.Execution.CancellationToken);
		return configPath;
	}

	private ChangelogConfigurationLoader NewLoader() => new(LoggerFactory, ConfigurationContext, FileSystem);

	[Test]
	public async Task Load_AValidFile_IsAResultWithTheValueAndNoDiagnostics()
	{
		var ct = TestContext.Current!.Execution.CancellationToken;
		var configPath = await WriteConfig(RequiredTypes);

		var result = await NewLoader().Load(configPath, ct);

		result.Value.Should().NotBeNull();
		result.HasErrors.Should().BeFalse();
		result.Diagnostics.Should().BeEmpty();
		Collector.Diagnostics.Should().BeEmpty("a Result is returned, nothing is written to a collector until the caller asks");
	}

	[Test]
	public async Task Load_AnErrorIsNoValue_AndEveryDiagnosticRidesInTheResult()
	{
		var ct = TestContext.Current!.Execution.CancellationToken;
		var configPath = await WriteConfig(RequiredTypes + "    nope:\n");

		var result = await NewLoader().Load(configPath, ct);

		result.Value.Should().BeNull();
		result.Diagnostics.Should().ContainSingle(d => d.Severity == Severity.Error && d.File == configPath);
		Collector.Diagnostics.Should().BeEmpty();
	}

	[Test]
	public async Task Load_AWarningKeepsTheValue()
	{
		var ct = TestContext.Current!.Execution.CancellationToken;
		var configPath = await WriteConfig("bundle:\n  profiles:\n    release:\n      source: github_release\n");

		var result = await NewLoader().Load(configPath, ct);

		result.Value.Should().NotBeNull();
		result.Diagnostics.Should().ContainSingle(d => d.Severity == Severity.Warning);
	}

	[Test]
	public async Task Load_NoFile_IsTheCheckedDefault()
	{
		var result = await NewLoader().Load("/nowhere/changelog.yml", TestContext.Current!.Execution.CancellationToken);

		result.Value.Should().BeSameAs(ChangelogConfiguration.Default);
		result.Diagnostics.Should().BeEmpty();
	}

	[Test]
	public async Task LoadRequired_NoFile_IsAnError()
	{
		var result = await NewLoader().LoadRequired("/nowhere/changelog.yml", TestContext.Current!.Execution.CancellationToken);

		result.Value.Should().BeNull();
		result.Diagnostics.Should().ContainSingle(d => d.Severity == Severity.Error && d.Message.Contains("not found"));
	}

	[Test]
	public async Task Load_ReadsTheFileEveryTime_SoAnEditedFileIsSeen()
	{
		var ct = TestContext.Current!.Execution.CancellationToken;
		var configPath = await WriteConfig(RequiredTypes);
		var loader = NewLoader();
		(await loader.Load(configPath, ct)).Value.Should().NotBeNull();

		await FileSystem.File.WriteAllTextAsync(configPath, RequiredTypes + "    nope:\n", ct);

		(await loader.Load(configPath, ct)).Value.Should().BeNull();
	}
}
