// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Equality;
using Generator.Equals;

namespace Elastic.Documentation.DocumentationSets;

/// <summary>
/// One entry of a resolved table of contents: the <c>docset.yml</c> <c>toc:</c> tree with every nested
/// <c>toc.yml</c>, folder auto-discovery, listing and detection-rule expansion applied. Paths are final.
/// Subtypes of a case (<see cref="IndexFileRef"/> under <see cref="FileRef"/>, <see cref="DeepLinkedFolderRef"/>
/// under <see cref="FolderRef"/>) match their case's pattern too, so switch on the most specific type first.
/// </summary>
public readonly union TocItem(
	FileRef,
	FolderRef,
	IsolatedTableOfContentsRef,
	CrossLinkRef,
	ListingRef,
	ListingGroupRef,
	CliReferenceRef
);

/// <summary>The members every <see cref="TocItem"/> case shares.</summary>
public interface ITableOfContentsItem
{
	/// <summary>
	/// The full path of this item relative to the documentation source directory.
	/// For files: includes .md extension (e.g., "guides/getting-started.md")
	/// For folders: the folder path (e.g., "guides/advanced")
	/// For TOCs: the path to the toc.yml directory (e.g., "development" or "guides/advanced")
	/// </summary>
	string PathRelativeToDocumentationSet { get; }

	/// <summary>
	/// The full path of this item relative to the container docset.yml or toc.yml file.
	/// For files: includes .md extension (e.g., "guides/getting-started.md")
	/// For folders: the folder path (e.g., "guides/advanced")
	/// For TOCs: the path to the toc.yml directory (e.g., "development" or "guides/advanced")
	/// </summary>
	string PathRelativeToContainer { get; }

	/// <summary>
	/// The path to the YAML file (docset.yml or toc.yml) that defined this item.
	/// This provides context for where the item was declared in the configuration.
	/// </summary>
	string Context { get; }

	IReadOnlyList<TocItem> Children { get; }
}

public static class TocItemExtensions
{
	/// <summary>The case value as its shared shape. A <c>default</c> union has none and throws.</summary>
	public static ITableOfContentsItem Item(this TocItem item) =>
		item.Value as ITableOfContentsItem ?? throw new InvalidOperationException("TocItem has no value");

	/// <summary>
	/// The items whose case value is a <typeparamref name="T"/>. Use this, not <c>OfType</c>: a union is a struct
	/// wrapping its case, so <c>OfType</c> over unions is always empty.
	/// </summary>
	public static IEnumerable<T> OfCase<T>(this IEnumerable<TocItem> items) => items.Select(i => i.Value).OfType<T>();

	/// <summary>Every file in the subtree, in declaration order. Cross-links and listing groups contribute none of their own.</summary>
	public static IEnumerable<FileRef> FileRefs(this TocItem item) => item switch
	{
		FileRef file => [file],
		FolderRef folder => folder.Children.SelectMany(FileRefs),
		IsolatedTableOfContentsRef toc => toc.Children.SelectMany(FileRefs),
		CliReferenceRef cli => cli.Children.SelectMany(FileRefs),
		CrossLinkRef => [],
		ListingRef => throw new InvalidOperationException($"Unexpected item type {nameof(ListingRef)}"),
		ListingGroupRef => throw new InvalidOperationException($"Unexpected item type {nameof(ListingGroupRef)}"),
		null => throw new InvalidOperationException("TocItem has no value")
	};
}

/// <summary>Specifies the sort order for auto-discovered files in a folder.</summary>
public enum SortOrder
{
	/// <summary>Sort files in ascending alphabetical order (A-Z). This is the default.</summary>
	Ascending,

	/// <summary>
	/// Sort files in descending alphabetical order (Z-A).
	/// Useful for version-numbered folders where newest should appear first.
	/// </summary>
	Descending
}

/// <summary>Parsing helpers for <see cref="SortOrder"/>.</summary>
public static class SortOrderExtensions
{
	/// <summary>Tries to parse a YAML sort value (asc, ascending, desc, descending) into a <see cref="SortOrder"/>.</summary>
	public static bool TryParse(string? value, out SortOrder result)
	{
		var normalized = value?.ToLowerInvariant();
		(result, var valid) = normalized switch
		{
			"desc" or "descending" => (SortOrder.Descending, true),
			"asc" or "ascending" => (SortOrder.Ascending, true),
			_ => (SortOrder.Ascending, false)
		};
		return valid;
	}
}

