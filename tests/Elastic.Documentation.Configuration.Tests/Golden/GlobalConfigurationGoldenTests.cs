// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using System.Text.Json.Nodes;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Codex;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.RelatedLearning;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Publishing;
using Elastic.Documentation.Search;
using Microsoft.Extensions.Logging.Abstractions;
using static Elastic.Documentation.Configuration.Tests.Golden.Golden;

namespace Elastic.Documentation.Configuration.Tests.Golden;

/// <summary>
/// Golden snapshots for the global configuration in <c>Baseline/config/</c>: what today's loaders produce,
/// as values and failures. The domain-model refactor must reproduce each snapshot exactly.
/// </summary>
public class GlobalConfigurationGoldenTests
{
	[Test]
	public async Task Versions_Baseline_MatchesGolden()
	{
		var provider = BaselineProvider();
		await AssertMatches("config-versions", Capture(() => ProjectVersions(CatalogLoader.Load(provider).OrThrow().VersioningSystems)));
	}

	[Test]
	public async Task Products_Baseline_MatchesGolden()
	{
		var provider = BaselineProvider();
		await AssertMatches("config-products", Capture(() => ProjectProducts(CatalogLoader.Load(provider).OrThrow().Products)));
	}

	[Test]
	public async Task LegacyUrlMappings_Baseline_MatchesGolden()
	{
		var provider = BaselineProvider();
		await AssertMatches(
			"config-legacy-url-mappings",
			Capture(() => ProjectLegacyUrlMappings(CatalogLoader.Load(provider).OrThrow().LegacyUrlMappings))
		);
	}

	[Test]
	public async Task RelatedLearning_Baseline_MatchesGolden()
	{
		var provider = BaselineProvider();
		await AssertMatches(
			"config-related-learning",
			Capture(() => ProjectRelatedLearning(RelatedLearningLoader.Load(provider).OrThrow()))
		);
	}

	[Test]
	public async Task Search_Baseline_MatchesGolden()
	{
		var provider = BaselineProvider();
		await AssertMatches("config-search", Capture(() => ProjectSearch(SearchTuningLoader.Load(provider).OrThrow())));
	}

	[Test]
	[Arguments(false)]
	[Arguments(true)]
	public async Task Assembly_Baseline_MatchesGolden(bool skipPrivateRepositories)
	{
		var provider = BaselineProvider(skipPrivateRepositories);
		var name = skipPrivateRepositories ? "config-assembler-skip-private" : "config-assembler";
		await AssertMatches(name, Capture(() => ProjectAssembly(AssemblyLoader.Create(provider))));
	}

	[Test]
	[Arguments("navigation.yml")]
	[Arguments("navigation_preview.yml")]
	public async Task SiteNavigation_Baseline_MatchesGolden(string file)
	{
		var yaml = await File.ReadAllTextAsync(BaselineFile("config", file));
		var name = "config-" + Path.GetFileNameWithoutExtension(file).Replace('_', '-');
		await AssertMatches(name, Capture(() => ProjectSiteNavigationWithValidation(yaml, file)));
	}

	[Test]
	public async Task SiteNavigation_FilteredForPrivateRepositories_MatchesGolden()
	{
		var provider = BaselineProvider(skipPrivateRepositories: true);
		await AssertMatches("config-navigation-filtered", Capture(() =>
		{
			var assembly = AssemblyLoader.Create(provider);
			var navigation = SiteNavigationLoader.Deserialize(
				provider.NavigationFile.FileSystem.File.ReadAllText(provider.NavigationFile.FullName)
			);
			return ProjectSiteNavigationWithValidation(
				SiteNavigationLoader.ForAssembly(navigation, assembly, skipPrivateRepositories: true),
				"navigation.yml"
			);
		}));
	}

	[Test]
	[Arguments(false)]
	[Arguments(true)]
	public async Task NavigationTocMappings_Baseline_MatchesGolden(bool skipPrivateRepositories)
	{
		var provider = BaselineProvider(skipPrivateRepositories);
		var name = skipPrivateRepositories ? "config-navigation-toc-mappings-filtered" : "config-navigation-toc-mappings";
		await AssertMatches(name, Capture(() =>
		{
			var assembly = AssemblyLoader.Create(provider);
			var file = provider.NavigationFile;
			var navigation = SiteNavigationLoader.ForAssembly(
				SiteNavigationLoader.Deserialize(file.FileSystem.File.ReadAllText(file.FullName)),
				assembly,
				skipPrivateRepositories
			);
			var collector = new CapturingDiagnosticsCollector();
			var mappings = SiteNavigationLoader.GetTocMappings(navigation, file, collector);
			return Obj(
				("mappings", Map(
					mappings.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
					m => Obj(("source", Str(m.Source.ToString())), ("pathPrefix", Str(m.SourcePathPrefix)))
				)),
				("diagnostics", DiagnosticList(collector.Diagnostics))
			);
		}));
	}

