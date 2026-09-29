// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.DocumentationSets;

namespace Elastic.Documentation.Configuration.Toc;

// The toc: entries of docset.yml and toc.yml exactly as authored, before resolution. Paths are as written,
// relative to wherever the entry sits. DocumentationSetFile resolves these into the domain TocItem tree.

internal interface ITocEntry
{
	IReadOnlyList<ITocEntry> Children { get; }
}

/// <summary>A list of <c>toc:</c> entries, bound by <see cref="TocItemCollectionYamlConverter"/>.</summary>
internal sealed class TocEntryList : List<ITocEntry>;

/// <summary>A bare scalar (e.g. <c>- toc.yml</c>) where an entry mapping was expected; resolution reports it.</summary>
internal sealed record InvalidTocEntry(string RawValue) : ITocEntry
{
	public IReadOnlyList<ITocEntry> Children => [];
}

/// <summary><c>file:</c> or <c>hidden:</c>.</summary>
internal record FileEntry(string Path, bool Hidden, IReadOnlyList<ITocEntry> Children, string? DefaultCta = null) : ITocEntry;

/// <summary>A <c>file: index.md</c> or <c>hidden: index.md</c>.</summary>
internal sealed record IndexFileEntry(string Path, bool Hidden, IReadOnlyList<ITocEntry> Children, string? DefaultCta = null) : FileEntry(
	Path,
	Hidden,
	Children,
	DefaultCta
);

/// <summary>The <c>file:</c> of a <c>folder:</c> + <c>file:</c> entry, placed first among the folder's children.</summary>
internal sealed record FolderIndexFileEntry(string Path) : FileEntry(Path, false, []);

/// <summary><c>detection_rules:</c> + <c>file:</c>.</summary>
internal sealed record DetectionRulesEntry(
	string Path,
	IReadOnlyList<string> RuleFolders,
	IReadOnlyList<ITocEntry> Children,
	string? DeprecatedFile
) : FileEntry(Path, false, Children);

/// <summary><c>folder:</c>, with or without <c>file:</c>.</summary>
internal record FolderEntry(
	string Path,
	IReadOnlyList<ITocEntry> Children,
	string? Sort = null,
	IReadOnlyList<string>? Exclude = null,
	string? DefaultCta = null
) : ITocEntry;

/// <summary>The childless <c>file: subdir/index.md</c> sugar, expanded to a single-page folder.</summary>
internal sealed record DeepLinkedFolderEntry(string Path, IReadOnlyList<ITocEntry> Children, string? DefaultCta) : FolderEntry(
	Path,
	Children,
	DefaultCta: DefaultCta
);

/// <summary><c>toc:</c>, pointing at a folder holding a nested <c>toc.yml</c>.</summary>
internal sealed record NestedTocEntry(string Path, IReadOnlyList<ITocEntry> Children, bool Island) : ITocEntry;

/// <summary><c>crosslink:</c>.</summary>
internal sealed record CrossLinkTocEntry(Uri CrossLinkUri, string? Title, bool Hidden, IReadOnlyList<ITocEntry> Children) : ITocEntry;

/// <summary><c>listing:</c>.</summary>
internal sealed record ListingEntry(string Path, IReadOnlyList<ITocEntry> Children, ListingOptions Options) : ITocEntry;

/// <summary><c>cli:</c>.</summary>
internal sealed record CliReferenceEntry(
	string SchemaPath,
	string? SupplementalFolder,
	string? Title,
	string? NavigationTitle,
	IReadOnlyList<ITocEntry> Children,
	ApplicableTo? AppliesTo
) : ITocEntry;
