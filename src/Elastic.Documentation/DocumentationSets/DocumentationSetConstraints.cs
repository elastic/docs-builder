// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Catalog;
using HintType = Elastic.Documentation.Diagnostics.HintType;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Links;
using Elastic.Documentation.Suggestions;
using Nullean.Entities;
using static Elastic.Documentation.SymlinkValidator;
using Located = Nullean.Entities.SourceLocation;

namespace Elastic.Documentation.DocumentationSets;

/// <summary>A documentation set together with the catalog its product references resolve against.</summary>
public sealed record DocumentationSetInCatalog(DocumentationSetDefinition Docset, ProductCatalog Products);

/// <summary>The evidence of an <c>api-entry-valid</c> violation: what resolution drops, an entry (<see cref="RejectedApi"/>) or one of its child pages (<see cref="RejectedApiChild"/>).</summary>
public abstract record ApiRejection;

/// <summary>
/// The evidence of an <c>api-entry-valid</c> violation that makes resolution drop the whole <c>api</c> entry
/// (the loader keeps what satisfies the rules, so it reads this instead of re-deciding).
/// </summary>
public sealed record RejectedApi(string ProductKey) : ApiRejection;

/// <summary>The evidence of an <c>api-entry-valid</c> violation that makes resolution drop one child page of an <c>api</c> entry.</summary>
public sealed record RejectedApiChild(string ProductKey, string Child) : ApiRejection;

/// <summary>A <c>branding</c> image that is set, with the field it is reported under; the evidence of a <c>branding-images-valid</c> violation.</summary>
public sealed record DeclaredBrandingImage(string Field, string Path);

/// <summary>
/// Every rule a declared documentation set must satisfy, in the order they are reported. The loader only rejects
/// what it cannot build a <see cref="DocumentationSetDefinition"/> from (malformed YAML, an invalid table of
/// contents); everything else is checked here, whoever built the definition.
/// </summary>
[Constraints]
public static partial class DocumentationSetConstraints
{
	/// <summary>The extensions <c>docset.yml</c> can enable; <c>cli</c> and <c>listing</c> enable theirs by being used and need no declaration.</summary>
	private static readonly IReadOnlySet<string> KnownExtensions = new HashSet<string>(StringComparer.Ordinal) { "detection-rules" };

	private static readonly IReadOnlySet<string> AllowedImageExtensions = new HashSet<string>
	{
		".svg",
		".png",
		".jpg",
		".jpeg",
		".gif",
		".webp",
		".ico"
	};

	/// <summary>Slugs API Explorer uses for its own pages; a child page cannot take them (<c>tags</c> is reserved only in the docs, see <see cref="DocumentedReservedChildSlug"/>).</summary>
	private static readonly IReadOnlySet<string> ReservedApiChildSlugs = new HashSet<string>(StringComparer.Ordinal)
	{
		"types",
		"group",
		"operation",
		"authentication",
		"servers"
	};

	public static Constraint<DocumentationSetInCatalog> NestedTocHasChildren { get; } = Constraints<DocumentationSetInCatalog>.EachCheck(
		m => Entries<IsolatedTableOfContentsRef>(m).Where(toc => toc.Children.Count == 0),
		(m, toc) => TocFile(m.Docset, toc) is var tocFile && m.Docset.FileSystem.File.Exists(tocFile)
			// A missing toc.yml is reported by the loader, which could not read it.
			? [Violation.Error($"Table of contents '{toc.PathRelativeToDocumentationSet}' has no children defined") with { File = tocFile }]
			: []
	).Describe("A nested table of contents whose toc.yml exists lists at least one entry.");

	public static Constraint<DocumentationSetInCatalog> CliSupplementalFolderExists { get; } = Constraints<DocumentationSetInCatalog>
		.Each(
			m => Entries<CliReferenceRef>(m).Where(
				cli => cli.SupplementalFolder is { } folder &&
					!m.Docset.FileSystem.Directory.Exists(m.Docset.FileSystem.Path.Join(m.Docset.SourceFile.Directory!.FullName, folder))
			),
			cli => $"CLI supplemental docs folder not found: {cli.SupplementalFolder}",
			at: cli => new Located(cli.Context)
		)
		.WithSeverity(Severity.Warning)
		.Describe("A CLI reference's supplemental docs folder exists.");

