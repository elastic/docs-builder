// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Equality;
using Generator.Equals;

namespace Elastic.Documentation.DocumentationSets;

/// <summary>
/// A documentation set as a build uses it: a <see cref="DocumentationSetDefinition"/> joined against the catalog and the files next to it.
/// Holds no I/O and no runtime state.
/// </summary>
/// <remarks>
/// Redirects and the table of contents are read separately and are not part of this record yet.
/// Constructed by the documentation set resolver. Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
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

	// Product is a record of scalars, so compiler record equality is already structural; it needs no [Equatable].
#pragma warning disable GE003
	[SetEquality]
	public IReadOnlySet<Product> Products { get; internal init; } = new HashSet<Product>();
#pragma warning restore GE003

	/// <summary>Authored <c>subs</c> merged with the generated <c>{{version.*}}</c> and <c>{{product.*}}</c> substitutions.</summary>
	[UnorderedEquality]
	public IReadOnlyDictionary<string, string> Substitutions { get; internal init; } = new Dictionary<string, string>(
		StringComparer.OrdinalIgnoreCase
	);

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
	public IReadOnlyDictionary<string, Cta> Ctas { get; internal init; } = new Dictionary<string, Cta>(StringComparer.OrdinalIgnoreCase)
	{
		[Cta.DefaultName] = Cta.Default
	};

	/// <summary>Pages registered with a default CTA via <c>default_cta</c>, keyed by docset-relative path.</summary>
	[UnorderedEquality]
	public IReadOnlyDictionary<string, string> TocDefaultCtas { get; internal init; } = new Dictionary<string, string>();
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
