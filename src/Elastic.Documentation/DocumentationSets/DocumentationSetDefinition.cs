// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Equality;
using Generator.Equals;
using Nullean.Entities;

namespace Elastic.Documentation.DocumentationSets;

/// <summary>
/// A documentation set as declared: <c>docset.yml</c> with its table of contents and every nested <c>toc.yml</c> resolved.
/// References to other aggregates are ids, exactly as written. Nothing here is validated against the catalog.
/// </summary>
/// <remarks>
/// Constructed with <c>DocumentationSetLoader</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
[Equatable]
public sealed partial record DocumentationSetDefinition : IHaveConstraints<DocumentationSetDefinition>
{
	public static ConstraintSet<DocumentationSetDefinition> Constraints => DocumentationSetConstraints.All;

	/// <summary>The <c>docset.yml</c> (or <c>_docset.yml</c>) this set is declared in. It need not exist.</summary>
	[CustomEquality(typeof(FileSystemInfoLocationComparer))]
	public required IFileInfo SourceFile { get; init; }

	/// <summary>The documentation source directory that docset-relative paths resolve from.</summary>
	[CustomEquality(typeof(FileSystemInfoLocationComparer))]
	public required IDirectoryInfo SourceDirectory { get; init; }

	public string? Project { get; internal init; }

	public int MaxTocDepth { get; internal init; } = 2;

	public bool DevDocs { get; internal init; }

	/// <summary>When the root table of contents renders as an island in an assembled build.</summary>
	public bool Island { get; internal init; }

	/// <summary><c>cross_links</c> as written: a repository name, optionally prefixed with its registry (<c>internal://repo</c>).</summary>
	[OrderedEquality]
	public IReadOnlyList<string> CrossLinks { get; internal init; } = [];

	/// <summary>The <c>product</c> of each <c>release_notes</c> entry as written; null when an entry omits it.</summary>
	[OrderedEquality]
	public IReadOnlyList<string?> ReleaseNotesProducts { get; internal init; } = [];

	/// <summary><c>exclude</c> as written: globs, and <c>!</c>-prefixed paths that re-include a single file.</summary>
	[OrderedEquality]
	public IReadOnlyList<string> Exclude { get; internal init; } = [];

	[OrderedEquality]
	public IReadOnlyList<string> Extensions { get; internal init; } = [];

	/// <summary><c>subs</c> as written; keys may collide when compared case-insensitively.</summary>
	[UnorderedEquality]
	public IReadOnlyDictionary<string, string> Substitutions { get; internal init; } = new Dictionary<string, string>();

	public string? Icon { get; internal init; }

	/// <summary><c>registry</c> as written; empty or <c>public</c> means the public link index.</summary>
	public string? Registry { get; internal init; }

	public DeclaredFeatures Features { get; internal init; } = new();

	/// <summary><c>api</c> entries by product key, in declaration order.</summary>
	[OrderedEquality]
	public IReadOnlyList<ApiDeclaration> Api { get; internal init; } = [];

	/// <summary><c>products[].id</c> as written.</summary>
	[OrderedEquality]
	public IReadOnlyList<string> Products { get; internal init; } = [];

	/// <summary><c>codex.group</c>: the codex landing-page group this set lists under.</summary>
	public string? CodexGroup { get; internal init; }

	[DefaultEquality]
	public BrandingDeclaration? Branding { get; internal init; }

	/// <summary><c>storybook.registry</c> as written, before environment interpolation; null when there is no <c>storybook</c> section.</summary>
	public string? StorybookRegistry { get; internal init; }

	/// <summary>Whether a <c>storybook</c> section is present, even without a <c>registry</c>.</summary>
	public bool HasStorybook { get; internal init; }

	/// <summary><c>cta</c> templates in declaration order.</summary>
	[OrderedEquality]
	public IReadOnlyList<CtaDeclaration> Ctas { get; internal init; } = [];

