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
	DocsetPath PathRelativeToDocumentationSet { get; }

	/// <summary>
	/// The full path of this item relative to the container docset.yml or toc.yml file.
	/// For files: includes .md extension (e.g., "guides/getting-started.md")
	/// For folders: the folder path (e.g., "guides/advanced")
	/// For TOCs: the path to the toc.yml directory (e.g., "development" or "guides/advanced")
	/// </summary>
	ContainerPath PathRelativeToContainer { get; }

	/// <summary>
	/// The path to the YAML file (docset.yml or toc.yml) that defined this item.
	/// This provides context for where the item was declared in the configuration.
	/// </summary>
	DeclaringFilePath Context { get; }

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

	/// <summary>Every item in the tree, depth-first in declaration order, the items themselves first.</summary>
	public static IEnumerable<TocItem> Descendants(this IEnumerable<TocItem> items)
	{
		foreach (var item in items)
		{
			yield return item;
			foreach (var descendant in item.Item().Children.Descendants())
				yield return descendant;
		}
	}

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

/// <summary>A table-of-contents <c>file</c> entry.</summary>
/// <remarks>
/// <para><b>Built by:</b> the table-of-contents YAML converters (<c>TocItemYamlConverter</c>) and <c>DocumentationSetDto</c> in <c>Elastic.Documentation.Configuration</c>, reached through <c>DocumentationSetLoader.Load</c>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new FileRef { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
/// <param name="DefaultCta">
/// Optional <c>default_cta</c> declared on this entry. Applies to the file itself and every page beneath it
/// unless a nearer entry, nested <c>toc.yml</c>, or page frontmatter selects another template.
/// </param>
[Equatable]
public partial record FileRef : ITableOfContentsItem
{
	internal FileRef(
		DocsetPath pathRelativeToDocumentationSet,
		ContainerPath pathRelativeToContainer,
		bool hidden,
		IReadOnlyList<TocItem> children,
		DeclaringFilePath context,
		string? defaultCta = null
	)
	{
		PathRelativeToDocumentationSet = pathRelativeToDocumentationSet;
		PathRelativeToContainer = pathRelativeToContainer;
		Hidden = hidden;
		Children = children;
		Context = context;
		DefaultCta = defaultCta;
	}

	public DocsetPath PathRelativeToDocumentationSet { get; internal init; }

	public ContainerPath PathRelativeToContainer { get; internal init; }

	public bool Hidden { get; internal init; }

	[OrderedEquality]
	public IReadOnlyList<TocItem> Children { get; internal init; }

	public DeclaringFilePath Context { get; internal init; }

	public string? DefaultCta { get; internal init; }

	public void Deconstruct(
		out DocsetPath pathRelativeToDocumentationSet,
		out ContainerPath pathRelativeToContainer,
		out bool hidden,
		out IReadOnlyList<TocItem> children,
		out DeclaringFilePath context,
		out string? defaultCta
	)
	{
		pathRelativeToDocumentationSet = PathRelativeToDocumentationSet;
		pathRelativeToContainer = PathRelativeToContainer;
		hidden = Hidden;
		children = Children;
		context = Context;
		defaultCta = DefaultCta;
	}
}

/// <summary>A table-of-contents <c>file</c> entry that is a folder's index page.</summary>
/// <remarks>
/// <para><b>Built by:</b> the table-of-contents YAML converters (<c>TocItemYamlConverter</c>) and <c>DocumentationSetDto</c> in <c>Elastic.Documentation.Configuration</c>, reached through <c>DocumentationSetLoader.Load</c>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new IndexFileRef { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
public record IndexFileRef : FileRef
{
	internal IndexFileRef(
		DocsetPath pathRelativeToDocumentationSet,
		ContainerPath pathRelativeToContainer,
		bool hidden,
		IReadOnlyList<TocItem> children,
		DeclaringFilePath context,
		string? defaultCta = null
	) : base(pathRelativeToDocumentationSet, pathRelativeToContainer, hidden, children, context, defaultCta) { }
}

