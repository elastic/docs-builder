// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.IO.Configuration;

public interface ITocItem
{
	ITableOfContentsScope TableOfContentsScope { get; }
}

public record FileReference(ITableOfContentsScope TableOfContentsScope, string Path, bool Found, bool Hidden, IReadOnlyCollection<ITocItem> Children)
	: ITocItem;

public record FolderReference(ITableOfContentsScope TableOfContentsScope, string Path, bool Found, IReadOnlyCollection<ITocItem> Children)
	: ITocItem;

public record TocReference(Uri Source, ITableOfContentsScope TableOfContentsScope, string Path, bool Found, IReadOnlyCollection<ITocItem> Children)
	: FolderReference(TableOfContentsScope, Path, Found, Children)

{
	public IReadOnlyDictionary<Uri, TocReference> TocReferences { get; } =
		Children.OfType<TocReference>().ToDictionary(kv => kv.Source, kv => kv);
};