	[Test]
	public async Task Codex_Example_MatchesGolden()
	{
		var yaml = await File.ReadAllTextAsync(BaselineFile("config", "codex.example.yml"));
		await AssertMatches("config-codex-example", Capture(() => ProjectCodex(CodexSiteLoader.Deserialize(yaml))));
	}

	// ── projections ──────────────────────────────────────────────────────────

	private static JsonNode ProjectVersioningSystem(VersioningSystem v) =>
		Obj(
			("id", Str(v.Id.ToString())),
			("base", Str(v.Base.ToString())),
			("current", Str(v.Current.ToString())),
			("isVersioned", Bool(v.IsVersioned())),
			("isVersionless", Bool(v.IsVersionless))
		);

	private static JsonNode ProjectVersions(VersioningSystemCatalog versions) =>
		Obj(("versioningSystems", Map(versions.VersioningSystems, ProjectVersioningSystem)));

	private static JsonNode ProjectProduct(Product p) =>
		Obj(
			("id", Str(p.Id)),
			("displayName", Str(p.DisplayName)),
			("versioningSystem", Str(p.VersioningSystem?.Id.ToString())),
			("repository", Str(p.Repository)),
			("features", Obj(
				("publicReference", Bool(p.Features.PublicReference)),
				("releaseNotes", Str(p.Features.ReleaseNotes.ToString()))
			))
		);

	private static JsonNode ProjectProducts(ProductCatalog products) =>
		Obj(
			("products", Map(products.Products, ProjectProduct)),
			("publicReferenceProducts", Set(products.PublicReferenceProducts.Keys)),
			("displayNames", Map(products.ProductDisplayNames, n => Str(n)))
		);

	private static JsonNode ProjectLegacyUrlMappings(LegacyUrlMappingCatalog legacy) =>
		Obj(
			("mappings", Arr(
				legacy.Mappings,
				m => Obj(
					("baseUrl", Str(m.BaseUrl)),
					("product", Str(m.Product.Id)),
					("legacyVersions", Arr(m.LegacyVersions, v => Str(v)))
				)
			))
		);

	private static JsonNode ProjectRelatedLearning(RelatedLearningCatalog related) =>
		Obj(("links", Map(related.Links, l => Obj(("id", Str(l.Id)), ("title", Str(l.Title)), ("url", Str(l.Url))))));

	private static JsonNode ProjectSearch(SearchTuning search) =>
		Obj(
			("synonyms", Arr(search.Synonyms, group => Arr(group.Terms, s => Str(s)))),
			("synonymBiDirectional", Map(search.SynonymBiDirectional, targets => Arr(targets, t => Str(t)))),
			("rules", Arr(
				search.Rules,
				r => Obj(
					("ruleId", Str(r.RuleId)),
					("type", Str(r.Type.ToString())),
					("criteria", Arr(
						r.Criteria,
						c => Obj(("type", Str(c.Type.ToString())), ("metadata", Str(c.Metadata)), ("values", Arr(c.Values, v => Str(v))))
					)),
					("actions", Obj(("ids", Arr(r.Actions.Ids, i => Str(i)))))
				)
			)),
			("diminishTerms", Arr(search.DiminishTerms, t => Str(t)))
		);

	/// <summary>Origins default from environment variables (CI, GitHub Actions); normalize the three default forms.</summary>
	private static string? NormalizeOrigin(string name, string? origin) =>
		origin == $"git@github.com:elastic/{name}.git"
			|| origin == $"https://github.com/elastic/{name}.git"
			|| (origin?.StartsWith("https://oauth2:", StringComparison.Ordinal) == true
				&& origin.EndsWith($"@github.com/elastic/{name}.git", StringComparison.Ordinal))
			? $"<default-origin:elastic/{name}>"
			: origin;

	private static JsonNode ProjectRepository(Repository r) =>
		Obj(
			("name", Str(r.Name)),
			("origin", Str(NormalizeOrigin(r.Name, r.Origin))),
			("checkoutStrategy", Str(r.CheckoutStrategy.ToString())),
			("skip", Bool(r.Skip)),
			("path", Str(r.Path is null ? null : NormalizePath(r.Path))),
			("current", Str(r.GitReferenceCurrent)),
			("next", Str(r.GitReferenceNext)),
			("edge", Str(r.GitReferenceEdge)),
			("sparsePaths", Arr(r.SparsePaths, p => Str(p))),
			("private", Bool(r.Private)),
			("cloneTimeout", Str(r.CloneTimeout?.ToString("c")))
		);