/// <summary>
/// Represents a file reference created from a folder+file combination in YAML (e.g., "folder: path/to/dir, file: index.md").
/// Children of this file should resolve relative to the folder path, not the parent TOC path.
/// </summary>
/// <remarks>
/// <para><b>Built by:</b> the table-of-contents YAML converters (<c>TocItemYamlConverter</c>) and <c>DocumentationSetDto</c> in <c>Elastic.Documentation.Configuration</c>, reached through <c>DocumentationSetLoader.Load</c>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new FolderIndexFileRef { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
public record FolderIndexFileRef : IndexFileRef
{
	internal FolderIndexFileRef(
		DocsetPath pathRelativeToDocumentationSet,
		ContainerPath pathRelativeToContainer,
		bool hidden,
		IReadOnlyList<TocItem> children,
		DeclaringFilePath context,
		string? defaultCta = null
	) : base(pathRelativeToDocumentationSet, pathRelativeToContainer, hidden, children, context, defaultCta) { }
}

/// <summary>
/// A detection rule page, generated from a <c>.toml</c> rule file under a <c>detection_rules</c> folder. Rules are not
/// declared in a toc.yml, so their container path is their docset path.
/// </summary>
/// <remarks>
/// <para><b>Built by:</b> <c>DetectionRuleDiscovery</c> in <c>Elastic.Documentation.Configuration</c>, which makes one per <c>.toml</c> rule file it finds under a <c>detection_rules</c> folder.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new DetectionRuleRef { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
[Equatable]
public partial record DetectionRuleRef : FileRef
{
	internal DetectionRuleRef(IFileInfo fileInfo, DocsetPath pathRelativeToDocumentationSet, DeclaringFilePath context) : base(
			pathRelativeToDocumentationSet,
			ContainerPath.From(pathRelativeToDocumentationSet),
			true,
			[],
			context
		) => FileInfo = fileInfo;

	[CustomEquality(typeof(FileSystemInfoLocationComparer))]
	public IFileInfo FileInfo { get; internal init; }

	public void Deconstruct(out IFileInfo fileInfo, out DocsetPath pathRelativeToDocumentationSet, out DeclaringFilePath context)
	{
		fileInfo = FileInfo;
		pathRelativeToDocumentationSet = PathRelativeToDocumentationSet;
		context = Context;
	}
}

/// <summary>
/// The overview page of a <c>detection_rules</c> entry. Its children are the explicit children followed by one
/// page per discovered rule.
/// </summary>
/// <remarks>
/// <para><b>Built by:</b> <c>DetectionRuleDiscovery</c> in <c>Elastic.Documentation.Configuration</c> (the overview page of a <c>detection_rules</c> entry) and the table-of-contents converters.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new DetectionRuleOverviewRef { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
[Equatable]
public partial record DetectionRuleOverviewRef : FileRef
{
	internal DetectionRuleOverviewRef(
		DocsetPath pathRelativeToDocumentationSet,
		ContainerPath pathRelativeToContainer,
		IReadOnlyList<string> detectionRuleFolders,
		IReadOnlyList<TocItem> children,
		DeclaringFilePath context,
		string? deprecatedFile = null
	) : base(pathRelativeToDocumentationSet, pathRelativeToContainer, false, children, context)
	{
		DetectionRuleFolders = detectionRuleFolders;
		DeprecatedFile = deprecatedFile;
	}

	[OrderedEquality]
	public IReadOnlyList<string> DetectionRuleFolders { get; internal init; }

	public string? DeprecatedFile { get; internal init; }

	public void Deconstruct(
		out DocsetPath pathRelativeToDocumentationSet,
		out ContainerPath pathRelativeToContainer,
		out IReadOnlyList<string> detectionRuleFolders,
		out IReadOnlyList<TocItem> children,
		out DeclaringFilePath context,
		out string? deprecatedFile
	)
	{
		pathRelativeToDocumentationSet = PathRelativeToDocumentationSet;
		pathRelativeToContainer = PathRelativeToContainer;
		detectionRuleFolders = DetectionRuleFolders;
		children = Children;
		context = Context;
		deprecatedFile = DeprecatedFile;
	}

