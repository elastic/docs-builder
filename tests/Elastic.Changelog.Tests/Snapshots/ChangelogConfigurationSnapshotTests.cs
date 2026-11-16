// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Tests.Changelogs;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Testing;
using Nullean.Snapshots.Json;

namespace Elastic.Changelog.Tests.Snapshots;

/// <summary>
/// One snapshot per changelog.yml shape: the loaded configuration, the diagnostics in order, and the exception when loading
/// throws. Locks in what the loader does today, including inputs it is lenient about.
/// </summary>
public class ChangelogConfigurationSnapshotTests : ChangelogTestBase
{
	private const string ConfigName = "changelog.yml";

	private async Task<string> LoadAsync(string yaml)
	{
		var docsDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs");
		FileSystem.Directory.CreateDirectory(docsDir);
		var configPath = FileSystem.Path.Join(docsDir, ConfigName);
		await FileSystem.File.WriteAllTextAsync(configPath, yaml, TestContext.Current!.Execution.CancellationToken);

		var loader = new ChangelogConfigurationLoader(LoggerFactory, ConfigurationContext, FileSystem);
		ChangelogConfiguration? config = null;
		Exception? exception = null;
		try
		{
			config = await loader.LoadChangelogConfiguration(Collector, configPath, TestContext.Current!.Execution.CancellationToken);
		}
		catch (Exception e)
		{
			exception = e;
		}

		var json = ChangelogConfigurationDump.ToJson(config, Collector.Diagnostics, exception);
		var root = System
			.Text
			.RegularExpressions
			.Regex
			.Replace(
				Paths.WorkingDirectoryRoot.FullName.Replace('\\', '/'),
				"^[A-Za-z]:",
				"",
				System.Text.RegularExpressions.RegexOptions.None,
				RegexTimeouts.MatchTimeout
			);
		return json.Replace(root, "{root}", StringComparison.Ordinal);
	}

	[Test]
	public async Task NoFile_UsesDefaults()
	{
		var loader = new ChangelogConfigurationLoader(LoggerFactory, ConfigurationContext, FileSystem);
		var config = await loader.LoadChangelogConfiguration(
			Collector,
			"/nowhere/changelog.yml",
			TestContext.Current!.Execution.CancellationToken
		);
		ChangelogConfigurationDump.ToJson(config, Collector.Diagnostics).Should().MatchJsonSnapshot();
	}

	[Test]
	public async Task ExampleFileShippedWithTheRepo()
	{
		using var stream =
			typeof(ChangelogConfigurationSnapshotTests).Assembly.GetManifestResourceStream("Elastic.Changelog.Tests.changelog.example.yml")!;
		using var reader = new StreamReader(stream);
		var yaml = await reader.ReadToEndAsync(TestContext.Current!.Execution.CancellationToken);
		(await LoadAsync(yaml)).Should().MatchJsonSnapshot();
	}

	[Test]
	public async Task EmptyFile() => (await LoadAsync("")).Should().MatchJsonSnapshot();

	[Test]
	public async Task CommentsOnly() => (await LoadAsync("""
			# nothing here
			""")).Should().MatchJsonSnapshot();

	[Test]
	public async Task EmptyMapping() => (await LoadAsync("""
			{}
			""")).Should().MatchJsonSnapshot();

	[Test]
	public async Task LifecyclesOnly() =>
		(await LoadAsync("""
			lifecycles:
			  - ga
			""")).Should().MatchJsonSnapshot();

	[Test]
	public async Task LifecyclesCommaString() =>
		(await LoadAsync("""
			lifecycles: "preview, ga"
			""")).Should().MatchJsonSnapshot();

	[Test]
	public async Task LifecyclesMixedCase() =>
		(await LoadAsync("""
			lifecycles: [Beta, GA, Experimental]
			""")).Should().MatchJsonSnapshot();

