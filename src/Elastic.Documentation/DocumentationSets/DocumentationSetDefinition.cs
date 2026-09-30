// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Equality;
using Elastic.Documentation.Links;
using Generator.Equals;

namespace Elastic.Documentation.DocumentationSets;

/// <summary>
/// A documentation set as declared: <c>docset.yml</c>, its table of contents with every nested <c>toc.yml</c> loaded,
/// and <c>redirects.yml</c>. References to other aggregates are ids, exactly as written. Nothing here is validated;
/// <see cref="DocumentationSetConstraints"/> checks it, and <c>DocumentationSetResolver</c> joins it against the
/// catalog into a <see cref="ResolvedDocumentationSet"/>.
/// </summary>
[Equatable]
public sealed partial record DocumentationSetDefinition
{
	/// <summary>The <c>docset.yml</c> (or <c>_docset.yml</c>) this set is declared in. It need not exist.</summary>
	[CustomEquality(typeof(FileSystemInfoLocationComparer))]
	public required IFileInfo SourceFile { get; init; }

	/// <summary>The documentation source directory that docset-relative paths resolve from.</summary>
	[CustomEquality(typeof(FileSystemInfoLocationComparer))]
	public required IDirectoryInfo SourceDirectory { get; init; }

	/// <summary>The file system the set's files are read through; branding images and API specs are checked on it.</summary>
	[IgnoreEquality]
	public required IFileSystem FileSystem { get; init; }

	public string? Project { get; init; }

	public int MaxTocDepth { get; init; } = 2;

	public bool DevDocs { get; init; }

	/// <summary>When the root table of contents renders as an island in an assembled build.</summary>
	public bool Island { get; init; }

	/// <summary><c>cross_links</c> as written: a repository name, optionally prefixed with its registry (<c>internal://repo</c>).</summary>
	[OrderedEquality]
	public IReadOnlyList<string> CrossLinks { get; init; } = [];

	/// <summary>The <c>product</c> of each <c>release_notes</c> entry as written; null when an entry omits it.</summary>
	[OrderedEquality]
	public IReadOnlyList<string?> ReleaseNotesProducts { get; init; } = [];

	/// <summary><c>exclude</c> as written: globs, and <c>!</c>-prefixed paths that re-include a single file.</summary>
	[OrderedEquality]
	public IReadOnlyList<string> Exclude { get; init; } = [];

	[OrderedEquality]
	public IReadOnlyList<string> Extensions { get; init; } = [];

	/// <summary><c>subs</c> as written, in declaration order; keys may collide when compared case-insensitively.</summary>
	[OrderedEquality]
	public IReadOnlyList<KeyValuePair<string, string>> Substitutions { get; init; } = [];

	[Obsolete("Use the index.md h1 heading instead. This field will be removed in a future version.")]
	public string? DisplayName { get; init; }

	[Obsolete("Use the index.md frontmatter description instead. This field will be removed in a future version.")]
	public string? Description { get; init; }

	public string? Icon { get; init; }

	/// <summary><c>registry</c> as written; empty or <c>public</c> means the public link index.</summary>
	public string? Registry { get; init; }

	public DeclaredFeatures Features { get; init; } = new();

	/// <summary><c>api</c> entries by product key, in declaration order.</summary>
	[OrderedEquality]
	public IReadOnlyList<ApiDeclaration> Api { get; init; } = [];

	/// <summary><c>products[].id</c> as written.</summary>
	[OrderedEquality]
	public IReadOnlyList<string> Products { get; init; } = [];

	/// <summary><c>codex.group</c>: the codex landing-page group this set lists under.</summary>
	public string? CodexGroup { get; init; }

	[DefaultEquality]
	public BrandingDeclaration? Branding { get; init; }

	/// <summary><c>storybook.registry</c> as written, before environment interpolation; null when there is no <c>storybook</c> section.</summary>
	public string? StorybookRegistry { get; init; }

	/// <summary>Whether a <c>storybook</c> section is present, even without a <c>registry</c>.</summary>
	public bool HasStorybook { get; init; }

	/// <summary><c>cta</c> templates in declaration order.</summary>
	[OrderedEquality]
	public IReadOnlyList<CtaDeclaration> Ctas { get; init; } = [];

	[SetEquality]
	public IReadOnlySet<HintType> SuppressDiagnostics { get; init; } = new HashSet<HintType>();

	/// <summary><c>default_cta</c> of <c>docset.yml</c> itself.</summary>
	public string? DefaultCta { get; init; }

	[OrderedEquality]
	public IReadOnlyList<TocItem> TableOfContents { get; init; } = [];

	/// <summary>Files excluded by a folder-level <c>exclude</c> in the table of contents, docset-relative.</summary>
	[SetEquality]
	public IReadOnlySet<string> FolderExcludedFiles { get; init; } = new HashSet<string>();

	/// <summary>Pages given a default CTA by <c>default_cta</c> in the table of contents, keyed by docset-relative path.</summary>
	[OrderedEquality]
	public IReadOnlyList<KeyValuePair<string, string>> TocDefaultCtas { get; init; } = [];

	/// <summary><c>redirects.yml</c>, or null when the docset has none.</summary>
	[UnorderedEquality]
	public IReadOnlyDictionary<string, LinkRedirect>? Redirects { get; init; }
}

/// <summary>The <c>features</c> toggles of <c>docset.yml</c>; null when not set.</summary>
public sealed record DeclaredFeatures(bool? PrimaryNav = null, bool? DisableGithubEditLink = null, bool? GuideNav = null);

/// <summary><c>branding</c> as written: image paths relative to the documentation source directory.</summary>
public sealed record BrandingDeclaration(
	string? Icon = null,
	string? HeaderBg = null,
	string? OgImage = null,
	string? Favicon = null,
	string? AppleTouchIcon = null
);

/// <summary>A <c>cta</c> template as written.</summary>
[Equatable]
public sealed partial record CtaDeclaration(
	string Name,
	string? Label,
	string? Url,
	[property: OrderedEquality] IReadOnlyList<string> Benefits
);

/// <summary>A key of the <c>api</c> map: normally a single entry; any other count is invalid.</summary>
[Equatable]
public sealed partial record ApiDeclaration(string ProductKey, [property: OrderedEquality] IReadOnlyList<ApiDeclarationEntry> Entries);

/// <summary>One <c>api</c> entry as written, with where its keys are in <c>docset.yml</c>.</summary>
[Equatable]
public sealed partial record ApiDeclarationEntry
{
	public string? Spec { get; init; }
	public string? Product { get; init; }
	public string? Repository { get; init; }

	/// <summary>The <c>file</c> of each <c>children</c> entry; null when an entry omits it.</summary>
	[OrderedEquality]
	public IReadOnlyList<string?> Children { get; init; } = [];

	/// <summary><c>catalog.categories</c> as written; empty when there is no <c>catalog</c>.</summary>
	[OrderedEquality]
	public IReadOnlyList<string> CatalogCategories { get; init; } = [];

	public SourceLocation? Location { get; init; }
	public SourceLocation? ProductLocation { get; init; }
	public SourceLocation? SpecLocation { get; init; }
	public SourceLocation? RepositoryLocation { get; init; }
	public SourceLocation? CatalogLocation { get; init; }
}

/// <summary>A line and column in a YAML file.</summary>
public readonly record struct SourceLocation(int Line, int Column);
