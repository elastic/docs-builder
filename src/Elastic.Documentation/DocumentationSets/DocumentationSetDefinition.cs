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
/// <remarks>
/// Constructed with <c>DocumentationSetLoader.Load</c>.
/// Members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can build one directly.
/// </remarks>
[Equatable]
public sealed partial record DocumentationSetDefinition
{
	/// <summary>The <c>docset.yml</c> (or <c>_docset.yml</c>) this set is declared in. It need not exist.</summary>
	[CustomEquality(typeof(FileSystemInfoLocationComparer))]
	public IFileInfo SourceFile { get; internal init; } = default!;

	/// <summary>The documentation source directory that docset-relative paths resolve from.</summary>
	[CustomEquality(typeof(FileSystemInfoLocationComparer))]
	public IDirectoryInfo SourceDirectory { get; internal init; } = default!;

	/// <summary>The file system the set's files are read through; branding images and API specs are checked on it.</summary>
	[IgnoreEquality]
	public IFileSystem FileSystem { get; internal init; } = default!;

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

	/// <summary><c>subs</c> as written, in declaration order; keys may collide when compared case-insensitively.</summary>
	[OrderedEquality]
	public IReadOnlyList<KeyValuePair<string, string>> Substitutions { get; internal init; } = [];

	[Obsolete("Use the index.md h1 heading instead. This field will be removed in a future version.")]
	public string? DisplayName { get; internal init; }

	[Obsolete("Use the index.md frontmatter description instead. This field will be removed in a future version.")]
	public string? Description { get; internal init; }

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

	[OrderedEquality]
	public IReadOnlyList<TocItem> TableOfContents { get; internal init; } = [];

	/// <summary>Files excluded by a folder-level <c>exclude</c> in the table of contents, docset-relative.</summary>
	[SetEquality]
	public IReadOnlySet<string> FolderExcludedFiles { get; internal init; } = new HashSet<string>();

	/// <summary>Pages given a default CTA by <c>default_cta</c> in the table of contents, keyed by docset-relative path.</summary>
	[OrderedEquality]
	public IReadOnlyList<KeyValuePair<string, string>> TocDefaultCtas { get; internal init; } = [];

	/// <summary><c>redirects.yml</c>, or null when the docset has none.</summary>
	[UnorderedEquality]
	public IReadOnlyDictionary<string, LinkRedirect>? Redirects { get; internal init; }

	/// <summary>The redirects file the loader looked for (<c>redirects.yml</c>, or <c>_redirects.yml</c> for a <c>_</c>-prefixed docset file); null when the model was not loaded from a file. It need not exist.</summary>
	[IgnoreEquality]
	public IFileInfo? RedirectsFile { get; internal init; }

	/// <summary>Redirect entries and <c>many</c> items the reader dropped because they hold nothing it can use.</summary>
	[IgnoreEquality]
	public IReadOnlyList<DroppedRedirect> DroppedRedirects { get; internal init; } = [];

	/// <summary>The <c>suppress</c> names the reader does not recognise as a hint type and drops, as written.</summary>
	[IgnoreEquality]
	public IReadOnlyList<string> UnknownSuppressions { get; internal init; } = [];

	/// <summary>Every <c>toc:</c> entry mapping of <c>docset.yml</c> and the nested <c>toc.yml</c> files that were read, with what the reader hid.</summary>
	[IgnoreEquality]
	public IReadOnlyList<TocEntryAuthoring> TocAuthoring { get; internal init; } = [];
}

/// <summary>A redirect the reader drops: the whole entry for <see cref="Source"/>, or its <c>many</c> item number <see cref="ManyItem"/> (0-based).</summary>
/// <remarks>
/// Constructed through <c>DocumentationSetDefinition</c>.
/// Members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can build one directly.
/// </remarks>
public sealed record DroppedRedirect
{
	internal DroppedRedirect(string source, int? manyItem = null)
	{
		Source = source;
		ManyItem = manyItem;
	}

	public string Source { get; internal init; }

	public int? ManyItem { get; internal init; }

	public void Deconstruct(out string source, out int? manyItem)
	{
		source = Source;
		manyItem = ManyItem;
	}
}