	/// <summary>
	/// The deprecated-rules overview emitted as this entry's next sibling, when a <c>_deprecated</c> subfolder exists.
	/// </summary>
	[DefaultEquality]
	public FileRef? DeprecatedSiblingRef { get; internal init; }
}

/// <summary>A table-of-contents entry that points at a page of another repository by cross-link.</summary>
/// <remarks>
/// <para><b>Built by:</b> the table-of-contents YAML converters (<c>TocItemYamlConverter</c>) and <c>DocumentationSetDto</c> in <c>Elastic.Documentation.Configuration</c>, reached through <c>DocumentationSetLoader.Load</c>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new CrossLinkRef { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
[Equatable]
public partial record CrossLinkRef : ITableOfContentsItem
{
	internal CrossLinkRef(Uri crossLinkUri, string? title, bool hidden, IReadOnlyList<TocItem> children, DeclaringFilePath context)
	{
		CrossLinkUri = crossLinkUri;
		Title = title;
		Hidden = hidden;
		Children = children;
		Context = context;
	}

	public Uri CrossLinkUri { get; internal init; }

	public string? Title { get; internal init; }

	public bool Hidden { get; internal init; }

	[OrderedEquality]
	public IReadOnlyList<TocItem> Children { get; internal init; }

	public DeclaringFilePath Context { get; internal init; }

	public void Deconstruct(
		out Uri crossLinkUri,
		out string? title,
		out bool hidden,
		out IReadOnlyList<TocItem> children,
		out DeclaringFilePath context
	)
	{
		crossLinkUri = CrossLinkUri;
		title = Title;
		hidden = Hidden;
		children = Children;
		context = Context;
	}

	// CrossLinks don't have a file system path, so we use the CrossLinkUri as the Path
	public DocsetPath PathRelativeToDocumentationSet => DocsetPath.From(CrossLinkUri.ToString());

	// CrossLinks don't have a file system path, so we use the CrossLinkUri as the Path
	public ContainerPath PathRelativeToContainer => ContainerPath.From(CrossLinkUri.ToString());
}

/// <summary>A table-of-contents <c>folder</c> entry.</summary>
/// <remarks>
/// <para><b>Built by:</b> the table-of-contents YAML converters (<c>TocItemYamlConverter</c>) and <c>DocumentationSetDto</c> in <c>Elastic.Documentation.Configuration</c>, reached through <c>DocumentationSetLoader.Load</c>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new FolderRef { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
/// <param name="Sort">The folder's <c>sort</c>; null keeps plain alphabetical order. An unrecognized value is rejected by the loader.</param>
/// <param name="Exclude">File names to exclude from auto-discovery (like "draft.md", "internal.md").</param>
/// <param name="DefaultCta">Optional <c>default_cta</c> declared on this entry; applies to every page inside the folder.</param>
[Equatable]
public partial record FolderRef : ITableOfContentsItem
{
	internal FolderRef(
		DocsetPath pathRelativeToDocumentationSet,
		ContainerPath pathRelativeToContainer,
		IReadOnlyList<TocItem> children,
		DeclaringFilePath context,
		SortOrder? sort = null,
		IReadOnlyList<string>? exclude = null,
		string? defaultCta = null
	)
	{
		PathRelativeToDocumentationSet = pathRelativeToDocumentationSet;
		PathRelativeToContainer = pathRelativeToContainer;
		Children = children;
		Context = context;
		Sort = sort;
		Exclude = exclude;
		DefaultCta = defaultCta;
	}

	public DocsetPath PathRelativeToDocumentationSet { get; internal init; }

	public ContainerPath PathRelativeToContainer { get; internal init; }

	[OrderedEquality]
	public IReadOnlyList<TocItem> Children { get; internal init; }

	public DeclaringFilePath Context { get; internal init; }

	public SortOrder? Sort { get; internal init; }

	[OrderedEquality]
	public IReadOnlyList<string>? Exclude { get; internal init; }

	public string? DefaultCta { get; internal init; }

