// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using AwesomeAssertions;
using Elastic.Documentation.Codex;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Links;
using Elastic.Documentation.Publishing;
using Elastic.Documentation.Serialization;
using static Elastic.Documentation.Configuration.Tests.Golden.Golden;

namespace Elastic.Documentation.Configuration.Tests.Golden;

/// <summary>
/// Loading the same input twice yields equal aggregates, and a change anywhere in an aggregate makes it unequal.
/// This is what value equality on the domain records buys: caches, change detection and property tests can compare
/// aggregates directly instead of projecting them.
/// </summary>
public class AggregateEqualityTests
{
	[Test]
	public void GlobalCatalog_LoadedTwice_IsEqual() =>
		CatalogLoader.Load(BaselineProvider()).OrThrow().Should().Be(CatalogLoader.Load(BaselineProvider()).OrThrow());

	[Test]
	public void GlobalCatalog_OneProductRenamed_IsNotEqual()
	{
		var catalog = CatalogLoader.Load(BaselineProvider()).OrThrow();
		var (id, product) = catalog.Products.Products.First();
		var renamed = catalog with
		{
			Products = catalog.Products with
			{
				Products = catalog
					.Products
					.Products
					.Select(kv => kv.Key == id ? new(kv.Key, product with { DisplayName = product.DisplayName + " (renamed)" }) : kv)
					.ToFrozenDictionary()
			}
		};

		renamed.Should().NotBe(catalog);
	}

	[Test]
	public void SearchTuning_LoadedTwice_IsEqual() =>
		SearchTuningLoader.Load(BaselineProvider()).OrThrow().Should().Be(SearchTuningLoader.Load(BaselineProvider()).OrThrow());

	[Test]
	public void RelatedLearning_LoadedTwice_IsEqual() =>
		RelatedLearningLoader.Load(BaselineProvider()).OrThrow().Should().Be(RelatedLearningLoader.Load(BaselineProvider()).OrThrow());

	private static IReadOnlyList<TocItem> LoadBaselineToc()
	{
		var fs = DocumentationSetGoldenTests.CreateDocsTree();
		var docsetFile = fs.FileInfo.New(Path.Join(DocumentationSetGoldenTests.DocsRoot, "_docset.yml"));
		return DocumentationSetLoader.LoadAndResolve(new CapturingDiagnosticsCollector(), docsetFile).TableOfContents;
	}

	[Test]
	public void TableOfContents_LoadedTwice_IsEqual() => LoadBaselineToc().Should().Equal(LoadBaselineToc());

	[Test]
	public void TableOfContents_OneNestedFileHidden_IsNotEqual()
	{
		var toc = LoadBaselineToc();
		var (nested, nestedAt) = FirstOfCase<TocItem, IsolatedTableOfContentsRef>(toc);
		var (file, fileAt) = FirstOfCase<TocItem, FileRef>(nested.Children);
		var children = nested.Children.ToList();
		children[fileAt] = new TocItem(file with { Hidden = !file.Hidden });
		var changed = toc.ToList();
		changed[nestedAt] = new TocItem(nested with { Children = children });

		changed.Should().NotEqual(toc);
	}

	private static ResolvedDocumentationSet ResolveBaselineDocset()
	{
		var catalog = CatalogLoader.Load(BaselineProvider()).OrThrow();
		var fs = DocumentationSetGoldenTests.CreateDocsTree();
		var docsetFile = fs.FileInfo.New(Path.Join(DocumentationSetGoldenTests.DocsRoot, "_docset.yml"));
		var docSet = DocumentationSetLoader.ParseDefinition(docsetFile, docsetFile.Directory!, fs).OrThrowIfMissing();
		return DocumentationSetResolver.Resolve(
			docSet,
			catalog.VersioningSystems,
			catalog.Products,
			new EmptyEnvironment()
		).OrThrowIfMissing();
	}

	private sealed class EmptyEnvironment : IEnvironmentVariables
	{
		public string? GetEnvironmentVariable(string name) => null;
		public bool IsRunningOnCI => false;
	}

	[Test]
	public void DocumentationSetDefinition_LoadedTwice_IsEqual()
	{
		DocumentationSetDefinition Load()
		{
			var fs = DocumentationSetGoldenTests.CreateDocsTree();
			return DocumentationSetLoader.ParseDefinition(
				fs.FileInfo.New(Path.Join(DocumentationSetGoldenTests.DocsRoot, "_docset.yml")),
				fs.DirectoryInfo.New(DocumentationSetGoldenTests.DocsRoot),
				fs
			).OrThrowIfMissing();
		}

		Load().Should().Be(Load());
	}

	[Test]
	public void ResolvedDocumentationSet_ResolvedTwice_IsEqual() => ResolveBaselineDocset().Should().Be(ResolveBaselineDocset());

	[Test]
	public void ResolvedDocumentationSet_OneSubstitutionChanged_IsNotEqual()
	{
		var docset = ResolveBaselineDocset();
		var (key, value) = docset.Substitutions.First();
		var changed = docset with
		{
			Substitutions = docset.Substitutions.ToDictionary(kv => kv.Key, kv => kv.Key == key ? value + " (changed)" : kv.Value)
		};

		changed.Should().NotBe(docset);
	}

