// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Diagnostics;

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

		var config = await loader.LoadChangelogConfigurationRequired(Collector, configPath, ct);

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
}
