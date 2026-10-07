// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using System.IO.Abstractions;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Equality;
using Elastic.Documentation.Links;
using Generator.Equals;
using NetEscapades.EnumGenerators;

namespace Elastic.Documentation.DocumentationSets;

/// <summary>
/// A documentation set as a build uses it: <c>docset.yml</c>, every nested <c>toc.yml</c> and <c>redirects.yml</c>,
/// validated and joined against the <see cref="GlobalCatalog"/>. Produced once per build (or serve reload) by
/// <c>DocumentationSetResolver</c>; holds no I/O and no runtime state.
/// </summary>
/// <remarks>
/// Constructed with <c>DocumentationSetResolver</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
[Equatable]
public sealed partial record ResolvedDocumentationSet
{
	/// <summary>The <c>docset.yml</c> (or <c>_docset.yml</c>) this set was loaded from.</summary>
	[CustomEquality(typeof(FileSystemInfoLocationComparer))]
	public required IFileInfo SourceFile { get; init; }

	/// <summary>The directory holding <see cref="SourceFile"/>; the root every docset-relative path resolves from.</summary>
	[CustomEquality(typeof(FileSystemInfoLocationComparer))]
	public required IDirectoryInfo ScopeDirectory { get; init; }

	public string? Project { get; internal init; }

	/// The maximum depth `toc.yml` files may appear
	public int MaxTocDepth { get; internal init; } = 1;

	/// This is a documentation set not linked to by assembler.
	/// Setting this to true relaxes a few restrictions such as mixing toc references with file and folder reference
	public bool DevelopmentDocs { get; internal init; }

	[DefaultEquality]
	public DocumentationSetExclusions Exclusions { get; internal init; } = DocumentationSetExclusions.None;

	/// <summary>Registry for this documentation set. <c>Public</c> uses S3 link index; other values use codex-link-index.</summary>
	public DocSetRegistry Registry { get; internal init; } = DocSetRegistry.Public;

	/// <summary>Parsed cross-link entries with registry for each target.</summary>
	[OrderedEquality]
	public IReadOnlyList<CrossLinkEntry> CrossLinkEntries { get; internal init; } = [];

	[OrderedEquality]
	public IReadOnlyList<string> CrossLinkRepositories { get; internal init; } = [];

	/// <summary>
	/// Canonical product ids declared under <c>release_notes</c> whose changelog content is sourced from
	/// the public CDN. Validated against products.yml; drives startup prefetch for the <c>{changelog}</c>
	/// directive and CDN sourcing for <c>changelog bundle</c>.
	/// </summary>
	[OrderedEquality]
	public IReadOnlyList<string> ReleaseNotesProducts { get; internal init; } = [];

	[DefaultEquality]
	public EnabledExtensions Extensions { get; internal init; } = EnabledExtensions.None;

	[UnorderedEquality]
	public IReadOnlyDictionary<string, LinkRedirect>? Redirects { get; internal init; }

	// Product is a record of scalars, so compiler record equality is already structural; it needs no [Equatable].
#pragma warning disable GE003
	[SetEquality]
	public IReadOnlySet<Product> Products { get; internal init; } = new HashSet<Product>();
#pragma warning restore GE003

	/// <summary>Authored <c>subs</c> merged with the generated <c>{{version.*}}</c> and <c>{{product.*}}</c> substitutions.</summary>
	[UnorderedEquality]
	public IReadOnlyDictionary<string, string> Substitutions { get; internal init; } = new Dictionary<string, string>();

	/// <summary>The <c>features</c> toggles set in <c>docset.yml</c>, keyed by flag name.</summary>
	[UnorderedEquality]
	public IReadOnlyDictionary<string, bool> FeatureToggles { get; internal init; } = new Dictionary<string, bool>();

	public string? StorybookRegistry { get; internal init; }

	/// <summary>
	/// Environment-independent <c>storybook.registry</c> value (committed default). Non-null only when an allow-listed
	/// environment variable changed <see cref="StorybookRegistry"/>, so the directive can degrade to the committed
	/// registry when the environment-supplied one is unreachable (e.g. an ephemeral PR registry that is not yet published).
	/// </summary>
	public string? StorybookRegistryFallback { get; internal init; }

	/// <summary>Resolved API configurations with template and specification file information.</summary>
	[UnorderedEquality]
	public IReadOnlyDictionary<string, ResolvedApiConfiguration>? ApiConfigurations { get; internal init; }

	/// <summary>Set of diagnostic hint types to suppress for this documentation set.</summary>
	[SetEquality]
	public IReadOnlySet<HintType> SuppressDiagnostics { get; internal init; } = new HashSet<HintType>();