/// <summary>The <c>features</c> toggles of <c>docset.yml</c>; null when not set.</summary>
/// <remarks>
/// Constructed through <c>DocumentationSetDefinition</c>.
/// Members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can build one directly.
/// </remarks>
public sealed record DeclaredFeatures
{
	internal DeclaredFeatures(bool? primaryNav = null, bool? disableGithubEditLink = null, bool? guideNav = null)
	{
		PrimaryNav = primaryNav;
		DisableGithubEditLink = disableGithubEditLink;
		GuideNav = guideNav;
	}

	public bool? PrimaryNav { get; internal init; }

	public bool? DisableGithubEditLink { get; internal init; }

	public bool? GuideNav { get; internal init; }

	public void Deconstruct(out bool? primaryNav, out bool? disableGithubEditLink, out bool? guideNav)
	{
		primaryNav = PrimaryNav;
		disableGithubEditLink = DisableGithubEditLink;
		guideNav = GuideNav;
	}
}

/// <summary><c>branding</c> as written: image paths relative to the documentation source directory.</summary>
/// <remarks>
/// Constructed through <c>DocumentationSetDefinition</c>.
/// Members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can build one directly.
/// </remarks>
public sealed record BrandingDeclaration
{
	internal BrandingDeclaration(
		string? icon = null,
		string? headerBg = null,
		string? ogImage = null,
		string? favicon = null,
		string? appleTouchIcon = null
	)
	{
		Icon = icon;
		HeaderBg = headerBg;
		OgImage = ogImage;
		Favicon = favicon;
		AppleTouchIcon = appleTouchIcon;
	}

	public string? Icon { get; internal init; }

	public string? HeaderBg { get; internal init; }

	public string? OgImage { get; internal init; }

	public string? Favicon { get; internal init; }

	public string? AppleTouchIcon { get; internal init; }

	public void Deconstruct(out string? icon, out string? headerBg, out string? ogImage, out string? favicon, out string? appleTouchIcon)
	{
		icon = Icon;
		headerBg = HeaderBg;
		ogImage = OgImage;
		favicon = Favicon;
		appleTouchIcon = AppleTouchIcon;
	}
}

/// <summary>A <c>cta</c> template as written.</summary>
/// <remarks>
/// Constructed through <c>DocumentationSetDefinition</c>.
/// Members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can build one directly.
/// </remarks>
[Equatable]
public sealed partial record CtaDeclaration
{
	internal CtaDeclaration(string name, string? label, string? url, IReadOnlyList<string> benefits)
	{
		Name = name;
		Label = label;
		Url = url;
		Benefits = benefits;
	}

	public string Name { get; internal init; }

	public string? Label { get; internal init; }

	public string? Url { get; internal init; }

	[OrderedEquality]
	public IReadOnlyList<string> Benefits { get; internal init; }

	public void Deconstruct(out string name, out string? label, out string? url, out IReadOnlyList<string> benefits)
	{
		name = Name;
		label = Label;
		url = Url;
		benefits = Benefits;
	}
}

/// <summary>A key of the <c>api</c> map: normally a single entry; any other count is invalid.</summary>
/// <remarks>
/// Constructed through <c>DocumentationSetDefinition</c>.
/// Members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can build one directly.
/// </remarks>
[Equatable]
public sealed partial record ApiDeclaration
{
	internal ApiDeclaration(string productKey, IReadOnlyList<ApiDeclarationEntry> entries)
	{
		ProductKey = productKey;
		Entries = entries;
	}

	public string ProductKey { get; internal init; }

	[OrderedEquality]
	public IReadOnlyList<ApiDeclarationEntry> Entries { get; internal init; }

	public void Deconstruct(out string productKey, out IReadOnlyList<ApiDeclarationEntry> entries)
	{
		productKey = ProductKey;
		entries = Entries;
	}
}

/// <summary>One <c>api</c> entry as written, with where its keys are in <c>docset.yml</c>.</summary>
/// <remarks>
/// Constructed through <c>ApiDeclaration</c>.
/// Members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can build one directly.
/// </remarks>
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

	/// <summary>The keys of the entry the reader does not read and drops, in the order written.</summary>
	[IgnoreEquality]
	public IReadOnlyList<string> UnknownKeys { get; internal init; } = [];

	public SourceLocation? Location { get; internal init; }
	public SourceLocation? ProductLocation { get; internal init; }
	public SourceLocation? SpecLocation { get; internal init; }
	public SourceLocation? RepositoryLocation { get; internal init; }
	public SourceLocation? CatalogLocation { get; internal init; }
}

/// <summary>A line and column in a YAML file.</summary>
public readonly record struct SourceLocation(int Line, int Column);