/// <param name="DefaultCta">
/// Optional <c>default_cta</c> declared on this entry. Applies to the file itself and every page beneath it
/// unless a nearer entry, nested <c>toc.yml</c>, or page frontmatter selects another template.
/// </param>
[Equatable]
public partial record FileRef(
	string PathRelativeToDocumentationSet,
	string PathRelativeToContainer,
	bool Hidden,
	[property: OrderedEquality] IReadOnlyList<TocItem> Children,
	string Context,
	string? DefaultCta = null
) : ITableOfContentsItem;

public record IndexFileRef(
	string PathRelativeToDocumentationSet,
	string PathRelativeToContainer,
	bool Hidden,
	IReadOnlyList<TocItem> Children,
	string Context,
	string? DefaultCta = null
) : FileRef(PathRelativeToDocumentationSet, PathRelativeToContainer, Hidden, Children, Context, DefaultCta);

/// <summary>
/// Represents a file reference created from a folder+file combination in YAML (e.g., "folder: path/to/dir, file: index.md").
/// Children of this file should resolve relative to the folder path, not the parent TOC path.
/// </summary>
public record FolderIndexFileRef(
	string PathRelativeToDocumentationSet,
	string PathRelativeToContainer,
	bool Hidden,
	IReadOnlyList<TocItem> Children,
	string Context,
	string? DefaultCta = null
) : IndexFileRef(PathRelativeToDocumentationSet, PathRelativeToContainer, Hidden, Children, Context, DefaultCta);

/// <summary>A detection rule page, generated from a <c>.toml</c> rule file under a <c>detection_rules</c> folder.</summary>
[Equatable]
public partial record DetectionRuleRef(
	[property: CustomEquality(typeof(FileSystemInfoLocationComparer))] IFileInfo FileInfo,
	string PathRelativeToDocumentationSet,
	string Context
) : FileRef(PathRelativeToDocumentationSet, PathRelativeToDocumentationSet, true, [], Context);

/// <summary>
/// The overview page of a <c>detection_rules</c> entry. Its children are the explicit children followed by one
/// page per discovered rule.
/// </summary>
[Equatable]
public partial record DetectionRuleOverviewRef(
	string PathRelativeToDocumentationSet,
	string PathRelativeToContainer,
	[property: OrderedEquality] IReadOnlyList<string> DetectionRuleFolders,
	IReadOnlyList<TocItem> Children,
	string Context,
	string? DeprecatedFile = null
) : FileRef(PathRelativeToDocumentationSet, PathRelativeToContainer, false, Children, Context)
{
	/// <summary>
	/// The deprecated-rules overview emitted as this entry's next sibling, when a <c>_deprecated</c> subfolder exists.
	/// </summary>
	[DefaultEquality]
	public FileRef? DeprecatedSiblingRef { get; init; }
}

[Equatable]
public partial record CrossLinkRef(
	Uri CrossLinkUri,
	string? Title,
	bool Hidden,
	[property: OrderedEquality] IReadOnlyList<TocItem> Children,
	string Context
) : ITableOfContentsItem
{
	// CrossLinks don't have a file system path, so we use the CrossLinkUri as the Path
	public string PathRelativeToDocumentationSet => CrossLinkUri.ToString();

	// CrossLinks don't have a file system path, so we use the CrossLinkUri as the Path
	public string PathRelativeToContainer => CrossLinkUri.ToString();
}

/// <param name="Sort">Raw YAML sort value, parsed and validated during resolution via <see cref="SortOrderExtensions.TryParse"/>.</param>
/// <param name="Exclude">File names to exclude from auto-discovery (like "draft.md", "internal.md").</param>
/// <param name="DefaultCta">Optional <c>default_cta</c> declared on this entry; applies to every page inside the folder.</param>
[Equatable]
public partial record FolderRef(
	string PathRelativeToDocumentationSet,
	string PathRelativeToContainer,
	[property: OrderedEquality] IReadOnlyList<TocItem> Children,
	string Context,
	string? Sort = null,
	[property: OrderedEquality] IReadOnlyList<string>? Exclude = null,
	string? DefaultCta = null
) : ITableOfContentsItem;