	public static Constraint<DocumentationSetInCatalog> ListingGlobHasExtension { get; } = Constraints<DocumentationSetInCatalog>.Each(
		m => Entries<ListingRef>(m).Where(
			l => TableOfContentsRules.GlobMayMatchNonMarkdown(l.Options.Glob ?? "**/*.md") && string.IsNullOrEmpty(l.Options.Extension)
		),
		l =>
			$"Listing '{l.PathRelativeToDocumentationSet}': glob '{l.Options.Glob ?? "**/*.md"}' may match non-.md files — 'extension:' is required to handle them.",
		at: l => new Located(l.Context)
	).Describe("A listing whose glob may match non-Markdown files names an extension to handle them.");

	public static Constraint<DocumentationSetInCatalog> DocsetFileExists { get; } = Constraints<DocumentationSetInCatalog>
		.Always(m => m.Docset.SourceFile.Exists, "No configuration file found")
		.WithSeverity(Severity.Warning)
		.Describe("docset.yml exists; without it the set is empty and its project is 'unknown'.");

	public static Constraint<DocumentationSetInCatalog, string> CrossLinkWellFormed { get; } = Constraints<DocumentationSetInCatalog>.Each(
		DeclaredCrossLinks,
		CrossLinkProblem
	).Describe(
		"Each cross_links entry names a repository, with an optional 'public' or 'internal' registry prefix, and a public docset links only to public docsets."
	);

	public static Constraint<DocumentationSetInCatalog, string?> ReleaseNotesProductKnown
	{
		get;
	} = Constraints<DocumentationSetInCatalog>.Each(m => m.Docset.ReleaseNotesProducts, ReleaseNotesProductProblem).Describe(
		"Each release_notes entry names a products.yml product with the 'release-notes' feature."
	);

	public static Constraint<DocumentationSetInCatalog> SubstitutionKeysUnique { get; } = Constraints<DocumentationSetInCatalog>.Unique(
		m => m.Docset.Substitutions,
		s => s.Key,
		StringComparer.OrdinalIgnoreCase,
		describeGroup: (_, subs) => $"Could not load docset.yml: An item with the same key has already been added. Key: {subs[1].Key}"
	).Describe("subs keys are unique when compared case-insensitively.");

	public static Constraint<DocumentationSetInCatalog, ApiRejection> ApiEntryValid
	{
		get;
	} = Constraints<DocumentationSetInCatalog>.EachCheck<ApiDeclaration, ApiRejection>(m => m.Docset.Api, ApiProblems).Describe(
		"Each api key has one entry naming a products.yml product and a spec file inside the docset, an optional 'org/repo' repository, known catalog categories and child pages under api/<key>/."
	);

	public static Constraint<DocumentationSetInCatalog> StorybookVariablesAllowed { get; } = Constraints<DocumentationSetInCatalog>
		.Each(
			m => EnvironmentInterpolation.DisallowedVariables(m.Docset.StorybookRegistry?.Trim()),
			name =>
				$"'storybook.registry' references environment variable '{name}' which is not allow-listed for interpolation and is left literal. Allowed: {string.Join(", ", EnvironmentInterpolation.AllowedVariables)}."
		)
		.WithSeverity(Severity.Warning)
		.Describe("storybook.registry only interpolates allow-listed environment variables.");

	public static Constraint<DocumentationSetInCatalog, DeclaredBrandingImage> BrandingImagesValid
	{
		get;
	} = Constraints<DocumentationSetInCatalog>.Each(
		m => BrandingImages(m.Docset),
		(m, image) => BrandingImageProblem(m.Docset, image)
	).Describe(
		"Each branding image has an allowed extension, stays inside the documentation source directory, is not an unsafe symlink and exists."
	);

	public static Constraint<DocumentationSetInCatalog, CtaDeclaration> CtaTemplateValid
	{
		get;
	} = Constraints<DocumentationSetInCatalog>.Each(m => m.Docset.Ctas, CtaProblem).Describe(
		"Each cta template has a button label and url, an absolute url uses http(s), and it lists at most three benefits."
	);

	public static Constraint<DocumentationSetInCatalog> DefaultCtaKnown { get; } = Constraints<DocumentationSetInCatalog>.Each(
		m => m.Docset.TocDefaultCtas,
		(m, page) => page.Value is null || !UsableCtaNames(m.Docset).Contains(page.Value)
			? $"'default_cta: {page.Value}' on page '{page.Key}' does not match any 'cta' template in docset.yml."
			: null
	).Describe("Every default_cta in the table of contents names a valid cta template or the built-in default.");

	public static Constraint<DocumentationSetInCatalog> PrimaryNavWithoutBranding { get; } = Constraints<DocumentationSetInCatalog>.Always(
		m => m.Docset.Branding is null || m.Docset.Features.PrimaryNav is not true,
		"'features.primary-nav' cannot be used together with 'branding': the primary nav requires Elastic global navigation."
	).Describe("features.primary-nav is not combined with branding: white-label builds have no Elastic global navigation.");

	public static Constraint<DocumentationSetInCatalog> FileStaysInFolder { get; } = Constraints<DocumentationSetInCatalog>
		.Each(
			m => Entries<FileRef>(m).Where(f => f.PathRelativeToDocumentationSet.Value.Split('/', '\\').Contains("..")),
			f =>
				$"File '{f.PathRelativeToDocumentationSet}' leaves the folder it is declared in with '..'. A file's children must be its siblings or deeper; move the entry to where the file lives.",
			at: f => new Located(f.Context)
		)
		.WithSeverity(Severity.Warning)
		.Describe("A file entry names a file in the folder it is declared in, or deeper.");

	public static Constraint<DocumentationSetInCatalog> RegistryKnown { get; } = Constraints<DocumentationSetInCatalog>
		.Always(
			m => m.Docset.Registry,
			registry => string.IsNullOrWhiteSpace(registry) || registry.Trim().Equals(
				DocSetRegistry.Internal.ToStringFast(true),
				StringComparison.OrdinalIgnoreCase
			),
			describe: registry =>
				$"registry '{registry?.Trim()}' is not a known codex registry, so this is built as the default docset. Remove 'registry' for the default docset, or set it to: {DocSetRegistry.Internal.ToStringFast(true)}."
		)
		.WithSeverity(Severity.Warning)
		.Describe("A registry, when set, names a known codex registry.");

	public static Constraint<DocumentationSetInCatalog> DocsetFileNotSymlink { get; } = Constraints<DocumentationSetInCatalog>.Always(
		m => m.Docset.SourceFile,
		file => file is not { Exists: true, LinkTarget: not null },
		describe: file =>
			$"Control file '{file.FullName}' is a symlink, which is not allowed for security reasons. Symlinked control files could be used for path traversal attacks."
	).Describe("D4: docset.yml is not a symlink.");

	public static Constraint<DocumentationSetInCatalog> TocNotEmpty { get; } = Constraints<DocumentationSetInCatalog>.Always(
		m => m.Docset.TableOfContents.Count > 0,
		"toc must produce at least one navigation item: the documentation set has no table of contents defined"
	).Describe("D8: toc produces at least one navigation item.");

	public static Constraint<DocumentationSetInCatalog> TocNestingWithinMaxDepth { get; } = Constraints<DocumentationSetInCatalog>
		.Each(
			m => NestedTablesOfContents(m.Docset.TableOfContents, 0).Where(t => t.Depth > m.Docset.MaxTocDepth),
			(m, t) =>
				$"Table of contents '{t.Toc.PathRelativeToDocumentationSet}' is nested {t.Depth} levels deep, beyond 'max_toc_depth: {m.Docset.MaxTocDepth}'. The setting is never read, so nothing limits the depth.",
			at: t => new Located(t.Toc.Context)
		)
		.WithSeverity(Severity.Warning)
		.Describe("D11: nested tables of contents stay within max_toc_depth, which is otherwise never read.");

	public static Constraint<DocumentationSetInCatalog> DevDocsHasNoEffect { get; } = Constraints<DocumentationSetInCatalog>
		.Always(m => !m.Docset.DevDocs, "'dev_docs' relaxes no rule and is never read; remove it.")
		.WithSeverity(Severity.Warning)
		.Describe("D12: dev_docs is not set: it is documented as relaxing a rule that does not exist.");

	public static Constraint<DocumentationSetInCatalog> CrossLinksUnique { get; } = Constraints<DocumentationSetInCatalog>
		.Unique(DeclaredCrossLinks, c => c, message: "A cross_links entry is declared more than once.")
		.WithSeverity(Severity.Warning)
		.Describe("D14: no cross_links entry is declared twice.");

	public static Constraint<DocumentationSetInCatalog> CrossLinksNotBlank { get; } = Constraints<DocumentationSetInCatalog>
		.Each(m => m.Docset.CrossLinks, link => !string.IsNullOrWhiteSpace(link), _ => "A blank 'cross_links' entry is ignored.")
		.WithSeverity(Severity.Warning)
		.Describe("D14: no cross_links entry is blank.");

	public static Constraint<DocumentationSetInCatalog> ExcludeOverridesLiteral { get; } = Constraints<DocumentationSetInCatalog>
		.Each(
			m => m.Docset.Exclude.Where(e => e.StartsWith('!') && e.IndexOfAny(['*', '?', '[']) >= 0),
			e => $"Exclude entry '{e}' starts with '!' so it is an exact, case-insensitive path, not a glob."
		)
		.WithSeverity(Severity.Warning)
		.Describe("D17: a !-prefixed exclude entry is an exact path and contains no glob characters.");

	public static Constraint<DocumentationSetInCatalog> ExcludeNotBlank { get; } = Constraints<DocumentationSetInCatalog>
		.Each(m => m.Docset.Exclude, e => !string.IsNullOrEmpty(e), _ => "An empty 'exclude' entry is ignored.")
		.WithSeverity(Severity.Warning)
		.Describe("D17: no exclude entry is empty.");

	public static Constraint<DocumentationSetInCatalog> ExtensionKnown { get; } = Constraints<DocumentationSetInCatalog>
		.Each(
			m => m.Docset.Extensions,
			KnownExtensions.Contains,
			e =>
				$"Unknown extension '{e}' is ignored. Only {string.Join(", ", KnownExtensions)} can be declared; 'cli' and 'listing' enable themselves."
		)
		.WithSeverity(Severity.Warning)
		.Describe("D18: every declared extension is one that is recognized.");

	public static Constraint<DocumentationSetInCatalog> SubstitutionKeyNotGenerated { get; } = Constraints<DocumentationSetInCatalog>
		.Each(
			m => m.Docset.Substitutions,
			(m, s) => IsGeneratedSubstitutionKey(s.Key, m.Products)
				? $"Substitution '{s.Key}' is overwritten by a generated value: version.*, product.* and .{{id}} keys are generated."
				: null
		)
		.WithSeverity(Severity.Warning)
		.Describe("D20: no subs key collides with a generated version.*, product.* or .{id} key.");

	public static Constraint<DocumentationSetInCatalog> ProductsKnown { get; } = Constraints<DocumentationSetInCatalog>
		.References(
			m => m.Docset.Products,
			id => [ProductNames.Normalize(id)],
			m => m.Products.Products.Keys,
			message: "A 'products' id is not defined in products.yml, so it is dropped."
		)
		.WithSeverity(Severity.Warning)
		.Describe("D22: every products[].id names a products.yml product.");

	public static Constraint<DocumentationSetInCatalog> ReleaseNotesProductsUnique { get; } = Constraints<DocumentationSetInCatalog>
		.Unique(
			m => m.Docset.ReleaseNotesProducts.OfType<string>().Where(p => !string.IsNullOrWhiteSpace(p)),
			p => ProductNames.Normalize(p),
			message: "A release_notes product is declared more than once."
		)
		.WithSeverity(Severity.Warning)
		.Describe("D24: no release_notes product is declared twice.");

#pragma warning disable CS0618 // The rule is about the obsolete fields
	public static Constraint<DocumentationSetInCatalog> DocsetObsoleteFieldsUnused { get; } = Constraints<DocumentationSetInCatalog>
		.NoneOf(
			("display_name", m =>
				m.Docset.DisplayName is not null, "'display_name' is obsolete and ignored; use the index.md h1 heading instead."),
			("description", m =>
				m.Docset.Description is not null, "'description' is obsolete; use the index.md frontmatter description instead. Codex still reads it.")
		)
		.WithSeverity(Severity.Warning)
		.Describe("D28: the obsolete display_name and description are not set.");

#pragma warning restore CS0618
	public static Constraint<DocumentationSetInCatalog> BrandingHeaderBgValid { get; } = Constraints<DocumentationSetInCatalog>
		.Always(
			m => m.Docset.Branding?.HeaderBg,
			bg => string.IsNullOrWhiteSpace(bg) || CssColour().IsMatch(bg.Trim()),
			describe: bg => $"'branding.header-bg' value '{bg}' is not a CSS colour such as '#1a1a2e'."
		)
		.WithSeverity(Severity.Warning)
		.Describe("D36: branding.header-bg is a CSS colour value.");

	public static Constraint<DocumentationSetInCatalog> CtaNamesUnique { get; } = Constraints<DocumentationSetInCatalog>
		.Unique(
			m => m.Docset.Ctas,
			c => c.Name,
			StringComparer.OrdinalIgnoreCase,
			"A 'cta' name is declared more than once; names are case-insensitive."
		)
		.WithSeverity(Severity.Warning)
		.Describe("D39: cta names are unique when compared case-insensitively.");

	public static Constraint<DocumentationSetInCatalog> CtaReplacesDefault { get; } = Constraints<DocumentationSetInCatalog>
		.Always(
			m => !m.Docset.Ctas.Any(c => c.Name.Equals(Cta.DefaultName, StringComparison.OrdinalIgnoreCase)),
			$"The 'cta' named '{Cta.DefaultName}' replaces the built-in default call to action."
		)
		.WithSeverity(Severity.Hint)
		.Describe("D39: no cta replaces the built-in default named 'trial'.");

	public static Constraint<DocumentationSetInCatalog> DefaultCtaOnePerPage { get; } = Constraints<DocumentationSetInCatalog>.Each(
		m => DefaultCtaRegistrations(m.Docset)
			.GroupBy(r => r.Page, StringComparer.Ordinal)
			.SelectMany(
				g => g.Where(r => !r.Cta.Equals(g.First().Cta, StringComparison.OrdinalIgnoreCase)).Select(
					r => (r.Page, First: g.First().Cta, r.Cta, r.Context)
				)
			),
		r => $"'{r.Page}' is registered with default CTA '{r.First}' and '{r.Cta}'. Each page can only have one default CTA.",
		at: r => new Located(r.Context)
	).Describe("D42: a page is not given two different default CTAs.");

	public static Constraint<DocumentationSetInCatalog> DefaultCtaPageExists { get; } = Constraints<DocumentationSetInCatalog>
		.Each(
			m => m.Docset.TocDefaultCtas,
			(m, page) => m.Docset.FileSystem.File.Exists(m.Docset.FileSystem.Path.Join(m.Docset.SourceDirectory.FullName, page.Key))
				? null
				: $"'default_cta: {page.Value}' is registered on page '{page.Key}', which does not exist."
		)
		.WithSeverity(Severity.Warning)
		.Describe("D43: a page registered through default_cta exists.");

	public static Constraint<DocumentationSetInCatalog> TocFileExistsAndMarkdown { get; } = Constraints<DocumentationSetInCatalog>.Each(
		m => PagesNeedingAFile(m.Docset),
		(m, file) => !IsMarkdown(file)
			? $"Table of contents references '{file.PathRelativeToDocumentationSet}' but the file exists but is not a valid Markdown document."
			: !m.Docset.FileSystem.File.Exists(PageFile(m.Docset, file))
				? $"Table of contents references '{file.PathRelativeToDocumentationSet}' but the file does not exist on disk."
				: null,
		file => new Located(file.Context)
	).Describe("D54: a file in the table of contents exists and is Markdown.");

	public static Constraint<DocumentationSetInCatalog> TocDeepLinkedFileHasNoChildren { get; } = Constraints<DocumentationSetInCatalog>
		.Each(
			m => m.Docset.SuppressDiagnostics.Contains(HintType.DeepLinkingVirtualFile)
				? []
				: Entries<FileRef>(m).Where(
					f => f is not FolderIndexFileRef and not DetectionRuleOverviewRef && f
						.PathRelativeToContainer
						.Value
						.Contains('/') && f.Children.Count > 0
				),
			f =>
				$"File '{f.PathRelativeToContainer}' uses deep-linking with children. Consider using 'folder' instead of 'file' for better navigation structure. Virtual files are primarily intended to group sibling files together.",
			at: f => new Located(f.Context)
		)
		.WithSeverity(Severity.Hint)
		.Describe("D56: a file entry whose path contains '/' has no children, unless DeepLinkingVirtualFile is suppressed.");

	public static Constraint<DocumentationSetInCatalog> TocFileChildrenCreatable { get; } = Constraints<DocumentationSetInCatalog>.Each(
		m => Entries<FileRef>(m).Where(f => f.Children.Count > 0 && !f.Children.Any(c => IsCreatable(m.Docset, c))),
		f => $"File navigation '{f.PathRelativeToDocumentationSet}' has children defined but none could be created",
		at: f => new Located(f.Context)
	).Describe("D57: a file with children has at least one child that can be created.");

	public static Constraint<DocumentationSetInCatalog> ApiKeyUniqueIgnoringCase { get; } = Constraints<DocumentationSetInCatalog>
		.Unique(
			m => m.Docset.Api,
			a => a.ProductKey,
			StringComparer.OrdinalIgnoreCase,
			describeGroup: (_, apis) =>
				$"API keys {string.Join(", ", apis.Select(a => $"'{a.ProductKey}'"))} differ only in case; they are collected case-insensitively, so only one is kept."
		)
		.WithSeverity(Severity.Warning)
		.Describe("A1: api keys are unique when compared case-insensitively.");

	public static Constraint<DocumentationSetInCatalog> ApiKeyUrlSegment { get; } = Constraints<DocumentationSetInCatalog>
		.Each(
			m => m.Docset.Api,
			a => ApiKeySegment().IsMatch(a.ProductKey),
			a =>
				$"API key '{a.ProductKey}' is not a valid path segment: it is used as 'api/{a.ProductKey}/' and in URLs, so use letters, digits, '.', '_' and '-'."
		)
		.WithSeverity(Severity.Warning)
		.Describe("A2: an api key is usable as a path and URL segment.");

	public static Constraint<DocumentationSetInCatalog> ApiChildSlugsUnique { get; } = Constraints<DocumentationSetInCatalog>.Unique(
		m => ApiChildren(m.Docset),
		c => (c.Api, Slug: ApiChildSlug(Path.GetFileName(c.Child))),
		describeGroup: (key, children) =>
			$"Child pages {string.Join(", ", children.Select(c => $"'{c.Child}'"))} of API '{key.Api}' share the slug '{key.Slug}'."
	).Describe("A21: the child pages of an api key have distinct URL slugs.");

	public static Constraint<DocumentationSetInCatalog> ApiChildSlugNotReserved { get; } = Constraints<DocumentationSetInCatalog>.Each(
		m => ApiChildren(m.Docset),
		c => !ReservedApiChildSlugs.Contains(ApiChildSlug(Path.GetFileName(c.Child))),
		c =>
			$"Child page '{c.Child}' for API '{c.Api}' uses the slug '{ApiChildSlug(Path.GetFileName(c.Child))}', which API Explorer reserves. Rename the file."
	).Describe("D98: no api child page takes a slug API Explorer reserves (types, group, operation, authentication, servers).");

	public static Constraint<DocumentationSetInCatalog> FolderIndexFileNameMatchesFolder { get; } = Constraints<DocumentationSetInCatalog>
		.Each(
			m => m.Docset.SuppressDiagnostics.Contains(HintType.FolderFileNameMismatch)
				? []
				: Entries<FolderIndexFileRef>(m).Where(f => NamesDiffer(f) is not null),
			f => NamesDiffer(f) is { } names
				? $"File name '{names.FileName}' does not match folder name '{names.FolderName}'. Best practice is to name the file the same as the folder (e.g., 'folder: {names.FolderName}, file: {names.FolderName}.md')."
				: "",
			at: f => new Located(f.Context)
		)
		.WithSeverity(Severity.Hint)
		.Describe(
			"D60: a folder's file is named after the folder, unless it is index.md or at the root, or FolderFileNameMismatch is suppressed."
		);

	public static Constraint<DocumentationSetInCatalog> TocFolderExists { get; } = Constraints<DocumentationSetInCatalog>.Each(
		m => Entries<FolderRef>(m).Where(
			f => f is not DeepLinkedFolderRef &&
				!m
					.Docset
					.FileSystem
					.Directory
					.Exists(m.Docset.FileSystem.Path.Join(m.Docset.SourceDirectory.FullName, f.PathRelativeToDocumentationSet.Value))
		),
		f => $"Folder '{f.PathRelativeToDocumentationSet}' does not exist in the documentation source directory.",
		at: f => new Located(f.Context)
	).Describe("D64: a folder entry names a directory that exists.");

	public static Constraint<DocumentationSetInCatalog> NestedTocPathsUnique { get; } = Constraints<DocumentationSetInCatalog>
		.Unique(
			m => Entries<IsolatedTableOfContentsRef>(m),
			t => t.PathRelativeToDocumentationSet.Value,
			StringComparer.Ordinal,
			describe: (path, count) => $"Table of contents '{path}' is declared {count} times; only the first is used.",
			at: t => new Located(t.Context)
		)
		.WithSeverity(Severity.Warning)
		.Describe("D70: a nested table of contents is declared once; duplicates are ignored.");

	public static Constraint<DocumentationSetInCatalog> TocNodeHasIndexPage { get; } = Constraints<DocumentationSetInCatalog>.Each(
		m => m
			.Docset
			.TableOfContents
			.Descendants()
			.Select(i => i.Item())
			.Where(
				n => n is FolderRef or IsolatedTableOfContentsRef && n.Children.Count > 0 && n.Children.All(c => c.Value is CrossLinkRef)
			),
		n => $"'{n.PathRelativeToDocumentationSet}' lists only cross-links, so it has no index page to navigate to.",
		at: n => new Located(n.Context)
	).Describe(
		"D73: a folder or nested table of contents that lists entries lists at least one besides cross-links, so it has an index page."
	);

	public static Constraint<DocumentationSetInCatalog> TocFileListedOnce { get; } = Constraints<DocumentationSetInCatalog>
		.Unique(
			m => Entries<FileRef>(m).Where(f => f is not DetectionRuleRef),
			f => f.PathRelativeToDocumentationSet.Value,
			StringComparer.Ordinal,
			describe: (path, count) => $"File '{path}' is listed {count} times in the table of contents; only the first is used.",
			at: f => new Located(f.Context)
		)
		.WithSeverity(Severity.Warning)
		.Describe("D74: a file appears in the table of contents once; later entries are ignored.");

	public static Constraint<DocumentationSetInCatalog> PageFileNameChars { get; } = Constraints<DocumentationSetInCatalog>.Each(
		m => Entries<FileRef>(m).Where(f => f is not DetectionRuleRef && !IsValidPageFileName(f.PathRelativeToDocumentationSet.Value)),
		f =>
			$"File name {f.PathRelativeToDocumentationSet} is not valid needs to be lowercase and contain only alphanumeric characters, spaces, dashes, dots, underscores, and plus signs",
		at: f => new Located(f.Context)
	).Describe("D76: page file names are lowercase [a-z0-9_] plus space, '-', '.' and '+', with a few hard-coded exemptions.");

	public static Constraint<DocumentationSetInCatalog> ListingIslandNeedsVisual { get; } = Constraints<DocumentationSetInCatalog>.Each(
		m => Entries<ListingRef>(m).Where(l => l.Options is { Island: true, Visual: ListingVisual.None }),
		l =>
			$"Listing '{l.PathRelativeToDocumentationSet}' sets island: true with visual: none. Set visual: groups or visual: all so the listing is reachable from the main nav.",
		at: l => new Located(l.Context)
	).Describe("D81: a listing with island: true sets a visual other than none.");

	public static Constraint<DocumentationSetInCatalog> DetectionRuleFoldersExist { get; } = Constraints<DocumentationSetInCatalog>.Each(
		m => Entries<DetectionRuleOverviewRef>(m)
			.SelectMany(overview => overview.DetectionRuleFolders.Select(folder => (Overview: overview, Folder: folder)))
			.Where(
				r => !m.Docset.FileSystem.Directory.Exists(m.Docset.FileSystem.Path.Join(OverviewDirectory(m.Docset, r.Overview), r.Folder))
			),
		r => $"Detection rule folder '{r.Folder}' of '{r.Overview.PathRelativeToDocumentationSet}' does not exist.",
		at: r => new Located(r.Overview.Context)
	).Describe("D88: every rule folder of a detection_rules entry exists.");

	public static Constraint<DocumentationSetInCatalog> RedirectTargetValid { get; } = Constraints<DocumentationSetInCatalog>.Each(
		m => RedirectTargets(m, nextToTo: false),
		RedirectTargetProblem
	).Describe("D106: an external redirect target is an absolute URI; a local one exists and is Markdown.");

	public static Constraint<DocumentationSetInCatalog> RedirectManyTargetsValid { get; } = Constraints<DocumentationSetInCatalog>
		.Each(m => RedirectTargets(m, nextToTo: true), RedirectTargetProblem)
		.WithSeverity(Severity.Warning)
		.Describe("D107: 'many' targets are validated like 'to' even when a top-level 'to' is set.");

	public static Constraint<DocumentationSetInCatalog> RedirectNotToItself { get; } = Constraints<DocumentationSetInCatalog>
		.Each(
			m => Redirects(m).Where(
				r => (r.Value.To ?? r.Value.Many?.FirstOrDefault(x => x.To is not null)?.To) is { } to && string.Equals(
					to.Trim().TrimEnd('/'),
					r.Key.Trim().TrimEnd('/'),
					StringComparison.OrdinalIgnoreCase
				)
			),
			r => $"Redirect '{r.Key}' points to itself and is dropped."
		)
		.WithSeverity(Severity.Warning)
		.Describe("P22: a redirect does not point to itself (trimmed, case-insensitive).");

	public static Constraint<DocumentationSetInCatalog> RedirectManySingleTarget { get; } = Constraints<DocumentationSetInCatalog>
		.Each(
			m => Redirects(m).Where(r => r.Value.Many?.Count(x => x.To is not null) > 1),
			r => $"Redirect '{r.Key}' lists several 'many' targets; only the first with a 'to' is used."
		)
		.WithSeverity(Severity.Warning)
		.Describe("P23: a redirect's 'many' lists one target; only the first with a 'to' is used.");

	public static Constraint<DocumentationSetInCatalog> RedirectsFileNamedForDocset { get; } = Constraints<DocumentationSetInCatalog>.Check(
		m => m.Docset.RedirectsFile is { } file && file.Name != RedirectsFileName(m.Docset)
			? [
				Violation.Warning(
					$"The redirects file is '{file.Name}', but a docset file named '{m.Docset.SourceFile.Name}' reads '{RedirectsFileName(m.Docset)}'."
				)
			]
			: []
	).Describe("D6: the redirects file is _redirects.yml when the docset file starts with '_', else redirects.yml.");

	public static Constraint<DocumentationSetInCatalog> SuppressHintKnown { get; } = Constraints<DocumentationSetInCatalog>
		.Each(
			m => m.Docset.UnknownSuppressions,
			name =>
				$"'suppress' lists '{name}', which is not a hint type, so it is ignored. Valid values: {string.Join(", ", Enum.GetNames<HintType>())}."
		)
		.WithSeverity(Severity.Warning)
		.Describe(
			"D30: every suppress entry names a hint type; an unknown name, or a suppress that is not a list, is dropped without notice."
		);

	public static Constraint<DocumentationSetInCatalog> ApiEntryKeysKnown { get; } = Constraints<DocumentationSetInCatalog>
		.Each(
			m => m
				.Docset
				.Api
				.SelectMany(a => a.Entries.SelectMany(e => e.UnknownKeys.Select(key => (Api: a.ProductKey, Key: key, Entry: e)))),
			k =>
				$"API '{k.Api}' has the key '{k.Key}', which is not read and is ignored. Check for typos: spec, product, repository, children and catalog are read.",
			k => k.Entry.Location is { } at ? new Located(null, at.Line, at.Column) : null
		)
		.WithSeverity(Severity.Warning)
		.Describe(
			"D90: an api entry writes only the keys the converter reads (spec, product, repository, children, catalog); any other key is skipped without notice."
		);

	public static Constraint<DocumentationSetInCatalog> RedirectNotDropped { get; } = Constraints<DocumentationSetInCatalog>
		.Each(
			m => m.Docset.DroppedRedirects,
			d => d.ManyItem is { } item
				? $"Item {item + 1} of 'many' in the redirect '{d.Source}' is not a mapping, or has no 'to' or no 'anchors', and is dropped."
				: $"The redirect '{d.Source}' holds no 'to', 'anchors' or 'many' the reader can use and is dropped."
		)
		.WithSeverity(Severity.Warning)
		.Describe("D103: a redirect entry and every 'many' item holds something the reader keeps; otherwise it is dropped without notice.");

	public static Constraint<DocumentationSetInCatalog> TocEntrySelectsAShape { get; } = Constraints<DocumentationSetInCatalog>
		.Each(
			m => m.Docset.TocAuthoring,
			a => a.Kind == TocEntryKind.Dropped
				? $"The toc entry {a.Entry} selects no shape and is dropped. An entry needs one of file, folder, toc, crosslink, listing, cli, hidden or detection_rules with file."
				: null,
			TocAt
		)
		.WithSeverity(Severity.Warning)
		.Describe("D45: a toc entry is a mapping with a key that selects its shape; any other entry is dropped without notice.");

	public static Constraint<DocumentationSetInCatalog> TocEntryKeysKnown { get; } = Constraints<DocumentationSetInCatalog>
		.Each(
			m => m.Docset.TocAuthoring.SelectMany(a => a.UnknownKeys.Select(key => (Entry: a, Key: key))),
			k => $"The toc entry '{k.Entry.Entry}' has the key '{k.Key}', which no toc entry reads, so it is ignored. Check for typos.",
			k => TocAt(k.Entry)
		)
		.WithSeverity(Severity.Warning)
		.Describe("D47: a toc entry writes only keys some entry shape reads; any other key is skipped without notice.");

	public static Constraint<DocumentationSetInCatalog> TocEntrySingleShape { get; } = Constraints<DocumentationSetInCatalog>
		.Each(
			m => m
				.Docset
				.TocAuthoring
				.SelectMany(a => a.IgnoredKeys.Where(key => !IsExplainedElsewhere(a, key)).Select(key => (Entry: a, Key: key))),
			k =>
				$"The toc entry '{k.Entry.Entry}' also has the key '{k.Key}', which is the key of another kind of entry and is ignored here. An entry is one kind: remove '{k.Key}' or split the entry.",
			k => TocAt(k.Entry)
		)
		.WithSeverity(Severity.Warning)
		.Describe(
			"D46: a toc entry is one kind; a key of another kind written beside it is ignored, with no check that the kinds exclude each other."
		);

	public static Constraint<DocumentationSetInCatalog> TocFileHiddenTitleIgnored { get; } = Constraints<DocumentationSetInCatalog>
		.Each(
			m => m
				.Docset
				.TocAuthoring
				.Where(a => a.Kind == TocEntryKind.File)
				.SelectMany(a => a.IgnoredKeys.Where(key => key is "hidden" or "title").Select(key => (Entry: a, Key: key))),
			k => $"The toc entry '{k.Entry.Entry}' has '{k.Key}' beside 'file:', which ignores it. Hide a page with 'hidden: <file>'.",
			k => TocAt(k.Entry)
		)
		.WithSeverity(Severity.Warning)
		.Describe("D48: 'hidden:' and 'title:' next to 'file:' do nothing: a file entry cannot be hidden or retitled that way.");

	public static Constraint<DocumentationSetInCatalog> TocDetectionRulesNeedsFile { get; } = Constraints<DocumentationSetInCatalog>
		.Each(
			m => m.Docset.TocAuthoring,
			a => a.IgnoredKeys.Contains("detection_rules")
				? $"The toc entry '{a.Entry}' has 'detection_rules' without a 'file:', so it is read as another kind of entry. Add the overview 'file:'."
				: null,
			TocAt
		)
		.WithSeverity(Severity.Warning)
		.Describe("D87: a detection_rules entry needs a 'file:' beside it; without one it falls through to another kind.");

	public static Constraint<DocumentationSetInCatalog> TocFolderSortExcludeNeedNoChildren { get; } = Constraints<DocumentationSetInCatalog>
		.Each(
			m => m
				.Docset
				.TocAuthoring
				.Where(a => a.Kind is TocEntryKind.Folder or TocEntryKind.FolderWithFile && a.ExplicitChildren)
				.SelectMany(a => a.WrittenKeys.Where(key => key is "sort" or "exclude").Select(key => (Entry: a, Key: key))),
			k =>
				$"The toc entry '{k.Entry.Entry}' lists its children, so '{k.Key}' is ignored: it only applies to files the folder finds itself.",
			k => TocAt(k.Entry)
		)
		.WithSeverity(Severity.Warning)
		.Describe("D62: sort and exclude on a folder only apply to auto-discovery; they are ignored when the folder lists children.");

	public static Constraint<DocumentationSetInCatalog> TocIslandIsBoolean { get; } = Constraints<DocumentationSetInCatalog>
		.Each(
			m => m.Docset.TocAuthoring,
			a => a.IslandRaw is { } raw && !bool.TryParse(raw, out _)
				? $"The toc entry '{a.Entry}' has island '{raw}', which is not a boolean, so it is read as false."
				: null,
			TocAt
		)
		.WithSeverity(Severity.Warning)
		.Describe("D52: 'island' is true or false; any other text is read as false without notice.");

	public static Constraint<DocumentationSetInCatalog> TocCrosslinkHiddenIsBoolean { get; } = Constraints<DocumentationSetInCatalog>
		.Each(
			m => m.Docset.TocAuthoring,
			a => a.HiddenRaw is { } raw && !bool.TryParse(raw, out _)
				? $"The toc entry '{a.Entry}' has hidden '{raw}', which is not a boolean, so it is read as false."
				: null,
			TocAt
		)
		.WithSeverity(Severity.Warning)
		.Describe("D49: 'hidden' on a crosslink entry is true or false; any other text is read as false without notice.");

	public static Constraint<DocumentationSetInCatalog> ListingVisualKnown { get; } = Constraints<DocumentationSetInCatalog>
		.Each(
			m => m.Docset.TocAuthoring,
			a => a.VisualRaw is { } raw && !ListingVisualExtensions.TryParse(raw, out _)
				? $"The toc entry '{a.Entry}' has visual '{raw}', which is not none, groups or all, so it is read as none."
				: null,
			TocAt
		)
		.WithSeverity(Severity.Warning)
		.Describe("D79: a listing's 'visual' is none, groups or all; any other text is read as none without notice.");

	public static Constraint<DocumentationSetInCatalog> ListingSortKnown { get; } = Constraints<DocumentationSetInCatalog>
		.Each(
			m => m.Docset.TocAuthoring,
			a => a.SortRaw is { } raw && !SortOrderExtensions.TryParse(raw, out _)
				? $"The toc entry '{a.Entry}' has sort '{raw}', which is not asc, ascending, desc or descending, so path order is kept."
				: null,
			TocAt
		)
		.WithSeverity(Severity.Warning)
		.Describe(
			"D79: a listing's 'sort' is asc, ascending, desc or descending; any other text keeps path order without notice (a folder reports it)."
		);

	private static string RedirectsFileName(DocumentationSetDefinition docset) =>
		docset.SourceFile.Name.StartsWith('_') ? "_redirects.yml" : "redirects.yml";

	private static Located? TocAt(TocEntryAuthoring entry) => new(entry.File, entry.Location?.Line, entry.Location?.Column);

	/// <summary>Keys another rule reports on an entry: <c>hidden</c>/<c>title</c> beside <c>file:</c> (D48) and <c>detection_rules</c> without a file (D87).</summary>
	private static bool IsExplainedElsewhere(TocEntryAuthoring entry, string key) =>
		(entry.Kind == TocEntryKind.File && key is "hidden" or "title") || key == "detection_rules";

	// ── Table of contents ──────────────────────────────────────────────────────────────────────────────────────────

	private static IEnumerable<T> Entries<T>(DocumentationSetInCatalog m) => m.Docset.TableOfContents.Descendants().OfCase<T>();

	private static string TocFile(DocumentationSetDefinition docset, IsolatedTableOfContentsRef toc) =>
		docset
			.FileSystem
			.Path
			.Join(
				docset
					.FileSystem
					.DirectoryInfo
					.New(docset.FileSystem.Path.Join(docset.SourceFile.Directory!.FullName, toc.PathRelativeToDocumentationSet.Value))
					.FullName,
				"toc.yml"
			);

	private static string PageFile(DocumentationSetDefinition docset, FileRef file) =>
		docset.FileSystem.Path.Join(docset.SourceDirectory.FullName, file.PathRelativeToDocumentationSet.Value);

	private static bool IsMarkdown(FileRef file) =>
		file.PathRelativeToDocumentationSet.Value.EndsWith(".md", StringComparison.OrdinalIgnoreCase);

	// Whether the navigation builder produces an item for this entry: a file needs its page and, when it has children, one creatable child;
	// a folder or nested table of contents needs one creatable child. Every other kind of entry is assumed to be created.
	private static bool IsCreatable(DocumentationSetDefinition docset, TocItem item) => item.Value switch
	{
		FileRef file =>
			(file is DetectionRuleRef || (IsMarkdown(file) && docset.FileSystem.File.Exists(PageFile(docset, file))))
				&& (file.Children.Count == 0 || file.Children.Any(c => IsCreatable(docset, c))),
		FolderRef folder => folder.Children.Any(c => IsCreatable(docset, c)),
		IsolatedTableOfContentsRef toc => toc.Children.Any(c => IsCreatable(docset, c)),
		_ => true
	};

	// The deprecated-rules overview that follows a detection_rules entry is generated, so it has no file on disk.
	private static IEnumerable<FileRef> PagesNeedingAFile(DocumentationSetDefinition docset)
	{
		var generated = docset
			.TableOfContents
			.Descendants()
			.OfCase<DetectionRuleOverviewRef>()
			.Select(o => o.DeprecatedSiblingRef?.PathRelativeToDocumentationSet.Value)
			.OfType<string>()
			.ToHashSet(StringComparer.Ordinal);
		return docset
			.TableOfContents
			.Descendants()
			.OfCase<FileRef>()
			.Where(f => f is not DetectionRuleRef && !generated.Contains(f.PathRelativeToDocumentationSet.Value));
	}

	// The first level is the one declared in docset.yml.
	private static IEnumerable<(IsolatedTableOfContentsRef Toc, int Depth)> NestedTablesOfContents(IEnumerable<TocItem> items, int depth)
	{
		foreach (var item in items.Select(i => i.Item()))
		{
			var itemDepth = item is IsolatedTableOfContentsRef ? depth + 1 : depth;
			if (item is IsolatedTableOfContentsRef toc)
				yield return (toc, itemDepth);
			foreach (var nested in NestedTablesOfContents(item.Children, itemDepth))
				yield return nested;
		}
	}

	private static string OverviewDirectory(DocumentationSetDefinition docset, DetectionRuleOverviewRef overview) =>
		docset
			.FileSystem
			.Path
			.GetDirectoryName(docset.FileSystem.Path.Join(docset.SourceDirectory.FullName, overview.PathRelativeToDocumentationSet.Value))
			?? docset.SourceDirectory.FullName;

	// folder: plus file: entries whose file name differs from the name of the folder they sit in, ignoring case, hyphens and underscores.
	// index.md and entries at the root are exempt.
	private static (string FileName, string FolderName)? NamesDiffer(FolderIndexFileRef file)
	{
		var segments = file.PathRelativeToDocumentationSet.Value.Split('/');
		if (segments.Length < 2)
			return null;
		var fileName = segments[^1];
		var folderName = segments[^2];
		if (fileName == "index.md")
			return null;
		return NormalizeForFolderComparison(fileName.Replace(".md", "")) != NormalizeForFolderComparison(folderName)
			? (fileName, folderName)
			: null;
	}

	private static string NormalizeForFolderComparison(string name) =>
		name.Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal).ToLowerInvariant();

	// A page's default CTA is the nearest declaration: its own, else the one inherited from the folder, nested table of contents or docset it sits in.
	private static IEnumerable<(string Page, string Cta, DeclaringFilePath Context)> DefaultCtaRegistrations(
		DocumentationSetDefinition docset
	) => Registrations(docset.TableOfContents, docset.DefaultCta);

	private static IEnumerable<(string Page, string Cta, DeclaringFilePath Context)> Registrations(
		IEnumerable<TocItem> items,
		string? inherited
	)
	{
		foreach (var item in items.Select(i => i.Value))
		{
			switch (item)
			{
				case FileRef file:
					var fileDefault = file.DefaultCta ?? inherited;
					if (!string.IsNullOrWhiteSpace(fileDefault))
						yield return (NormalizePagePath(file.PathRelativeToDocumentationSet.Value), fileDefault, file.Context);
					foreach (var r in Registrations(file.Children, fileDefault))
						yield return r;
					break;
				case IsolatedTableOfContentsRef toc:
					foreach (var r in Registrations(toc.Children, toc.DefaultCta ?? inherited))
						yield return r;
					break;
				case FolderRef folder:
					foreach (var r in Registrations(folder.Children, folder.DefaultCta ?? inherited))
						yield return r;
					break;
				case CrossLinkRef crossLink:
					foreach (var r in Registrations(crossLink.Children, inherited))
						yield return r;
					break;
			}
		}
	}

	private static string NormalizePagePath(string path)
	{
		var stack = new List<string>();
		foreach (var segment in path.Replace('\\', '/').TrimStart('/').Split('/'))
		{
			if (segment is "" or ".")
				continue;
			if (segment == "..")
			{
				if (stack.Count > 0)
					stack.RemoveAt(stack.Count - 1);
				continue;
			}
			stack.Add(segment);
		}
		return string.Join('/', stack);
	}

	// Output file names are validated against these when a page is written; some long-standing files are exempt.
	[GeneratedRegex(@"^[a-z0-9\s\-_\.\/\\+]*[a-z0-9_\-+]\.([a-z]+)$")]
	private static partial Regex FilePathRegex();

	[GeneratedRegex(@"^[a-z0-9_][a-z0-9_\-\s\.+]*?\.([a-z]+)$")]
	private static partial Regex FileNameRegex();

	private static bool IsValidPageFileName(string path) => path switch
	{
		_ when path.StartsWith("release-notes/elastic-agent/_snippets/", StringComparison.Ordinal) => true,
		_ when path.StartsWith("reference/query-languages/esql/_snippets/", StringComparison.Ordinal) => true,
		_ when path.EndsWith(".svg", StringComparison.Ordinal) => true,
		_ when path.EndsWith(".gif", StringComparison.Ordinal) => true,
		_ when path.EndsWith(".png", StringComparison.Ordinal) => true,
		"reference/security/prebuilt-rules/audit_policies/windows/README.md" => true,
		"audit_policies/windows/README.md" => true,
		"extend/integrations/developer-workflow-fleet-UI.md" => true,
		"extend/developer-workflow-fleet-UI.md" => true,
		"reference/elasticsearch/clients/ruby/Helpers.md" => true,
		"reference/Helpers.md" => true,
		"explore-analyze/ai-features/llm-guides/connect-to-vLLM.md" => true,
		_ => FilePathRegex().IsMatch(path) && FileNameRegex().IsMatch(Path.GetFileName(path))
	};

	// ── docset.yml ─────────────────────────────────────────────────────────────────────────────────────────────────

	/// <summary>A CSS colour value: hex, a keyword such as <c>navy</c>, or a function such as <c>rgb(0 0 0 / 50%)</c>.</summary>
	[GeneratedRegex(@"^(#(?:[0-9a-fA-F]{3,4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})|[a-zA-Z]+|[a-zA-Z-]+\([0-9a-zA-Z\s.,%/+-]*\))$")]
	private static partial Regex CssColour();

	// Substitution keys the resolver generates and so overwrites: version.*, and product.{id} and .{id} for every public-reference product in either spelling of its id.
	private static bool IsGeneratedSubstitutionKey(string key, ProductCatalog catalog) =>
		!string.IsNullOrEmpty(key)
			&& (key.StartsWith("version.", StringComparison.OrdinalIgnoreCase)
				|| catalog
					.PublicReferenceProducts
					.Keys
					.SelectMany(id => new[] { id, id.Replace('-', '_') })
					.Any(
						id => key.Equals($"product.{id}", StringComparison.OrdinalIgnoreCase) || key.Equals(
							$".{id}",
							StringComparison.OrdinalIgnoreCase
						)
					));

	// ── cross_links and release_notes ──────────────────────────────────────────────────────────────────────────────

	/// <summary>The non-blank <c>cross_links</c>, trimmed.</summary>
	private static IEnumerable<string> DeclaredCrossLinks(DocumentationSetInCatalog m) =>
		m.Docset.CrossLinks.Where(raw => !string.IsNullOrWhiteSpace(raw)).Select(raw => raw.Trim());

	private static string? CrossLinkProblem(DocumentationSetInCatalog m, string raw)
	{
		var docsetRegistry = DocumentationSetRules.ParseRegistry(m.Docset.Registry);
		var entryRegistry = docsetRegistry;

		var colonSlash = raw.IndexOf("://", StringComparison.Ordinal);
		if (colonSlash >= 0)
		{
			var prefix = raw[..colonSlash];
			if (string.IsNullOrWhiteSpace(raw[(colonSlash + 3)..]))
				return $"Cross-link '{raw}' has empty repository after registry prefix.";
			if (!DocSetRegistryExtensions.TryParse(prefix, out entryRegistry, true))
				return $"Cross-link '{raw}' uses unknown registry '{prefix}'. Use 'public' or 'internal'.";
		}

		return docsetRegistry == DocSetRegistry.Public && entryRegistry != DocSetRegistry.Public
			? $"Public documentation cannot link to codex docs. Cross-link '{raw}' targets registry '{entryRegistry.ToStringFast()}'. Remove it or use a public docset."
			: null;
	}

	private static string? ReleaseNotesProductProblem(DocumentationSetInCatalog m, string? raw)
	{
		var product = raw?.Trim();
		if (string.IsNullOrEmpty(product))
			return "A 'release_notes' entry is missing a 'product' value.";

		if (!product.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-'))
			return $"Invalid 'release_notes' product '{product}'. Product ids must match [a-zA-Z0-9_-]+.";

		// products.yml keys are hyphenated; accept the underscore variant for parity with `products`.
		if (!m.Products.Products.TryGetValue(ProductNames.Normalize(product), out var resolved))
			return $"Unknown 'release_notes' product '{product}'. It must be a product id defined in products.yml.";

		return resolved.Features.ParticipatesInReleaseNotes
			? null
			: $"Product '{product}' declared in 'release_notes' does not participate in the release-notes system (it lacks the 'release-notes' feature in products.yml).";
	}

	// ── branding ───────────────────────────────────────────────────────────────────────────────────────────────────

	/// <summary>The branding images that are set, with the field name each is reported under.</summary>
	private static IEnumerable<DeclaredBrandingImage> BrandingImages(DocumentationSetDefinition docset) =>
		docset.Branding is not { } branding
			? []
			: new[]
			{
				new DeclaredBrandingImage("branding.icon", branding.Icon ?? ""),
				new DeclaredBrandingImage("branding.og-image", branding.OgImage ?? ""),
				new DeclaredBrandingImage("branding.favicon", branding.Favicon ?? ""),
				new DeclaredBrandingImage("branding.apple-touch-icon", branding.AppleTouchIcon ?? "")
			}.Where(i => !string.IsNullOrEmpty(i.Path));

	private static string? BrandingImageProblem(DocumentationSetDefinition docset, DeclaredBrandingImage image)
	{
		var ext = Path.GetExtension(image.Path).ToLowerInvariant();
		if (!AllowedImageExtensions.Contains(ext))
			return $"'{image.Field}' has unsupported extension '{ext}'. Allowed: {string.Join(", ", AllowedImageExtensions)}";

		var sourceDirectory = docset.SourceDirectory;
		var resolved = docset.FileSystem.FileInfo.New(Path.GetFullPath(Path.Join(sourceDirectory.FullName, image.Path)));

		if (!resolved.IsSubPathOf(sourceDirectory))
			return $"'{image.Field}' path '{image.Path}' escapes the documentation source directory.";

		if (ValidateFileAccess(resolved, sourceDirectory) is { } symlinkError)
			return $"'{image.Field}' path '{image.Path}' is unsafe: {symlinkError}";

		return resolved.Exists ? null : $"'{image.Field}' file '{image.Path}' does not exist.";
	}

	// ── cta ────────────────────────────────────────────────────────────────────────────────────────────────────────

	private static string? CtaProblem(CtaDeclaration cta)
	{
		if (string.IsNullOrWhiteSpace(cta.Label) || string.IsNullOrWhiteSpace(cta.Url))
			return $"'cta.{cta.Name}' must define both 'button.label' and 'button.url'.";

		if (
			Uri.TryCreate(cta.Url.Trim(), UriKind.RelativeOrAbsolute, out var uri)
			&& uri.IsAbsoluteUri
			&& uri.Scheme != Uri.UriSchemeHttp
			&& uri.Scheme != Uri.UriSchemeHttps
		)
			return $"'cta.{cta.Name}.button.url' must use http/https or a relative URL.";

		return cta.Benefits.Count > Cta.MaxBenefits
			? $"'cta.{cta.Name}.benefits' has {cta.Benefits.Count} entries; a maximum of {Cta.MaxBenefits} is allowed."
			: null;
	}

	/// <summary>The CTA templates pages can use, by case-insensitive name: the built-in default and every valid <c>cta</c>.</summary>
	private static HashSet<string> UsableCtaNames(DocumentationSetDefinition docset) =>
		new([Cta.DefaultName, .. docset.Ctas.Where(c => CtaProblem(c) is null).Select(c => c.Name)], StringComparer.OrdinalIgnoreCase);

	// ── redirects ──────────────────────────────────────────────────────────────────────────────────────────────────

	private static IReadOnlyDictionary<string, LinkRedirect> Redirects(DocumentationSetInCatalog m) =>
		m.Docset.Redirects ?? new Dictionary<string, LinkRedirect>();

	// The top-level `to`, or the `many` targets when there is none; with nextToTo, the `many` targets of a redirect that also has a `to`.
	private static IEnumerable<(string From, string To)> RedirectTargets(DocumentationSetInCatalog m, bool nextToTo) =>
		Redirects(m)
			.SelectMany(
				r => nextToTo
					? r.Value.To is not null ? (r.Value.Many ?? []).Select(x => (r.Key, x.To)) : []
					: r.Value.To is not null ? [(r.Key, (string?)r.Value.To)] : (r.Value.Many ?? []).Select(x => (r.Key, x.To))
			)
			.Where(t => t.To is not null)
			.Select(t => (t.Key, t.To!));

	// An external target must be an absolute URI; a local one must be an existing Markdown file in the documentation source directory.
	private static string? RedirectTargetProblem(DocumentationSetInCatalog m, (string From, string To) redirect)
	{
		var to = redirect.To;
		var problem = to.Contains("://", StringComparison.Ordinal)
			? Uri.TryCreate(to, UriKind.Absolute, out _) ? null : "which is not a valid URI"
			: !m.Docset.FileSystem.File.Exists(m.Docset.FileSystem.Path.Join(m.Docset.SourceDirectory.FullName, to))
				? "which does not exist"
				: to.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ? null : "which is not a markdown file";
		return problem is null ? null : $"Redirect {redirect.From} points to {to} {problem}";
	}

	// ── api ────────────────────────────────────────────────────────────────────────────────────────────────────────

	/// <summary>An api key is used as <c>api/&lt;key&gt;/</c> and in URLs: letters, digits, <c>.</c>, <c>_</c> and <c>-</c>, starting with a letter or digit.</summary>
	[GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9._-]*$")]
	private static partial Regex ApiKeySegment();

	// API Explorer reserves the other child slugs by failing the build; 'tags' is only documented as reserved, so it warns.
	private const string DocumentedReservedChildSlug = "tags";

	/// <summary>
	/// The URL slug API Explorer gives a child page: the file name without extension or <c>.vN</c> version suffix,
	/// lowercased, with <c>-</c> for spaces and underscores.
	/// </summary>
	public static string ApiChildSlug(string fileName)
	{
		var stem = Path.GetFileNameWithoutExtension(fileName);
		var dot = stem.LastIndexOf(".v", StringComparison.OrdinalIgnoreCase);
		if (dot > 0 && int.TryParse(stem[(dot + 2)..], out var major) && major >= 0)
			stem = stem[..dot];
		return stem.ToLowerInvariant().Replace(' ', '-').Replace('_', '-');
	}

	private static IEnumerable<(string Api, string Child)> ApiChildren(DocumentationSetDefinition docset) =>
		docset.Api.SelectMany(
			api => api
				.Entries
				.SelectMany(e => e.Children.OfType<string>().Where(c => !string.IsNullOrWhiteSpace(c)))
				.Select(c => (api.ProductKey, c))
		);

	private static Violation At(string message, SourceLocation? location, object? evidence) =>
		Violation.Error(message) with { Line = location?.Line, Column = location?.Column, Evidence = evidence };

	// An api entry bound to its product, checked in order: one entry, a known product, a spec that names a file inside the docset and is safe to read,
	// a well-formed repository, then its catalog categories and child pages. The first failure of the entry itself stops; categories and children
	// each report every bad value. What is wrong with the entry itself is evidence RejectedApi, with a child RejectedApiChild: resolution drops those.
	private static IEnumerable<Violation> ApiProblems(DocumentationSetInCatalog m, ApiDeclaration api)
	{
		var docset = m.Docset;
		var productKey = api.ProductKey;
		var rejected = new RejectedApi(productKey);
		if (api.Entries.Count != 1)
		{
			yield return Violation.Error(
				$"API configuration for '{productKey}' must have exactly one entry, found {api.Entries.Count}."
			) with
			{ Evidence = rejected };
			yield break;
		}

		var entry = api.Entries[0];
		if (string.IsNullOrWhiteSpace(entry.Product))
		{
			yield return At(
				$"API '{productKey}' is missing required 'product:'. It must match a product id defined in products.yml.",
				entry.Location,
				rejected
			);
			yield break;
		}

		var normalizedProduct = ProductNames.Normalize(entry.Product);
		if (!m.Products.Products.ContainsKey(normalizedProduct))
		{
			var hint = new Suggestion(m.Products.Products.Keys.ToHashSet(), normalizedProduct).GetSuggestionQuestion();
			yield return At(
				$"Unknown 'product: {entry.Product}' for API '{productKey}'. It must be a product id defined in products.yml.{(string.IsNullOrEmpty(hint) ? "" : $" {hint}")}",
				entry.ProductLocation,
				rejected
			);
			yield break;
		}

		if (string.IsNullOrWhiteSpace(entry.Spec))
		{
			yield return At(
				$"API '{productKey}' is missing required 'spec:'. Its basename is required to resolve the remote version index, even when the file is not present locally.",
				entry.Location,
				rejected
			);
			yield break;
		}

		var specLocation = entry.SpecLocation ?? entry.Location;
		if (string.IsNullOrEmpty(Path.GetFileName(entry.Spec.Trim())))
		{
			yield return At($"'spec: {entry.Spec}' for API '{productKey}' does not resolve to a file name.", specLocation, rejected);
			yield break;
		}

		var sourceDirectory = docset.SourceDirectory;
		var specFile = docset.FileSystem.FileInfo.New(Path.GetFullPath(Path.Join(sourceDirectory.FullName, entry.Spec)));
		if (!specFile.IsSubPathOf(sourceDirectory))
		{
			yield return At(
				$"'spec: {entry.Spec}' for API '{productKey}' escapes the documentation source directory.",
				specLocation,
				rejected
			);
			yield break;
		}

		// A missing local file is expected, not an error: docsets that don't carry the spec
		// locally resolve the current version from S3 via the version index instead.
		if (specFile.Exists && ValidateFileAccess(specFile, sourceDirectory) is { } symlinkError)
		{
			yield return At($"'spec: {entry.Spec}' for API '{productKey}' is unsafe: {symlinkError}", specLocation, rejected);
			yield break;
		}

		if (!string.IsNullOrWhiteSpace(entry.Repository))
		{
			var candidate = entry.Repository.Trim();
			var separator = candidate.IndexOf('/');
			if (!(separator > 0 && separator < candidate.Length - 1))
			{
				yield return At(
					$"'repository: {entry.Repository}' for API '{productKey}' must be in 'org/repo' form, e.g. 'elastic/elasticsearch-specification'.",
					entry.RepositoryLocation ?? entry.Location,
					rejected
				);
				yield break;
			}
		}

		// Children resolve only under 'api/<key>/'; escaping paths and symlinks are rejected the same way branding image paths are.
		var childrenDirectory = docset.FileSystem.DirectoryInfo.New(Path.Join(sourceDirectory.FullName, "api", productKey));
		foreach (var child in entry.Children)
		{
			if (string.IsNullOrWhiteSpace(child))
			{
				yield return Violation.Error($"A 'children' entry for API '{productKey}' is missing a 'file' value.");
				continue;
			}

			var drops = new RejectedApiChild(productKey, child);
			var childFile = docset.FileSystem.FileInfo.New(Path.GetFullPath(Path.Join(childrenDirectory.FullName, child)));
			if (!childFile.IsSubPathOf(childrenDirectory))
				yield return Violation.Error($"Child page '{child}' for API '{productKey}' escapes 'api/{productKey}/'.") with
				{
					Evidence = drops
				};
			else if (ValidateFileAccess(childFile, childrenDirectory) is { } childSymlinkError)
				yield return Violation.Error($"Child page '{child}' for API '{productKey}' is unsafe: {childSymlinkError}") with
				{
					Evidence = drops
				};
			else if (!childFile.Exists)
				yield return Violation.Error($"Child page '{child}' for API '{productKey}' does not exist under 'api/{productKey}/'.") with
				{
					Evidence = drops
				};
			else if (
				childFile.Directory is not null
				&& string.Equals(childFile.Directory.FullName, childrenDirectory.FullName, StringComparison.OrdinalIgnoreCase)
				&& ResolvedApiConfiguration.IsSupplementalFileName(childFile.Name)
			)
				yield return Violation.Error(
					$"Child page '{child}' for API '{productKey}' uses a supplemental file name (op-*.md / tag-*.md). Those files are auto-discovered and cannot be listed under children:."
				) with
				{ Evidence = drops };
			else if (ApiChildSlug(childFile.Name) == DocumentedReservedChildSlug)
				yield return Violation.Warning(
					$"Child page '{child}' for API '{productKey}' uses the slug '{DocumentedReservedChildSlug}', which is reserved for API Explorer. Rename the file."
				);
		}

		var categoriesLocation = entry.CatalogLocation ?? entry.Location;
		foreach (var item in entry.CatalogCategories.Where(item => ApiCatalogCategory.Normalize(item) is null))
		{
			yield return At(
				$"Unknown catalog category '{item}' for API '{productKey}'. Valid categories: {string.Join(", ", ApiCatalogCategory.DisplayOrder)} (or {ApplicabilityKeys.Ech} for {ApplicabilityKeys.Ess}).",
				categoriesLocation,
				null
			);
		}
	}
}