	public void Deconstruct(
		out DocsetPath pathRelativeToDocumentationSet,
		out ContainerPath pathRelativeToContainer,
		out IReadOnlyList<TocItem> children,
		out DeclaringFilePath context,
		out SortOrder? sort,
		out IReadOnlyList<string>? exclude,
		out string? defaultCta
	)
	{
		pathRelativeToDocumentationSet = PathRelativeToDocumentationSet;
		pathRelativeToContainer = PathRelativeToContainer;
		children = Children;
		context = Context;
		sort = Sort;
		exclude = Exclude;
		defaultCta = DefaultCta;
	}
}

/// <summary>
/// A synthetic single-page folder created from the childless "file: subdir/index.md" sugar. Its path
/// always resolves relative to the parent, exactly like the <see cref="FileRef"/> it was expanded from
/// -- unlike an explicit "folder: a/b" entry, it never switches to context-relative resolution just
/// because its path contains '/'.
/// </summary>
/// <remarks>
/// <para><b>Built by:</b> the table-of-contents YAML converters (<c>TocItemYamlConverter</c>) and <c>DocumentationSetDto</c> in <c>Elastic.Documentation.Configuration</c>, reached through <c>DocumentationSetLoader.Load</c>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new DeepLinkedFolderRef { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
public record DeepLinkedFolderRef : FolderRef
{
	internal DeepLinkedFolderRef(
		DocsetPath pathRelativeToDocumentationSet,
		ContainerPath pathRelativeToContainer,
		IReadOnlyList<TocItem> children,
		DeclaringFilePath context,
		string? defaultCta = null
	) : base(pathRelativeToDocumentationSet, pathRelativeToContainer, children, context, defaultCta: defaultCta) { }

	public void Deconstruct(
		out DocsetPath pathRelativeToDocumentationSet,
		out ContainerPath pathRelativeToContainer,
		out IReadOnlyList<TocItem> children,
		out DeclaringFilePath context,
		out string? defaultCta
	)
	{
		pathRelativeToDocumentationSet = PathRelativeToDocumentationSet;
		pathRelativeToContainer = PathRelativeToContainer;
		children = Children;
		context = Context;
		defaultCta = DefaultCta;
	}
}

/// <summary>A nested <c>toc.yml</c> referenced from a table of contents.</summary>
/// <remarks>
/// <para><b>Built by:</b> the table-of-contents YAML converters (<c>TocItemYamlConverter</c>) and <c>DocumentationSetDto</c> in <c>Elastic.Documentation.Configuration</c>, reached through <c>DocumentationSetLoader.Load</c>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new IsolatedTableOfContentsRef { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
/// <param name="Island">
/// When <c>true</c>, this TOC renders as an island. Combines flags from both the inline
/// <c>- toc: x</c> entry and the child <c>toc.yml</c> root (OR semantics).
/// </param>
[Equatable]
public partial record IsolatedTableOfContentsRef : ITableOfContentsItem
{
	internal IsolatedTableOfContentsRef(
		DocsetPath pathRelativeToDocumentationSet,
		ContainerPath pathRelativeToContainer,
		IReadOnlyList<TocItem> children,
		DeclaringFilePath context,
		bool island = false,
		string? defaultCta = null
	)
	{
		PathRelativeToDocumentationSet = pathRelativeToDocumentationSet;
		PathRelativeToContainer = pathRelativeToContainer;
		Children = children;
		Context = context;
		Island = island;
		DefaultCta = defaultCta;
	}

	public DocsetPath PathRelativeToDocumentationSet { get; internal init; }

	public ContainerPath PathRelativeToContainer { get; internal init; }

	[OrderedEquality]
	public IReadOnlyList<TocItem> Children { get; internal init; }

	public DeclaringFilePath Context { get; internal init; }

	public bool Island { get; internal init; }

	public string? DefaultCta { get; internal init; }

