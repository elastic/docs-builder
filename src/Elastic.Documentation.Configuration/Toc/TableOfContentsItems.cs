// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration.Toc;

/// <summary>
/// Represents an item in a table of contents (file, folder, or TOC reference).
/// </summary>
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
}

public record FileRef(string PathRelativeToDocumentationSet, string PathRelativeToContainer, bool Hidden, IReadOnlyCollection<ITableOfContentsItem> Children, string Context)
	: ITableOfContentsItem;

public record IndexFileRef(string PathRelativeToDocumentationSet, string PathRelativeToContainer, bool Hidden, IReadOnlyCollection<ITableOfContentsItem> Children, string Context)
	: FileRef(PathRelativeToDocumentationSet, PathRelativeToContainer, Hidden, Children, Context);

/// <summary>
/// Represents a file reference created from a folder+file combination in YAML (e.g., "folder: path/to/dir, file: index.md").
/// Children of this file should resolve relative to the folder path, not the parent TOC path.
/// </summary>
public record FolderIndexFileRef(string PathRelativeToDocumentationSet, string PathRelativeToContainer, bool Hidden, IReadOnlyCollection<ITableOfContentsItem> Children, string Context)
	: IndexFileRef(PathRelativeToDocumentationSet, PathRelativeToContainer, Hidden, Children, Context);

public record CrossLinkRef(Uri CrossLinkUri, string? Title, bool Hidden, IReadOnlyCollection<ITableOfContentsItem> Children, string Context)
	: ITableOfContentsItem
{
	//TODO ensure we pass these to cross-links to
	// CrossLinks don't have a file system path, so we use the CrossLinkUri as the Path
	public string PathRelativeToDocumentationSet => CrossLinkUri.ToString();

	// CrossLinks don't have a file system path, so we use the CrossLinkUri as the Path
	public string PathRelativeToContainer => CrossLinkUri.ToString();
}

public record FolderRef(string PathRelativeToDocumentationSet, string PathRelativeToContainer, IReadOnlyCollection<ITableOfContentsItem> Children, string Context)
	: ITableOfContentsItem;

public record IsolatedTableOfContentsRef(string PathRelativeToDocumentationSet, string PathRelativeToContainer, IReadOnlyCollection<ITableOfContentsItem> Children, string Context)
	: ITableOfContentsItem;