	private static JsonNode ProjectEnvironment(PublishEnvironment e) =>
		Obj(
			("name", Str(e.Name)),
			("uri", Str(e.Uri)),
			("pathPrefix", Str(e.PathPrefix)),
			("allowIndexing", Bool(e.AllowIndexing)),
			("contentSource", Str(e.ContentSource.ToString())),
			("googleTagManager", Obj(
				("enabled", Bool(e.GoogleTagManager.Enabled)),
				("id", Str(e.GoogleTagManager.Id)),
				("auth", Str(e.GoogleTagManager.Auth)),
				("preview", Str(e.GoogleTagManager.Preview)),
				("cookiesWin", Str(e.GoogleTagManager.CookiesWin))
			)),
			("optimizely", Obj(("enabled", Bool(e.Optimizely.Enabled)), ("id", Str(e.Optimizely.Id)))),
			("featureFlags", Map(e.FeatureFlags, f => Bool(f))),
			("websiteSearchScriptUrl", Str(e.WebsiteSearchScriptUrl))
		);

	private static JsonNode ProjectAssembly(AssemblyConfiguration a) =>
		Obj(
			("narrative", ProjectRepository(a.Narrative)),
			("referenceRepositories", Map(a.ReferenceRepositories, ProjectRepository)),
			("availableRepositories", Set(a.AvailableRepositories.Keys)),
			("privateRepositories", Set(a.PrivateRepositories.Keys)),
			("allPrivateRepositoryNames", Set(a.AllPrivateRepositoryNames)),
			("environments", Map(a.Environments, ProjectEnvironment)),
			("sharedConfigurations", Set(a.SharedConfigurations.Keys))
		);

	private static JsonNode ProjectTocRef(SiteTableOfContentsRef r) =>
		Obj(
			("kind", Str("mount")),
			("source", Str(r.Source.ToString())),
			("pathPrefix", Str(r.PathPrefix)),
			("island", Bool(r.Island)),
			("navigationTitle", Str(r.NavigationTitle)),
			("children", Arr(r.Children, ProjectTocRef))
		);

	private static JsonNode ProjectSiteEntry(SiteNavigationEntry entry) => entry switch
	{
		SiteTableOfContentsRef mount => ProjectTocRef(mount),
		SiteSectionRef section =>
			Obj(
				("kind", Str("section")),
				("title", Str(section.Title)),
				("externalUrl", Str(section.ExternalUrl)),
				("dropdownLinks", Arr(section.DropdownLinks, l => Obj(("title", Str(l.Title)), ("url", Str(l.Url))))),
				("children", Arr(section.Children, ProjectTocRef))
			),
		_ => Obj(("kind", Str("unknown:" + entry.GetType().Name)))
	};

	private static JsonNode ProjectSiteNavigationWithValidation(string yaml, string fileName) =>
		ProjectSiteNavigationWithValidation(SiteNavigationLoader.Deserialize(yaml), fileName);

	private static JsonNode ProjectSiteNavigationWithValidation(SiteNavigationDefinition navigation, string fileName)
	{
		var collector = new CapturingDiagnosticsCollector();
		var file = new MockFileSystem().FileInfo.New(Path.Join(BaselineDirectory, "config", fileName));
		var valid = SiteNavigationLoader.Validate(collector, navigation, file);
		return Obj(
			("phantoms", Arr(navigation.Phantoms, p => Str(p.Source))),
			("toc", Arr(navigation.TableOfContents, ProjectSiteEntry)),
			("declaredSources", Set(SiteNavigationDefinition.GetAllDeclaredSources(navigation).Select(u => u.ToString()))),
			("phantomPrefixes", Set(SiteNavigationDefinition.GetPhantomPrefixes(navigation).Select(u => u.ToString()))),
			("pathPrefixesValid", Bool(valid)),
			("pathPrefixDiagnostics", DiagnosticList(collector.Diagnostics))
		);
	}

	private static JsonNode ProjectCodex(CodexConfiguration c) =>
		Obj(
			("sitePrefix", Str(c.SitePrefix)),
			("environment", Str(c.Environment)),
			("title", Str(c.Title)),
			("groups", Arr(
				c.Groups,
				g => Obj(("id", Str(g.Id)), ("name", Str(g.Name)), ("description", Str(g.Description)), ("icon", Str(g.Icon)))
			)),
			("canonicalBaseUrl", Str(c.CanonicalBaseUrl))
		);
}
