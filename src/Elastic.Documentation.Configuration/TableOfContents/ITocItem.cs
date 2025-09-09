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

public record CrossLinkReference(ITableOfContentsScope TableOfContentsScope, Uri CrossLinkUri, string? Title, bool Hidden, IReadOnlyCollection<ITocItem> Children)
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
	/// <para>Examples of phantom toc's:</para>
	/// <list type="">
	///		<item> - toc: elasticsearch://reference</item>
	///		<item> - toc: docs-content://</item>
	/// </list>
	/// <para>Because navigation.yml does exhaustive checks to ensure all toc.yml files are referenced, marking these containers as phantoms
	/// ensures that these skip validation checks
	/// </para>
	/// </summary>
	public bool IsPhantom { get; init; }
}