	public void Deconstruct(
		out DocsetPath pathRelativeToDocumentationSet,
		out ContainerPath pathRelativeToContainer,
		out IReadOnlyList<TocItem> children,
		out DeclaringFilePath context,
		out bool island,
		out string? defaultCta
	)
	{
		pathRelativeToDocumentationSet = PathRelativeToDocumentationSet;
		pathRelativeToContainer = PathRelativeToContainer;
		children = Children;
		context = Context;
		island = Island;
		defaultCta = DefaultCta;
	}
}

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
/// <remarks>
/// <para><b>Built by:</b> the table-of-contents YAML converters (<c>TocItemYamlConverter</c>) and <c>DocumentationSetDto</c> in <c>Elastic.Documentation.Configuration</c>, reached through <c>DocumentationSetLoader.Load</c>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new ListingOptions { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
[Equatable]
public partial record ListingOptions
{
	internal ListingOptions(
		string? glob = null,
		SortOrder? sort = null,
		IReadOnlyList<string>? exclude = null,
		ListingVisual visual = ListingVisual.None,
		IReadOnlyList<string>? groups = null,
		string? extension = null,
		bool island = false
	)
	{
		Glob = glob;
		Sort = sort;
		Exclude = exclude;
		Visual = visual;
		Groups = groups;
		Extension = extension;
		Island = island;
	}

	public string? Glob { get; internal init; }

	public SortOrder? Sort { get; internal init; }

	[OrderedEquality]
	public IReadOnlyList<string>? Exclude { get; internal init; }

	public ListingVisual Visual { get; internal init; }

	[OrderedEquality]
	public IReadOnlyList<string>? Groups { get; internal init; }

	public string? Extension { get; internal init; }

	public bool Island { get; internal init; }

	public void Deconstruct(
		out string? glob,
		out SortOrder? sort,
		out IReadOnlyList<string>? exclude,
		out ListingVisual visual,
		out IReadOnlyList<string>? groups,
		out string? extension,
		out bool island
	)
	{
		glob = Glob;
		sort = Sort;
		exclude = Exclude;
		visual = Visual;
		groups = Groups;
		extension = Extension;
		island = Island;
	}
}

/// <summary>
/// A listing entry in the TOC: glob-discovers files, hides them from the nav tree
/// (subject to <see cref="ListingOptions.Visual"/>), and generates a grouped index page.
/// </summary>
/// <remarks>
/// <para><b>Built by:</b> the table-of-contents YAML converters (<c>TocItemYamlConverter</c>) and <c>DocumentationSetDto</c> in <c>Elastic.Documentation.Configuration</c>, reached through <c>DocumentationSetLoader.Load</c>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new ListingRef { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
[Equatable]
public partial record ListingRef : ITableOfContentsItem
{
	internal ListingRef(
		DocsetPath pathRelativeToDocumentationSet,
		ContainerPath pathRelativeToContainer,
		IReadOnlyList<TocItem> children,
		DeclaringFilePath context,
		ListingOptions options
	)
	{
		PathRelativeToDocumentationSet = pathRelativeToDocumentationSet;
		PathRelativeToContainer = pathRelativeToContainer;
		Children = children;
		Context = context;
		Options = options;
	}

	public DocsetPath PathRelativeToDocumentationSet { get; internal init; }

	public ContainerPath PathRelativeToContainer { get; internal init; }

	[OrderedEquality]
	public IReadOnlyList<TocItem> Children { get; internal init; }

	public DeclaringFilePath Context { get; internal init; }

	public ListingOptions Options { get; internal init; }

	public void Deconstruct(
		out DocsetPath pathRelativeToDocumentationSet,
		out ContainerPath pathRelativeToContainer,
		out IReadOnlyList<TocItem> children,
		out DeclaringFilePath context,
		out ListingOptions options
	)
	{
		pathRelativeToDocumentationSet = PathRelativeToDocumentationSet;
		pathRelativeToContainer = PathRelativeToContainer;
		children = Children;
		context = Context;
		options = Options;
	}
}

