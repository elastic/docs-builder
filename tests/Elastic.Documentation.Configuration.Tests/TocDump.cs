// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.Documentation.Configuration.Toc;

namespace Elastic.Documentation.Configuration.Tests;

/// <summary>
/// A table of contents as indented text, one entry per line: <c>Type  path</c> followed by the flags that are set
/// (<c>[hidden]</c>, <c>[island]</c>, <c>title=</c>, <c>sort=</c>, ...). Children are indented below their parent.
/// The refs are records that hold their children behind an interface, which a JSON snapshot would flatten.
/// </summary>
public static class TocDump
{
	public static string ToText(IEnumerable<ITableOfContentsItem> items)
	{
		var sb = new StringBuilder();
		foreach (var item in items)
			Write(sb, item, 0);
		return sb.ToString();
	}

	private static void Write(StringBuilder sb, ITableOfContentsItem item, int depth)
	{
		sb.Append(' ', depth * 2).Append(item.GetType().Name).Append("  ").Append(item.PathRelativeToDocumentationSet);
		if (item.PathRelativeToContainer != item.PathRelativeToDocumentationSet)
			sb.Append(" (container-relative: ").Append(item.PathRelativeToContainer).Append(')');

		switch (item)
		{
			case FileRef { Hidden: true }:
			case CrossLinkRef { Hidden: true }:
				sb.Append(" [hidden]");
				break;
		}

		switch (item)
		{
			case CrossLinkRef { Title: { } title }:
				sb.Append(" title=\"").Append(title).Append('"');
				break;
			case IsolatedTableOfContentsRef { Island: true }:
				sb.Append(" [island]");
				break;
			case FolderRef folder:
				if (folder.Sort is { } sort)
					sb.Append(" sort=").Append(sort);
				if (folder.Exclude is { Count: > 0 } exclude)
					sb.Append(" exclude=[").Append(string.Join(", ", exclude)).Append(']');
				break;
			case ListingRef listing:
				sb.Append(" listing=").Append(listing.Options);
				break;
			case ListingGroupRef group:
				sb.Append(" group=").Append(group.GroupKey);
				break;
		}

		var defaultCta = item switch
		{
			FileRef f => f.DefaultCta,
			FolderRef f => f.DefaultCta,
			IsolatedTableOfContentsRef t => t.DefaultCta,
			_ => null
		};
		if (defaultCta is not null)
			sb.Append(" cta=").Append(defaultCta);

		sb.Append('\n');

		var children = item switch
		{
			FileRef f => f.Children,
			CrossLinkRef c => c.Children,
			FolderRef f => f.Children,
			IsolatedTableOfContentsRef t => t.Children,
			ListingRef l => l.Children,
			ListingGroupRef g => g.Children,
			_ => []
		};
		foreach (var child in children)
			Write(sb, child, depth + 1);
	}
}