	[Test]
	public void Assembly_LoadedTwice_IsEqual() =>
		AssemblyLoader.Load(BaselineProvider()).OrThrow().Should().Be(AssemblyLoader.Load(BaselineProvider()).OrThrow());

	[Test]
	public void Assembly_OneBranchChanged_IsNotEqual()
	{
		var assembly = AssemblyLoader.Load(BaselineProvider()).OrThrow();
		var (name, repository) = assembly.ReferenceRepositories.First();
		var changed = assembly with
		{
			ReferenceRepositories = assembly.ReferenceRepositories.ToDictionary(
				kv => kv.Key,
				kv => kv.Key == name ? repository with { GitReferenceCurrent = repository.GitReferenceCurrent + "-changed" } : kv.Value
			)
		};

		changed.Should().NotBe(assembly);
	}

	private static SiteNavigationDefinition LoadBaselineNavigation() =>
		SiteNavigationLoader.Deserialize(File.ReadAllText(BaselineFile("config", "navigation.yml")));

	[Test]
	public void SiteNavigation_LoadedTwice_IsEqual() => LoadBaselineNavigation().Should().Be(LoadBaselineNavigation());

	[Test]
	public void SiteNavigation_OneMountMoved_IsNotEqual()
	{
		var navigation = LoadBaselineNavigation();
		var (first, firstAt) = FirstOfCase<SiteNavigationEntry, SiteTableOfContentsRef>(navigation.TableOfContents);
		var entries = navigation.TableOfContents.ToList();
		entries[firstAt] = new SiteNavigationEntry(first with { PathPrefix = first.PathPrefix + "/moved" });
		var changed = navigation with { TableOfContents = entries };

		changed.Should().NotBe(navigation);
	}

	// The baseline codex.example.yml is deliberately invalid (it fails to load), so this uses a valid inline codex.yml.
	private static CodexConfiguration LoadBaselineCodex() =>
		CodexSiteLoader.Deserialize(
			"site_prefix: internal/\nenvironment: ' Internal '\ngroups:\n  - id: observability\n    name: Observability\n  - id: security\n    name: Security\n"
		);

	[Test]
	public void Codex_LoadedTwice_IsEqual() => LoadBaselineCodex().Should().Be(LoadBaselineCodex());

	[Test]
	public void Codex_OneGroupRenamed_IsNotEqual()
	{
		var codex = LoadBaselineCodex();
		var changed = codex with { Groups = [.. codex.Groups.Select((g, i) => i == 0 ? g with { Name = g.Name + " (renamed)" } : g)] };

		changed.Should().NotBe(codex);
	}

	[Test]
	public async Task ChangelogConfiguration_LoadedTwice_IsEqual()
	{
		var file = BaselineFile("changelog", "elasticsearch.changelog.yml");
		var first = await ChangelogConfigurationGoldenTests.LoadChangelog(file);
		var second = await ChangelogConfigurationGoldenTests.LoadChangelog(file);

		first.Should().NotBeNull();
		Elastic
			.Documentation
			.ReleaseNotes
			.ChangelogConfiguration
			.EqualityComparer
			.Default
			.Inequalities(first!, second!)
			.Select(i => i.ToString())
			.Should()
			.BeEmpty();
		first.Should().Be(second);
		(first! with { HighlightLabels = [.. first.HighlightLabels ?? [], "changed"] }).Should().NotBe(second);
	}

	private const string ManifestJson =
		"""
		{"origin":{"branch":"main","remote":"elastic/docs-builder","ref":"abc"},"url_path_prefix":"/docs","links":{"index.md":{"anchors":["a","b"]}},"cross_links":["kibana://index.md"],"redirects":{"old.md":{"to":"index.md","anchors":{"!":"!"}}}}
		""";

	[Test]
	public void LinkManifest_ReadTwice_IsEqual() =>
		LinkLoader.LoadManifest(ManifestJson, "links.json").Value!.Should().Be(LinkLoader.LoadManifest(ManifestJson, "links.json").Value!);

	[Test]
	public void LinkManifest_OneAnchorAdded_IsNotEqual()
	{
		var manifest = LinkLoader.LoadManifest(ManifestJson, "links.json").Value!;
		var changed = manifest with { Links = new Dictionary<string, LinkMetadata> { ["index.md"] = new() { Anchors = ["a", "b", "c"] } } };

		changed.Should().NotBe(manifest);
	}

	// Replaces by position: the union's Value is an object, so == would compare references and depend on instance identity.
	private static (TCase Item, int Index) FirstOfCase<TUnion, TCase>(IReadOnlyList<TUnion> items) where TUnion : IUnion where TCase : class
	{
		for (var i = 0; i < items.Count; i++)
		{
			if (items[i].Value is TCase match)
				return (match, i);
		}
		throw new InvalidOperationException($"The baseline has no {typeof(TCase).Name}.");
	}
}