/// <summary>
/// A synthetic single-page folder created from the childless "file: subdir/index.md" sugar. Its path
/// always resolves relative to the parent, exactly like the <see cref="FileRef"/> it was expanded from
/// -- unlike an explicit "folder: a/b" entry, it never switches to context-relative resolution just
/// because its path contains '/'.
/// </summary>
public record DeepLinkedFolderRef(
	string PathRelativeToDocumentationSet,
	string PathRelativeToContainer,
	IReadOnlyList<TocItem> Children,
	string Context,
	string? DefaultCta = null
) : FolderRef(PathRelativeToDocumentationSet, PathRelativeToContainer, Children, Context, DefaultCta: DefaultCta);

/// <param name="Island">
/// When <c>true</c>, this TOC renders as an island. Combines flags from both the inline
/// <c>- toc: x</c> entry and the child <c>toc.yml</c> root (OR semantics).
/// </param>
[Equatable]
public partial record IsolatedTableOfContentsRef(
	string PathRelativeToDocumentationSet,
	string PathRelativeToContainer,
	[property: OrderedEquality] IReadOnlyList<TocItem> Children,
	string Context,
	bool Island = false,
	string? DefaultCta = null
) : ITableOfContentsItem;

/// <summary>Controls how much of the listing appears in the rendered navigation tree.</summary>
public enum ListingVisual
{
	/// <summary>Only the listing root index page is visible in the nav tree (default).</summary>
	None,

	/// <summary>The listing root and one node per group (with their group index) are visible.</summary>
	Groups,

	/// <summary>Full depth — group nodes and all their pages are visible.</summary>
	All,
}

/// <summary>Parsing helpers for <see cref="ListingVisual"/>.</summary>
public static class ListingVisualExtensions
{
	/// <summary>Tries to parse a YAML visual value (none, groups, all) into a <see cref="ListingVisual"/>.</summary>
	public static bool TryParse(string? value, out ListingVisual result)
	{
		var normalized = value?.ToLowerInvariant();
		(result, var valid) = normalized switch
		{
			"none" => (ListingVisual.None, true),
			"groups" => (ListingVisual.Groups, true),
			"all" => (ListingVisual.All, true),
			_ => (ListingVisual.None, false)
		};
		return valid;
	}
}

/// <summary>Optional knobs on a <see cref="ListingRef"/> entry beyond the mandatory path.</summary>
[Equatable]
public partial record ListingOptions(
	string? Glob = null,
	string? Sort = null,
	[property: OrderedEquality] IReadOnlyList<string>? Exclude = null,
	ListingVisual Visual = ListingVisual.None,
	[property: OrderedEquality] IReadOnlyList<string>? Groups = null,
	string? Extension = null,
	bool Island = false
);

/// <summary>
/// A listing entry in the TOC: glob-discovers files, hides them from the nav tree
/// (subject to <see cref="ListingOptions.Visual"/>), and generates a grouped index page.
/// </summary>
[Equatable]
public partial record ListingRef(
	string PathRelativeToDocumentationSet,
	string PathRelativeToContainer,
	[property: OrderedEquality] IReadOnlyList<TocItem> Children,
	string Context,
	ListingOptions Options
) : ITableOfContentsItem;

/// <summary>A resolved group bucket inside a <see cref="ListingRef"/>. Children are the pages belonging to this group.</summary>
[Equatable]
public partial record ListingGroupRef(
	string GroupKey,
	string PathRelativeToDocumentationSet,
	string PathRelativeToContainer,
	[property: OrderedEquality] IReadOnlyList<TocItem> Children,
	string Context
) : ITableOfContentsItem;

/// <summary>A CLI reference generated from a <c>cli-schema.json</c>, optionally merged with supplemental docs.</summary>
[Equatable]
public partial record CliReferenceRef(
	string SchemaPath,
	string? SupplementalFolder,
	string? Title,
	string? NavigationTitle,
	string PathRelativeToDocumentationSet,
	string PathRelativeToContainer,
	string Context,
	[property: OrderedEquality] IReadOnlyList<TocItem> Children,
	[property: DefaultEquality] ApplicableTo? AppliesTo = null
) : ITableOfContentsItem;
