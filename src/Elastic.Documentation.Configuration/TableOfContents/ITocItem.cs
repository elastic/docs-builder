// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Navigation;

namespace Elastic.Documentation.Configuration.TableOfContents;

public interface ITocItem
{
	ITableOfContentsScope TableOfContentsScope { get; }
}

public record FileReference(ITableOfContentsScope TableOfContentsScope, string RelativePath, bool Hidden, IReadOnlyCollection<ITocItem> Children)
	: ITocItem;

public record FolderReference(ITableOfContentsScope TableOfContentsScope, string RelativePath, IReadOnlyCollection<ITocItem> Children)
	: ITocItem;

public record TocReference(Uri Source, ITableOfContentsScope TableOfContentsScope, string RelativePath, IReadOnlyCollection<ITocItem> Children)
	: FolderReference(TableOfContentsScope, RelativePath, Children)
{
	public IReadOnlyDictionary<Uri, TocReference> TocReferences { get; } =
		Children.OfType<TocReference>().ToDictionary(kv => kv.Source, kv => kv);

	/// <summary>
	/// A phantom table of contents is a table of contents that is not rendered in the UI but is used to generate the TOC.
	/// This should be used sparingly and needs explicit configuration in navigation.yml.
	/// It's typically used for container TOC that holds various other TOC's where its children are rehomed throughout the navigation.
	/// </summary>
	public bool IsPhantom { get; init; }
}

/// <summary>
/// Represents an external link in the table of contents.
/// </summary>
/// <param name="TableOfContentsScope">Scope this link belongs to.</param>
/// <param name="Url">Absolute URL of the external resource.</param>
/// <param name="Title">Display title for the link.</param>
public record LinkReference(ITableOfContentsScope TableOfContentsScope, string Url, string Title) : ITocItem;