	[Test]
	public async Task LifecyclesInvalid() =>
		(await LoadAsync("""
			lifecycles: [ga, stable]
			""")).Should().MatchJsonSnapshot();

	[Test]
	public async Task LifecyclesEmptyList() => (await LoadAsync("""
			lifecycles: []
			""")).Should().MatchJsonSnapshot();

	[Test]
	public async Task PivotTypesStringAndListLabels() =>
		(await LoadAsync(
			"""
			pivot:
			  types:
			    feature: ">feature, >new"
			    bug-fix:
			      - ">bug"
			      - ">fix"
			    breaking-change: ">breaking"
			    docs:
			    other: ""
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task PivotBreakingChangeObjectWithSubtypes() =>
		(await LoadAsync(
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			      labels: ">breaking, >bc"
			      subtypes:
			        api: ">api-breaking"
			        behavioral:
			          - ">behavioral"
			          - ">behaviour"
			        other:
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task PivotTypeNotAValidType() =>
		(await LoadAsync(
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			    chore: ">chore"
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task PivotMissingRequiredType() =>
		(await LoadAsync(
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task PivotSubtypesOnNonBreakingType() =>
		(await LoadAsync(
			"""
			pivot:
			  types:
			    feature:
			      labels: ">feature"
			      subtypes:
			        api: ">api"
			    bug-fix:
			    breaking-change:
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task PivotInvalidSubtypeUnderBreakingChange() =>
		(await LoadAsync(
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			      subtypes:
			        cosmetic: ">cosmetic"
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task PivotSubtypesSection() =>
		(await LoadAsync(
			"""
			pivot:
			  subtypes:
			    api: ">api"
			    security:
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task PivotInvalidSubtypesSection() =>
		(await LoadAsync(
			"""
			pivot:
			  subtypes:
			    cosmetic: ">cosmetic"
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task PivotAreasWithCommasAndLists() =>
		(await LoadAsync(
			"""
			pivot:
			  areas:
			    "Search, Ranking": ":Search/Ranking"
			    Security:
			      - ":Security/Auth"
			      - "Team:Security"
			    Empty:
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task PivotAreasLabelRepeatedForTwoAreas() =>
		(await LoadAsync(
			"""
			pivot:
			  areas:
			    Search:
			      - ":Search/Search"
			      - "Team:Search"
			    Observability:
			      - "Team:Search"
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task PivotProductsSpecs() =>
		(await LoadAsync(
			"""
			pivot:
			  products:
			    elasticsearch: ":stack/elasticsearch"
			    "kibana 9.2.0": [":stack/kibana"]
			    "cloud-serverless 2025-06 ga": ":example/serverless"
			    cloud_hosted: ":example/hosted"
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task PivotProductsUnknownProduct() =>
		(await LoadAsync(
			"""
			pivot:
			  products:
			    elasticsearch: ":stack/elasticsearch"
			    example-missing-product: ":example"
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task PivotFeatures() =>
		(await LoadAsync(
			"""
			pivot:
			  features:
			    "feature:new-search-api":
			      - "feature-flag:new-search-api"
			      - ":Feature/NewSearchApi"
			    "feature:other": ":Feature/Other"
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task PivotHighlightString() =>
		(await LoadAsync(
			"""
			pivot:
			  highlight: ">highlight, >release-highlight"
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task PivotHighlightList() =>
		(await LoadAsync(
			"""
			pivot:
			  highlight:
			    - ">highlight"
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task PivotRequiredAreasAndProducts() =>
		(await LoadAsync(
			"""
			pivot:
			  required: [areas, products]
			  areas:
			    Search: ":Search"
			  products:
			    elasticsearch: ":es"
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task PivotRequiredTypesIsNoop() =>
		(await LoadAsync("""
			pivot:
			  required: types
			""")).Should().MatchJsonSnapshot();

	[Test]
	public async Task PivotRequiredAreasWithoutLabels() =>
		(await LoadAsync("""
			pivot:
			  required: [areas]
			""")).Should().MatchJsonSnapshot();

	[Test]
	public async Task PivotRequiredUnknownPivot() =>
		(await LoadAsync("""
			pivot:
			  required: [owners]
			""")).Should().MatchJsonSnapshot();

	[Test]
	public async Task PivotEmptyMapping() => (await LoadAsync("""
			pivot: {}
			""")).Should().MatchJsonSnapshot();

	[Test]
	public async Task ProductsAvailableList() =>
		(await LoadAsync(
			"""
			products:
			  available: [elasticsearch, kibana]
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task ProductsAvailableCommaStringWithUnderscore() =>
		(await LoadAsync(
			"""
			products:
			  available: "elasticsearch, cloud_hosted"
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task ProductsAvailableEmptyList() =>
		(await LoadAsync("""
			products:
			  available: []
			""")).Should().MatchJsonSnapshot();

	[Test]
	public async Task ProductsAvailableUnknownProduct() =>
		(await LoadAsync(
			"""
			products:
			  available: [elasticsearch, example-missing-product]
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task ProductsDefault() =>
		(await LoadAsync(
			"""
			products:
			  default:
			    - product: elasticsearch
			      lifecycle: beta
			    - product: kibana
			    - product: cloud_serverless
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task ProductsDefaultUnknownProduct() =>
		(await LoadAsync(
			"""
			products:
			  default:
			    - product: example-missing-product
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task ProductsDefaultWithoutProductId() =>
		(await LoadAsync(
			"""
			products:
			  default:
			    - lifecycle: ga
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task ProductsImplies() =>
		(await LoadAsync(
			"""
			pivot:
			  products:
			    security: ":sec"
			products:
			  implies:
			    security: [kibana, cloud-serverless]
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task ProductsImpliesWithoutMappedLabel() =>
		(await LoadAsync(
			"""
			products:
			  implies:
			    security: [kibana]
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task ProductsImpliesUnknownProduct() =>
		(await LoadAsync(
			"""
			pivot:
			  products:
			    security: ":sec"
			products:
			  implies:
			    security: [example-missing-product]
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task ExtractAllDisabled() =>
		(await LoadAsync(
			"""
			extract:
			  release_notes: false
			  issues: false
			  strip_title_prefix: true
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task ExtractPartial() =>
		(await LoadAsync("""
			extract:
			  strip_title_prefix: true
			""")).Should().MatchJsonSnapshot();

	[Test]
	public async Task ExtractBadBoolean() =>
		(await LoadAsync("""
			extract:
			  release_notes: sometimes
			""")).Should().MatchJsonSnapshot();

	[Test]
	public async Task FilenamePr() => (await LoadAsync("""
			filename: pr
			""")).Should().MatchJsonSnapshot();

	[Test]
	public async Task FilenameUpperCase() => (await LoadAsync("""
			filename: PR
			""")).Should().MatchJsonSnapshot();

	[Test]
	public async Task FilenameTimestamp() =>
		(await LoadAsync("""
			filename: timestamp
			""")).Should().MatchJsonSnapshot();

	[Test]
	public async Task FilenameIssue() => (await LoadAsync("""
			filename: issue
			""")).Should().MatchJsonSnapshot();

	[Test]
	public async Task FilenameUnknown() =>
		(await LoadAsync("""
			filename: sequential
			""")).Should().MatchJsonSnapshot();

	[Test]
	public async Task FilenameBlank() => (await LoadAsync("""
			filename: ""
			""")).Should().MatchJsonSnapshot();

	[Test]
	public async Task BlockKeyFromOlderVersions() =>
		(await LoadAsync(
			"""
			block:
			  create:
			    exclude: ">non-issue"
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task UnknownTopLevelKey() =>
		(await LoadAsync("""
			lifecycles: [ga]
			example_unknown: true
			""")).Should().MatchJsonSnapshot();

	[Test]
	public async Task UnknownNestedKeys() =>
		(await LoadAsync(
			"""
			extract:
			  release_notes: true
			  example_unknown: 1
			pivot:
			  example_nested:
			    a: b
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task TopLevelIsAList() =>
		(await LoadAsync("""
			- a
			- b
			""")).Should().MatchJsonSnapshot();

	[Test]
	public async Task TopLevelIsAScalar() => (await LoadAsync("""
			just a string
			""")).Should().MatchJsonSnapshot();

	[Test]
	public async Task InvalidYamlSyntax() =>
		(await LoadAsync("""
			pivot: [unclosed
			""")).Should().MatchJsonSnapshot();

	[Test]
	public async Task PivotTypesIsAList() =>
		(await LoadAsync(
			"""
			pivot:
			  types:
			    - feature
			    - bug-fix
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task RulesMatchAll() =>
		(await LoadAsync("""
			rules:
			  match: all
			""")).Should().MatchJsonSnapshot();

	[Test]
	public async Task RulesMatchInvalid() =>
		(await LoadAsync("""
			rules:
			  match: some
			""")).Should().MatchJsonSnapshot();

	[Test]
	public async Task RulesCreateExcludeAndInclude() =>
		(await LoadAsync(
			"""
			rules:
			  create:
			    exclude: ">non-issue, >test"
			    match: all
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task RulesCreateInclude() =>
		(await LoadAsync(
			"""
			rules:
			  create:
			    include: [">changelog"]
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task RulesCreateByProduct() =>
		(await LoadAsync(
			"""
			rules:
			  create:
			    exclude: ">test"
			    products:
			      "elasticsearch, kibana":
			        exclude: ">test-es"
			      cloud-serverless:
			        include: "ILM"
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task RulesCreateExcludeAndIncludeTogether() =>
		(await LoadAsync(
			"""
			rules:
			  create:
			    exclude: ">a"
			    include: ">b"
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task RulesCreateUnknownProduct() =>
		(await LoadAsync(
			"""
			rules:
			  create:
			    products:
			      example-missing-product:
			        exclude: ">x"
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task RulesBundleGlobalContent() =>
		(await LoadAsync(
			"""
			rules:
			  bundle:
			    match_products: conjunction
			    include_products: [elasticsearch, kibana]
			    exclude_types: "deprecation, known-issue"
			    exclude_areas: [Search]
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task RulesBundlePerProduct() =>
		(await LoadAsync(
			"""
			rules:
			  bundle:
			    exclude_types: [docs]
			    products:
			      security:
			        exclude_products: [kibana]
			        exclude_types: [docs]
			        include_areas: [APM, "Infrastructure monitoring"]
			      kibana:
			        match_products: all
			        include_products: [kibana, security]
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task RulesBundleInvalidMatchProducts() =>
		(await LoadAsync(
			"""
			rules:
			  bundle:
			    match_products: sometimes
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task RulesBundleUnknownProduct() =>
		(await LoadAsync(
			"""
			rules:
			  bundle:
			    include_products: [example-missing-product]
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task RulesBundleIncludeAndExcludeTypes() =>
		(await LoadAsync(
			"""
			rules:
			  bundle:
			    include_types: [feature]
			    exclude_types: [docs]
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task RulesPublishWarnsAndIsIgnored() =>
		(await LoadAsync(
			"""
			rules:
			  publish:
			    exclude_types: [docs]
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task BundleDirectories() =>
		(await LoadAsync(
			"""
			bundle:
			  directory: docs/changelog
			  output_directory: docs/releases
			  use_local_changelogs: true
			  description: |
			    Line one {version}
			    Line two
			  branch: main
			  release_dates: false
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task BundleProfilesFull() =>
		(await LoadAsync(
			"""
			bundle:
			  profiles:
			    es-release:
			      product: elasticsearch
			      products: "elasticsearch {version} {lifecycle}"
			      description: "ES {version}"
			      release_dates: false
			      branch: 9.x
			      hide_features:
			        - flag-1
			        - flag-2
			    serverless:
			      product: cloud_serverless
			      hide_features: "flag-3, flag-4"
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task BundleProfilesDeprecatedFields() =>
		(await LoadAsync(
			"""
			bundle:
			  profiles:
			    old:
			      output: "{repo}-{version}.yaml"
			      output_products: "kibana {version}, security {version}"
			      output_directory: docs/releases/old
			      repo: example-repo
			      owner: example-owner
			      source: github_release
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task BundleProfileUnknownProduct() =>
		(await LoadAsync(
			"""
			bundle:
			  profiles:
			    broken:
			      product: example-missing-product
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task BundleProfileEmptyMapping() =>
		(await LoadAsync(
			"""
			bundle:
			  profiles:
			    nothing: {}
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task BundleLegacyRepoOwner() =>
		(await LoadAsync(
			"""
			bundle:
			  repo: example-repo
			  owner: example-owner
			  resolve: true
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task BundleLinkAllowRepos() =>
		(await LoadAsync(
			"""
			bundle:
			  link_allow_repos:
			    - example-owner/example-repo
			    - elastic/kibana
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task BundleLinkAllowReposInvalidEntry() =>
		(await LoadAsync(
			"""
			bundle:
			  link_allow_repos:
			    - not-owner-slash-repo
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task BundleBackportsLabels() =>
		(await LoadAsync(
			"""
			bundle:
			  backports:
			    labels: [backport-8.x, "backport-9.0"]
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task BundleBackportsGlobLabel() =>
		(await LoadAsync(
			"""
			bundle:
			  backports:
			    labels: ["backport*"]
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task BundleReleases() =>
		(await LoadAsync(
			"""
			bundle:
			  profiles:
			    es:
			      product: elasticsearch
			    serverless:
			      product: cloud-serverless
			  releases:
			    github:
			      "v9.*": es
			    products:
			      cloud-serverless: serverless
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task BundleReleasesUnknownProfile() =>
		(await LoadAsync(
			"""
			bundle:
			  profiles:
			    es:
			      product: elasticsearch
			  releases:
			    github:
			      "v9.*": missing
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task BundleReleasesEmptyProfileName() =>
		(await LoadAsync(
			"""
			bundle:
			  releases:
			    github:
			      "v9.*": ""
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task BundleReleasesUnknownProduct() =>
		(await LoadAsync(
			"""
			bundle:
			  profiles:
			    es:
			      product: elasticsearch
			  releases:
			    products:
			      example-missing-product: es
			"""
		)).Should().MatchJsonSnapshot();

	[Test]
	public async Task BundleUnknownKey() =>
		(await LoadAsync("""
			bundle:
			  example_unknown: 1
			""")).Should().MatchJsonSnapshot();

	[Test]
	public async Task AllSectionsTogether() =>
		(await LoadAsync(
			"""
			filename: pr
			lifecycles: [preview, ga]
			products:
			  available: [elasticsearch, kibana, security]
			  default:
			    - product: elasticsearch
			pivot:
			  types:
			    feature: ">feature"
			    bug-fix: ">bug"
			    breaking-change:
			      labels: ">breaking"
			      subtypes:
			        api: ">api"
			  areas:
			    Search: ":Search"
			  products:
			    elasticsearch: ":es"
			    kibana: ":kb"
			  features:
			    "feature:x": "flag:x"
			  highlight: ">highlight"
			  required: [areas]
			rules:
			  create:
			    exclude: ">test"
			extract:
			  strip_title_prefix: true
			bundle:
			  directory: docs/changelog
			  profiles:
			    es:
			      product: elasticsearch
			  releases:
			    github:
			      "v9.*": es
			"""
		)).Should().MatchJsonSnapshot();
}