	[SetEquality]
	public IReadOnlySet<HintType> SuppressDiagnostics { get; internal init; } = new HashSet<HintType>();

	/// <summary><c>default_cta</c> of <c>docset.yml</c> itself.</summary>
	public string? DefaultCta { get; internal init; }

	/// <summary>The resolved table of contents. The entries compare by reference until they are value types.</summary>
	[IgnoreEquality]
	public IReadOnlyCollection<ITableOfContentsItem> TableOfContents { get; internal init; } = [];

	/// <summary>Files excluded by a folder-level <c>exclude</c> in the table of contents, docset-relative.</summary>
	[SetEquality]
	public IReadOnlySet<string> FolderExcludedFiles { get; internal init; } = new HashSet<string>();

	/// <summary>Pages given a default CTA by <c>default_cta</c> in the table of contents, keyed by docset-relative path.</summary>
	[UnorderedEquality]
	public IReadOnlyDictionary<string, string> TocDefaultCtas { get; internal init; } = new Dictionary<string, string>();
}

/// <summary>The <c>features</c> toggles of <c>docset.yml</c>; null when not set.</summary>
public sealed record DeclaredFeatures
{
	public bool? PrimaryNav { get; internal init; }

	public bool? DisableGithubEditLink { get; internal init; }

	public bool? GuideNav { get; internal init; }
}

/// <summary><c>branding</c> as written: image paths relative to the documentation source directory.</summary>
public sealed record BrandingDeclaration
{
	public string? Icon { get; internal init; }

	public string? HeaderBg { get; internal init; }

	public string? OgImage { get; internal init; }

	public string? Favicon { get; internal init; }

	public string? AppleTouchIcon { get; internal init; }

	public string? Copyright { get; internal init; }
}

/// <summary>A <c>cta</c> template as written.</summary>
[Equatable]
public sealed partial record CtaDeclaration
{
	public required string Name { get; init; }

	public string? Label { get; internal init; }

	public string? Url { get; internal init; }

	[OrderedEquality]
	public IReadOnlyList<string> Benefits { get; internal init; } = [];
}

/// <summary>A key of the <c>api</c> map: normally a single entry; any other count is invalid.</summary>
[Equatable]
public sealed partial record ApiDeclaration
{
	public required string ProductKey { get; init; }

	[OrderedEquality]
	public IReadOnlyList<ApiDeclarationEntry> Entries { get; internal init; } = [];

	public bool IsValid => Entries.Count == 1;

	public ApiDeclarationEntry? SingleEntry => Entries.Count == 1 ? Entries[0] : null;
}

/// <summary>One <c>api</c> entry as written, with where its keys are in <c>docset.yml</c>.</summary>
[Equatable]
public sealed partial record ApiDeclarationEntry
{
	public string? Spec { get; internal init; }
	public string? Product { get; internal init; }
	public string? Repository { get; internal init; }

	/// <summary>The <c>file</c> of each <c>children</c> entry; null when an entry omits it.</summary>
	[OrderedEquality]
	public IReadOnlyList<string?> Children { get; internal init; } = [];

	/// <summary><c>catalog.categories</c> as written; empty when there is no <c>catalog</c>.</summary>
	[OrderedEquality]
	public IReadOnlyList<string> CatalogCategories { get; internal init; } = [];

	public SourceLocation? Location { get; internal init; }
	public SourceLocation? ProductLocation { get; internal init; }
	public SourceLocation? SpecLocation { get; internal init; }
	public SourceLocation? RepositoryLocation { get; internal init; }
	public SourceLocation? CatalogLocation { get; internal init; }

	public bool HasSpec => !string.IsNullOrWhiteSpace(Spec);
	public bool HasProduct => !string.IsNullOrWhiteSpace(Product);
}

/// <summary>A line and column in a YAML file.</summary>
public readonly record struct SourceLocation(int Line, int Column);