	/// <summary>White-label branding overrides. When non-null, all Elastic-specific chrome is suppressed.</summary>
	[DefaultEquality]
	public Branding? Branding { get; internal init; }

	/// <summary>
	/// Named right-gutter CTA templates declared under <c>docset.yml</c>'s <c>cta</c> map, keyed by name.
	/// Always contains at least the built-in <see cref="Cta.DefaultName"/> entry.
	/// </summary>
	[UnorderedEquality]
	public IReadOnlyDictionary<string, Cta> Ctas { get; internal init; } = new Dictionary<string, Cta> { [Cta.DefaultName] = Cta.Default };

	/// <summary>Pages registered with a default CTA via <c>default_cta</c>, keyed by docset-relative path.</summary>
	[UnorderedEquality]
	public IReadOnlyDictionary<string, string> TocDefaultCtas { get; internal init; } = new Dictionary<string, string>();

	[OrderedEquality]
	public IReadOnlyList<TocItem> TableOfContents { get; internal init; } = [];
}

/// <summary>
/// Which docset-relative paths the build skips: <c>exclude</c> globs, <c>!</c>-prefixed overrides that re-include a
/// single path, and files excluded by a folder-level <c>exclude</c> in the table of contents. Matching lives with the
/// loader, since glob semantics are not part of the domain.
/// </summary>
/// <remarks>
/// Constructed through <c>ResolvedDocumentationSet</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
[Equatable]
public sealed partial record DocumentationSetExclusions
{
	internal DocumentationSetExclusions(
		IReadOnlyList<string> excludeGlobs,
		IReadOnlyList<string> includeOverrides,
		IReadOnlySet<string> folderExcludedFiles
	)
	{
		ExcludeGlobs = excludeGlobs;
		IncludeOverrides = includeOverrides;
		FolderExcludedFiles = folderExcludedFiles;
	}

	[OrderedEquality]
	public IReadOnlyList<string> ExcludeGlobs { get; internal init; }

	[OrderedEquality]
	public IReadOnlyList<string> IncludeOverrides { get; internal init; }

	[SetEquality]
	public IReadOnlySet<string> FolderExcludedFiles { get; internal init; }

	public void Deconstruct(
		out IReadOnlyList<string> excludeGlobs,
		out IReadOnlyList<string> includeOverrides,
		out IReadOnlySet<string> folderExcludedFiles
	)
	{
		excludeGlobs = ExcludeGlobs;
		includeOverrides = IncludeOverrides;
		folderExcludedFiles = FolderExcludedFiles;
	}

	public static DocumentationSetExclusions None { get; } = new([], [], new HashSet<string>());
}

/// <summary>
/// Registry type for cross-link resolution. Maps to the link index source:
/// <c>Public</c> uses the S3-based public link index; other values use the codex-link-index for that environment.
/// </summary>
[EnumExtensions]
public enum DocSetRegistry
{
	/// <summary>Public documentation; uses S3-based link index.</summary>
	[Display(Name = "public")]
	Public,

	/// <summary>Internal codex environment; uses codex-link-index/internal/.</summary>
	[Display(Name = "internal")]
	Internal,
}

/// <summary>A parsed cross-link entry from docset.yml, with the target registry for lookup.</summary>
/// <remarks>
/// Constructed through <c>ResolvedDocumentationSet</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
/// <param name="Repository">Repository name (e.g. elasticsearch, docs-eng-team).</param>
/// <param name="Registry">Registry to use for lookup (public S3 or codex environment).</param>
public record CrossLinkEntry
{
	internal CrossLinkEntry(string repository, DocSetRegistry registry)
	{
		Repository = repository;
		Registry = registry;
	}

	public string Repository { get; internal init; }

	public DocSetRegistry Registry { get; internal init; }

	public void Deconstruct(out string repository, out DocSetRegistry registry)
	{
		repository = Repository;
		registry = Registry;
	}
}

/// <summary>The <c>extensions</c> enabled in <c>docset.yml</c>.</summary>
/// <remarks>
/// Constructed through <c>ResolvedDocumentationSet</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
[Equatable]
public sealed partial record EnabledExtensions
{
	internal EnabledExtensions(IReadOnlySet<string> enabled) => Enabled = enabled;

	[SetEquality]
	public IReadOnlySet<string> Enabled { get; internal init; }

	public void Deconstruct(out IReadOnlySet<string> enabled) => enabled = Enabled;

	public static EnabledExtensions None { get; } = new(new HashSet<string>());

	internal EnabledExtensions(IEnumerable<string> extensions) : this(new HashSet<string>(extensions)) { }

	public bool IsDetectionRulesEnabled => Enabled.Contains("detection-rules");
}

