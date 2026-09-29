// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.DocumentationSets;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json.Nodes;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Toc.CliReference;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Links;
using static Elastic.Documentation.Configuration.Tests.Golden.Golden;

namespace Elastic.Documentation.Configuration.Tests.Golden;

/// <summary>
/// Golden snapshot for docs-builder's own docset in <c>Baseline/docset/</c> (<c>_docset.yml</c>, every nested
/// <c>toc.yml</c>, <c>_redirects.yml</c>), resolved against the baseline catalog. The file tree is the frozen
/// <c>files.txt</c> listing; files other than the configuration files are empty stand-ins.
/// </summary>
public class DocumentationSetGoldenTests
{
	internal const string DocsRoot = "/docs";

	internal static MockFileSystem CreateDocsTree()
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>(), DocsRoot);
		var docsetDirectory = BaselineFile("docset");
		foreach (var relative in File.ReadAllLines(Path.Join(docsetDirectory, "files.txt")).Where(l => l.Length > 0))
		{
			var baselineCopy = Path.Join(docsetDirectory, relative);
			var content = relative != "files.txt" && File.Exists(baselineCopy) ? File.ReadAllText(baselineCopy) : string.Empty;
			fs.AddFile(Path.Join(DocsRoot, relative), new MockFileData(content));
		}
		return fs;
	}

	[Test]
	public async Task DocumentationSet_Baseline_MatchesGolden()
	{
		var provider = BaselineProvider();
		var catalog = CatalogLoader.Load(provider).OrThrow();
		var versions = catalog.VersioningSystems;
		var products = catalog.Products;

		var fs = CreateDocsTree();
		var docsetFile = fs.FileInfo.New(Path.Join(DocsRoot, "_docset.yml"));
		var collector = new CapturingDiagnosticsCollector();

		var snapshot = Capture(() =>
		{
			var docSet = DocumentationSetFile.LoadAndResolve(collector, docsetFile);
			var context = new GoldenDocumentationSetContext(collector, fs, docsetFile, docsetFile.Directory!);
			var configuration = new ConfigurationFile(docSet, context, versions, products);
			return Obj(("docset", ProjectDocumentationSetFile(docSet)), ("configuration", ProjectConfigurationFile(configuration)));
		});
		snapshot["diagnostics"] = DiagnosticList(collector.Diagnostics.Select(NormalizeDocsRoot));
		await AssertMatches("docset-docs-builder", snapshot);
	}

	private static Diagnostic NormalizeDocsRoot(Diagnostic d) =>
		d with
		{
			File = d.File.Replace(DocsRoot, "<docs>", StringComparison.Ordinal),
			Message = d.Message.Replace(DocsRoot + "/", "<docs>/", StringComparison.Ordinal)
		};

	private static string Rel(string path) => path.Replace(DocsRoot, "<docs>", StringComparison.Ordinal);

	// ── docset.yml + toc.yml tree ────────────────────────────────────────────

	private static JsonNode ProjectDocumentationSetFile(DocumentationSetFile d) =>
		Obj(
			("project", Str(d.Project)),
			("island", Bool(d.Island)),
			("maxTocDepth", Num(d.MaxTocDepth)),
			("devDocs", Bool(d.DevDocs)),
			("crossLinks", Arr(d.CrossLinks, c => Str(c))),
			("releaseNotes", Arr(d.ReleaseNotes, r => Str(r.Product))),
			("exclude", Arr(d.Exclude, e => Str(e))),
			("extensions", Arr(d.Extensions, e => Str(e))),
			("subs", Map(d.Subs, v => Str(v))),
#pragma warning disable CS0618 // the snapshot records obsolete keys too: they are still accepted, and codex still reads description
			("displayName", Str(d.DisplayName)),
			("description", Str(d.Description)),
#pragma warning restore CS0618
			("icon", Str(d.Icon)),
			("registry", Str(d.Registry)),
			("features", Obj(
				("primaryNav", d.Features.PrimaryNav is { } p ? Bool(p) : null),
				("disableGithubEditLink", d.Features.DisableGithubEditLink is { } g ? Bool(g) : null),
				("guideNav", d.Features.GuideNav is { } n ? Bool(n) : null)
			)),
			("api", Map(
				d.Api,
				seq => Arr(
					seq.Entries,
					e => Obj(
						("spec", Str(e.Spec)),
						("product", Str(e.Product)),
						("repository", Str(e.Repository)),
						("children", Arr(e.Children, c => Str(c.File))),
						("catalog", e.Catalog is null ? null : Arr(e.Catalog.Categories, c => Str(c))),
						("line", e.Line is { } l ? Num(l) : null),
						("column", e.Column is { } col ? Num(col) : null)
					)
				)
			)),
			("products", Arr(d.Products, p => Str(p.Id))),
			("codexGroup", Str(d.Codex?.Group)),
			("branding", d.Branding is null ? null : ProjectBranding(d.Branding)),
			("storybookRegistry", Str(d.Storybook?.Registry)),
			("cta", Map(
				d.Cta,
				c => Obj(("label", Str(c.Button?.Label)), ("url", Str(c.Button?.Url)), ("benefits", Arr(c.Benefits, b => Str(b))))
			)),
			("defaultCta", Str(d.DefaultCta)),
			("suppress", Set(d.SuppressDiagnostics.Select(h => h.ToString()))),
			("folderExcludedFiles", Set(d.FolderExcludedFiles)),
			("tocDefaultCtas", Map(d.TocDefaultCtas, v => Str(v))),
			("toc", Arr(d.TableOfContents, ProjectTocItem))
		);

	private static JsonNode ProjectBranding(BrandingConfiguration b) =>
		Obj(
			("icon", Str(b.Icon)),
			("headerBg", Str(b.HeaderBg)),
			("ogImage", Str(b.OgImage)),
			("favicon", Str(b.Favicon)),
			("appleTouchIcon", Str(b.AppleTouchIcon))
		);

	private static JsonNode ProjectTocItem(TocItem item)
	{
		var node = item switch
		{
			DetectionRuleOverviewRef r =>
				Obj(
					("kind", Str("detectionRuleOverview")),
					("path", Str(r.PathRelativeToDocumentationSet)),
					("container", Str(r.PathRelativeToContainer)),
					("hidden", Bool(r.Hidden)),
					("ruleFolders", Arr(r.DetectionRuleFolders, f => Str(f))),
					("deprecatedFile", Str(r.DeprecatedFile))
				),
			DetectionRuleRef r => Obj(("kind", Str("detectionRule")), ("path", Str(r.PathRelativeToDocumentationSet))),
			FolderIndexFileRef f => FileNode("folderIndexFile", f),
			IndexFileRef f => FileNode("indexFile", f),
			FileRef f => FileNode("file", f),
			CrossLinkRef c =>
				Obj(
					("kind", Str("crossLink")),
					("uri", Str(c.CrossLinkUri.ToString())),
					("title", Str(c.Title)),
					("hidden", Bool(c.Hidden))
				),
			DeepLinkedFolderRef f =>
				Obj(
					("kind", Str("deepLinkedFolder")),
					("path", Str(f.PathRelativeToDocumentationSet)),
					("container", Str(f.PathRelativeToContainer)),
					("defaultCta", Str(f.DefaultCta))
				),
			FolderRef f =>
				Obj(
					("kind", Str("folder")),
					("path", Str(f.PathRelativeToDocumentationSet)),
					("container", Str(f.PathRelativeToContainer)),
					("sort", Str(f.Sort)),
					("exclude", f.Exclude is null ? null : Arr(f.Exclude, e => Str(e))),
					("defaultCta", Str(f.DefaultCta))
				),
			IsolatedTableOfContentsRef t =>
				Obj(
					("kind", Str("nestedToc")),
					("path", Str(t.PathRelativeToDocumentationSet)),
					("container", Str(t.PathRelativeToContainer)),
					("island", Bool(t.Island)),
					("defaultCta", Str(t.DefaultCta))
				),
			ListingRef l =>
				Obj(
					("kind", Str("listing")),
					("path", Str(l.PathRelativeToDocumentationSet)),
					("container", Str(l.PathRelativeToContainer)),
					("glob", Str(l.Options.Glob)),
					("sort", Str(l.Options.Sort)),
					("exclude", l.Options.Exclude is null ? null : Arr(l.Options.Exclude, e => Str(e))),
					("visual", Str(l.Options.Visual.ToString())),
					("groups", l.Options.Groups is null ? null : Arr(l.Options.Groups, g => Str(g))),
					("extension", Str(l.Options.Extension)),
					("island", Bool(l.Options.Island))
				),
			ListingGroupRef g =>
				Obj(("kind", Str("listingGroup")), ("groupKey", Str(g.GroupKey)), ("path", Str(g.PathRelativeToDocumentationSet))),
			CliReferenceRef c =>
				Obj(
					("kind", Str("cliReference")),
					("schema", Str(c.SchemaPath)),
					("folder", Str(c.SupplementalFolder)),
					("title", Str(c.Title)),
					("navigationTitle", Str(c.NavigationTitle)),
					("path", Str(c.PathRelativeToDocumentationSet)),
					("container", Str(c.PathRelativeToContainer)),
					("hasAppliesTo", Bool(c.AppliesTo is not null))
				),
			_ => Obj(("kind", Str("unknown:" + item.Value?.GetType().Name)), ("path", Str(item.Item().PathRelativeToDocumentationSet)))
		};
		node["context"] = Str(Rel(item.Item().Context));
		node["children"] = Arr(ChildrenOf(item), ProjectTocItem);
		return node;
	}

	private static JsonObject FileNode(string kind, FileRef f) =>
		Obj(
			("kind", Str(kind)),
			("path", Str(f.PathRelativeToDocumentationSet)),
			("container", Str(f.PathRelativeToContainer)),
			("hidden", Bool(f.Hidden)),
			("defaultCta", Str(f.DefaultCta))
		);

	private static IEnumerable<TocItem> ChildrenOf(TocItem item) => item.Item().Children;

	// ── ConfigurationFile (resolved against the catalog) ─────────────────────

	private static JsonNode ProjectConfigurationFile(ConfigurationFile c) =>
		Obj(
			("project", Str(c.Project)),
			("maxTocDepth", Num(c.MaxTocDepth)),
			("developmentDocs", Bool(c.DevelopmentDocs)),
			("registry", Str(c.Registry.ToString())),
			("crossLinkRepositories", Arr(c.CrossLinkRepositories, r => Str(r))),
			("crossLinkEntries", Arr(
				c.CrossLinkEntries,
				e => Obj(("repository", Str(e.Repository)), ("registry", Str(e.Registry.ToString())))
			)),
			("releaseNotesProducts", Arr(c.ReleaseNotesProducts, p => Str(p))),
			("extensions", Set(c.Extensions.Enabled)),
			("products", Set(c.Products.Select(p => p.Id))),
			("substitutions", Map(c.Substitutions, v => Str(v))),
			("features", ProjectFeatures(c.Features)),
			("storybookRegistry", Str(c.StorybookRegistry)),
			("storybookRegistryFallback", Str(c.StorybookRegistryFallback)),
			("suppressDiagnostics", Set(c.SuppressDiagnostics.Select(h => h.ToString()))),
			("branding", c.Branding is null ? null : ProjectBranding(c.Branding)),
			("ctas", Map(
				c.Ctas,
				cta => Obj(
					("name", Str(cta.Name)),
					("label", Str(cta.Label)),
					("url", Str(cta.Url)),
					("benefits", Arr(cta.Benefits, b => Str(b)))
				)
			)),
			("redirects", c.Redirects is null ? null : Map(c.Redirects, ProjectRedirect)),
			("apiConfigurations", c.ApiConfigurations is null
				? null
				: Map(
					c.ApiConfigurations,
					a => Obj(
						("productKey", Str(a.ProductKey)),
						("product", Str(a.Product.Id)),
						("specFileName", Str(a.SpecFileName)),
						("localSpecFile", Str(a.LocalSpecFile is null ? null : Rel(a.LocalSpecFile.FullName))),
						("repository", Str(a.Repository)),
						("children", Arr(a.Children, f => Str(Rel(f.FullName)))),
						("apiContentDirectory", Str(a.ApiContentDirectory is null ? null : Rel(a.ApiContentDirectory.FullName))),
						("catalogCategories", Arr(a.CatalogCategories, cat => Str(cat)))
					)
				)),
			("isExcluded", Map(ExcludeProbes.ToDictionary(p => p, c.IsExcluded), b => Bool(b)))
		);

	/// <summary>Paths probed against <c>IsExcluded</c>: one per exclude rule shape seen in the docset, plus controls.</summary>
	private static readonly string[] ExcludeProbes =
	[
		"_snippets/example.md",
		"_hidden.md",
		"index.md",
		"syntax/_draft.md",
		"development/index.md"
	];

	private static JsonNode ProjectRedirect(LinkRedirect r) =>
		Obj(
			("to", Str(r.To)),
			("anchors", r.Anchors is null ? null : Map(r.Anchors, a => Str(a))),
			("many", r.Many is null
				? null
				: Arr(r.Many, m => Obj(("to", Str(m.To)), ("anchors", m.Anchors is null ? null : Map(m.Anchors, a => Str(a))))))
		);

	private static JsonNode ProjectFeatures(FeatureFlags f) =>
		Obj(
			("primaryNav", Bool(f.PrimaryNavEnabled)),
			("disableGitHubEditLink", Bool(f.DisableGitHubEditLink)),
			("stagingElasticNav", Bool(f.StagingElasticNavEnabled)),
			("websiteSearch", Bool(f.WebsiteSearchEnabled)),
			("websiteSearchScriptUrl", Str(f.WebsiteSearchScriptUrl)),
			("airGapped", Bool(f.AirGappedEnabled)),
			("diagnosticsPanel", Bool(f.DiagnosticsPanelEnabled)),
			("assemblerApiExplorer", Bool(f.AssemblerApiExplorerEnabled)),
			("guideNav", Bool(f.GuideNavEnabled)),
			("navigationPreview", Bool(f.NavigationPreviewEnabled)),
			("pageFeedback", Bool(f.PageFeedbackEnabled)),
			("apiNavGrouping", Bool(f.ApiNavGroupingEnabled))
		);

	private sealed class GoldenDocumentationSetContext(
		IDiagnosticsCollector collector,
		IFileSystem fileSystem,
		IFileInfo configurationPath,
		IDirectoryInfo documentationSourceDirectory
	) : IDocumentationSetContext
	{
		public IDiagnosticsCollector Collector => collector;
		public IDocumentationFileSystem ReadFileSystem { get; } = DocumentationFileSystem.Resolve(
			documentationSourceDirectory,
			new DocumentationScopeOptions { Inner = fileSystem, ConfigurationFile = configurationPath.FullName }
		);
		public DocumentationWriteFileSystem WriteFileSystem { get; } = new(fileSystem.DirectoryInfo.New("/out"), inner: fileSystem);
		public IDirectoryInfo OutputDirectory => fileSystem.DirectoryInfo.New("/out");
		public IFileInfo ConfigurationPath => configurationPath;
		public BuildType BuildType => BuildType.Isolated;
		public IDirectoryInfo DocumentationSourceDirectory => documentationSourceDirectory;
		public GitCheckoutInformation Git => GitCheckoutInformation.Unavailable;
		public IEnvironmentVariables Environment => new EmptyEnvironment();
	}

	/// <summary>No environment variables: keeps env-interpolated settings (storybook registry) deterministic.</summary>
	private sealed class EmptyEnvironment : IEnvironmentVariables
	{
		public string? GetEnvironmentVariable(string name) => null;
		public bool IsRunningOnCI => false;
	}
}