/// <summary>A resolved group bucket inside a <see cref="ListingRef"/>. Children are the pages belonging to this group.</summary>
/// <remarks>
/// <para><b>Built by:</b> <c>DocumentationSetDto</c> in <c>Elastic.Documentation.Configuration</c>, which resolves the groups of a <c>ListingRef</c> while it maps the file to the definition.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new ListingGroupRef { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
[Equatable]
public partial record ListingGroupRef : ITableOfContentsItem
{
	internal ListingGroupRef(
		string groupKey,
		DocsetPath pathRelativeToDocumentationSet,
		ContainerPath pathRelativeToContainer,
		IReadOnlyList<TocItem> children,
		DeclaringFilePath context
	)
	{
		GroupKey = groupKey;
		PathRelativeToDocumentationSet = pathRelativeToDocumentationSet;
		PathRelativeToContainer = pathRelativeToContainer;
		Children = children;
		Context = context;
	}

	public string GroupKey { get; internal init; }

	public DocsetPath PathRelativeToDocumentationSet { get; internal init; }

	public ContainerPath PathRelativeToContainer { get; internal init; }

	[OrderedEquality]
	public IReadOnlyList<TocItem> Children { get; internal init; }

	public DeclaringFilePath Context { get; internal init; }

	public void Deconstruct(
		out string groupKey,
		out DocsetPath pathRelativeToDocumentationSet,
		out ContainerPath pathRelativeToContainer,
		out IReadOnlyList<TocItem> children,
		out DeclaringFilePath context
	)
	{
		groupKey = GroupKey;
		pathRelativeToDocumentationSet = PathRelativeToDocumentationSet;
		pathRelativeToContainer = PathRelativeToContainer;
		children = Children;
		context = Context;
	}
}

/// <summary>A CLI reference generated from a <c>cli-schema.json</c>, optionally merged with supplemental docs.</summary>
/// <remarks>
/// <para><b>Built by:</b> the table-of-contents YAML converters (<c>TocItemYamlConverter</c>) and <c>DocumentationSetDto</c> in <c>Elastic.Documentation.Configuration</c>, reached through <c>DocumentationSetLoader.Load</c>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new CliReferenceRef { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
[Equatable]
public partial record CliReferenceRef : ITableOfContentsItem
{
	internal CliReferenceRef(
		string schemaPath,
		string? supplementalFolder,
		string? title,
		string? navigationTitle,
		DocsetPath pathRelativeToDocumentationSet,
		ContainerPath pathRelativeToContainer,
		DeclaringFilePath context,
		IReadOnlyList<TocItem> children,
		ApplicableTo? appliesTo = null
	)
	{
		SchemaPath = schemaPath;
		SupplementalFolder = supplementalFolder;
		Title = title;
		NavigationTitle = navigationTitle;
		PathRelativeToDocumentationSet = pathRelativeToDocumentationSet;
		PathRelativeToContainer = pathRelativeToContainer;
		Context = context;
		Children = children;
		AppliesTo = appliesTo;
	}

	public string SchemaPath { get; internal init; }

	public string? SupplementalFolder { get; internal init; }

	public string? Title { get; internal init; }

	public string? NavigationTitle { get; internal init; }

	public DocsetPath PathRelativeToDocumentationSet { get; internal init; }

	public ContainerPath PathRelativeToContainer { get; internal init; }

	public DeclaringFilePath Context { get; internal init; }

	[OrderedEquality]
	public IReadOnlyList<TocItem> Children { get; internal init; }

	[DefaultEquality]
	public ApplicableTo? AppliesTo { get; internal init; }

	public void Deconstruct(
		out string schemaPath,
		out string? supplementalFolder,
		out string? title,
		out string? navigationTitle,
		out DocsetPath pathRelativeToDocumentationSet,
		out ContainerPath pathRelativeToContainer,
		out DeclaringFilePath context,
		out IReadOnlyList<TocItem> children,
		out ApplicableTo? appliesTo
	)
	{
		schemaPath = SchemaPath;
		supplementalFolder = SupplementalFolder;
		title = Title;
		navigationTitle = NavigationTitle;
		pathRelativeToDocumentationSet = PathRelativeToDocumentationSet;
		pathRelativeToContainer = PathRelativeToContainer;
		context = Context;
		children = Children;
		appliesTo = AppliesTo;
	}
}