/// <summary>White-label branding from <c>docset.yml</c>'s <c>branding</c>, with image paths validated against the docset.</summary>
/// <remarks>
/// Constructed through <c>ResolvedDocumentationSet</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
public sealed record Branding
{
	public string? Icon { get; internal init; }
	public string? HeaderBg { get; internal init; }
	public string? OgImage { get; internal init; }
	public string? Favicon { get; internal init; }
	public string? AppleTouchIcon { get; internal init; }
}

/// <summary>A right-gutter call-to-action template.</summary>
/// <remarks>
/// Constructed through <c>ResolvedDocumentationSet</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
[Equatable]
public sealed partial record Cta
{
	/// <summary>Name of the template this was resolved from; <see cref="DefaultName"/> for the built-in default.</summary>
	public required string Name { get; init; }
	public required string Label { get; init; }
	public required string Url { get; init; }

	[OrderedEquality]
	public IReadOnlyList<string> Benefits { get; internal init; } = [];

	/// <summary>The built-in default, reproducing the CTA that renders when no <c>cta:</c> config exists.</summary>
	public const string DefaultName = "trial";

	/// <summary>Right-gutter card space is limited; benefit bullet lists are capped at this many entries.</summary>
	public const int MaxBenefits = 3;

	public static Cta Default { get; } = new()
	{
		Name = DefaultName,
		Label = "Get started free",
		Url = "https://cloud.elastic.co/registration?page=docs&placement=docs-siderail",
		Benefits = ["14-day free trial", "All features included", "No setup required"]
	};
}

/// <summary>An <c>api</c> entry of <c>docset.yml</c>, validated and bound to its <see cref="Catalog.Product"/>.</summary>
/// <remarks>
/// Constructed through <c>ResolvedDocumentationSet</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
[Equatable]
public sealed partial record ResolvedApiConfiguration
{
	public required string ProductKey { get; init; }

	/// <summary>The <c>products.yml</c> product this API key binds to.</summary>
	[DefaultEquality]
	public required Product Product { get; init; }

	/// <summary>
	/// The basename (including extension) of the declared <c>spec:</c> value, e.g.
	/// <c>elasticsearch-openapi.json</c>. This is the <c>&lt;spec-name&gt;</c> segment used to
	/// resolve the remote version index at the root <c>index.json</c> keyed by
	/// <c>&lt;org/repo&gt;</c> and this basename,
	/// regardless of whether <see cref="LocalSpecFile"/> is present.
	/// </summary>
	public required string SpecFileName { get; init; }

	/// <summary>
	/// Local override for the current OpenAPI specification file, present only when a file exists
	/// on disk at the path declared via <c>spec:</c>. Null means the current version resolves
	/// remotely through the product's version index — this is expected, not an error, for any
	/// docset that does not carry the spec file locally.
	/// </summary>
	[CustomEquality(typeof(FileSystemInfoLocationComparer))]
	public IFileInfo? LocalSpecFile { get; internal init; }

	/// <summary>
	/// Optional <c>org/repo</c> override from <c>repository:</c>, used instead of the current
	/// checkout's GitHub remote to look up this API's entry in the root version index. Null means
	/// use the checkout's own remote (the common case: a repo consuming its own published spec).
	/// </summary>
	public string? Repository { get; internal init; }

	/// <summary>
	/// Explicit child pages declared via <c>children:</c>, resolved to files under <c>api/&lt;key&gt;/</c>,
	/// in declared order.
	/// </summary>
	[OrderedEquality(typeof(FileSystemInfoLocationComparer))]
	public IReadOnlyList<IFileInfo> Children { get; internal init; } = [];

	/// <summary>
	/// The <c>api/&lt;key&gt;/</c> directory for this product, whether or not it exists yet.
	/// Supplemental <c>op-*.md</c> / <c>tag-*.md</c> files are discovered from here.
	/// </summary>
	[CustomEquality(typeof(FileSystemInfoLocationComparer))]
	public IDirectoryInfo? ApiContentDirectory { get; internal init; }

	/// <summary>
	/// Normalized catalog categories (<c>ece</c>, <c>ess</c>, <c>self</c>, <c>serverless</c>).
	/// Empty when the API is unclassified and appears only under All.
	/// </summary>
	[OrderedEquality]
	public IReadOnlyList<string> CatalogCategories { get; internal init; } = [];

	/// <summary>
	/// Whether <paramref name="fileName"/> is an auto-discovered supplemental file
	/// (<c>op-*.md</c> or <c>tag-*.md</c>), including version-suffixed names.
	/// </summary>
	public static bool IsSupplementalFileName(string fileName)
	{
		var name = Path.GetFileName(fileName);
		return name.StartsWith("op-", StringComparison.OrdinalIgnoreCase) || name.StartsWith("tag-", StringComparison.OrdinalIgnoreCase);
	}
}
