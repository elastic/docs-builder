// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.DocumentationSets;

namespace Elastic.Documentation.Configuration.Toc;

// The toc: entries of docset.yml and toc.yml exactly as authored, before resolution. Paths are as written,
// relative to wherever the entry sits. DocumentationSetFile resolves these into the domain TocItem tree.

internal interface ITocEntryDto
{
	IReadOnlyList<ITocEntryDto> Children { get; }
}

/// <summary>A list of <c>toc:</c> entries, bound by <see cref="TocItemCollectionYamlConverter"/>.</summary>
internal sealed class TocEntryListDto : List<ITocEntryDto>;

/// <summary>A bare scalar (e.g. <c>- toc.yml</c>) where an entry mapping was expected; resolution reports it.</summary>
internal sealed record InvalidTocEntryDto(string RawValue) : ITocEntryDto
{
	public IReadOnlyList<ITocEntryDto> Children => [];
}

/// <summary><c>file:</c> or <c>hidden:</c>.</summary>
internal record FileEntryDto(string Path, bool Hidden, IReadOnlyList<ITocEntryDto> Children, string? DefaultCta = null) : ITocEntryDto;

/// <summary>A <c>file: index.md</c> or <c>hidden: index.md</c>.</summary>
internal sealed record IndexFileEntryDto(
	string Path,
	bool Hidden,
	IReadOnlyList<ITocEntryDto> Children,
	string? DefaultCta = null
) : FileEntryDto(Path, Hidden, Children, DefaultCta);

/// <summary>The <c>file:</c> of a <c>folder:</c> + <c>file:</c> entry, placed first among the folder's children.</summary>
internal sealed record FolderIndexFileEntryDto(string Path) : FileEntryDto(Path, false, []);

/// <summary><c>detection_rules:</c> + <c>file:</c>.</summary>
internal sealed record DetectionRulesEntryDto(
	string Path,
	IReadOnlyList<string> RuleFolders,
	IReadOnlyList<ITocEntryDto> Children,
	string? DeprecatedFile
) : FileEntryDto(Path, false, Children);

/// <summary><c>folder:</c>, with or without <c>file:</c>.</summary>
internal record FolderEntryDto(
	string Path,
	IReadOnlyList<ITocEntryDto> Children,
	string? Sort = null,
	IReadOnlyList<string>? Exclude = null,
	string? DefaultCta = null
) : ITocEntryDto;

/// <summary>The childless <c>file: subdir/index.md</c> sugar, expanded to a single-page folder.</summary>
internal sealed record DeepLinkedFolderEntryDto(string Path, IReadOnlyList<ITocEntryDto> Children, string? DefaultCta) : FolderEntryDto(
	Path,
	Children,
	DefaultCta: DefaultCta
);

/// <summary><c>toc:</c>, pointing at a folder holding a nested <c>toc.yml</c>.</summary>
internal sealed record NestedTocEntryDto(string Path, IReadOnlyList<ITocEntryDto> Children, bool Island) : ITocEntryDto;

/// <summary><c>crosslink:</c>.</summary>
internal sealed record CrossLinkEntryDto(Uri CrossLinkUri, string? Title, bool Hidden, IReadOnlyList<ITocEntryDto> Children) : ITocEntryDto;

/// <summary><c>listing:</c>.</summary>
internal sealed record ListingEntryDto(string Path, IReadOnlyList<ITocEntryDto> Children, ListingOptions Options) : ITocEntryDto;

/// <summary><c>cli:</c>.</summary>
internal sealed record CliReferenceEntryDto(
	string SchemaPath,
	string? SupplementalFolder,
	string? Title,
	string? NavigationTitle,
	IReadOnlyList<ITocEntryDto> Children,
	ApplicableTo? AppliesTo
) : ITocEntryDto;
