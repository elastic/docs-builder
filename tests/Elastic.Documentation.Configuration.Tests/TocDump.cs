// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.DocumentationSets;

namespace Elastic.Documentation.Configuration.Tests;

/// <summary>
/// A table of contents as indented text, one entry per line: <c>Type  path</c> followed by the flags that are set
/// (<c>[hidden]</c>, <c>[island]</c>, <c>title=</c>, <c>sort=</c>, ...). Children are indented below their parent.
/// The entries hold their children behind an interface or a union, which a JSON snapshot would flatten.
/// The authored entries (<see cref="ITocEntryDto"/>) and the resolved items (<see cref="TocItem"/>) print the same type names.
/// </summary>
public static class TocDump
{
	internal static string ToText(IEnumerable<ITocEntryDto> entries)
	{
		var sb = new StringBuilder();
		foreach (var entry in entries)
			Write(sb, entry, 0);
		return sb.ToString();
	}

	public static string ToText(IEnumerable<TocItem> items)
	{
		var sb = new StringBuilder();
		foreach (var item in items)
			Write(sb, item.Item(), 0);
		return sb.ToString();
	}

	private static void Write(StringBuilder sb, ITocEntryDto entry, int depth)
	{
		sb.Append(' ', depth * 2).Append(TypeName(entry)).Append("  ").Append(EntryPath(entry));

		switch (entry)
		{
			case FileEntryDto { Hidden: true }:
			case CrossLinkEntryDto { Hidden: true }:
				sb.Append(" [hidden]");
				break;
		}

		switch (entry)
		{
			case CrossLinkEntryDto { Title: { } title }:
				sb.Append(" title=\"").Append(title).Append('"');
				break;
			case NestedTocEntryDto { Island: true }:
				sb.Append(" [island]");
				break;
			case FolderEntryDto folder:
				if (folder.Sort is { } sort)
					sb.Append(" sort=").Append(sort);
				if (folder.Exclude is { Count: > 0 } exclude)
					sb.Append(" exclude=[").Append(string.Join(", ", exclude)).Append(']');
				break;
			case ListingEntryDto listing:
				sb.Append(" listing=").Append(listing.Options);
				break;
		}

		var defaultCta = entry switch
		{
			FileEntryDto f => f.DefaultCta,
			FolderEntryDto f => f.DefaultCta,
			_ => null
		};
		if (defaultCta is not null)
			sb.Append(" cta=").Append(defaultCta);

		sb.Append('\n');
		foreach (var child in entry.Children)
			Write(sb, child, depth + 1);
	}

	private static string TypeName(ITocEntryDto entry) => entry switch
	{
		IndexFileEntryDto or FolderIndexFileEntryDto => "IndexFileRef",
		FileEntryDto => "FileRef",
		FolderEntryDto => "FolderRef",
		NestedTocEntryDto => "IsolatedTableOfContentsRef",
		CrossLinkEntryDto => "CrossLinkRef",
		ListingEntryDto => "ListingRef",
		_ => entry.GetType().Name
	};

	private static string EntryPath(ITocEntryDto entry) => entry switch
	{
		FileEntryDto f => f.Path,
		FolderEntryDto f => f.Path,
		NestedTocEntryDto n => n.Path,
		CrossLinkEntryDto c => c.CrossLinkUri.ToString(),
		ListingEntryDto l => l.Path,
		InvalidTocEntryDto i => i.RawValue,
		_ => string.Empty
	};

	private static void Write(StringBuilder sb, ITableOfContentsItem item, int depth)
	{
		var path = item.PathRelativeToDocumentationSet.Value;
		sb.Append(' ', depth * 2).Append(item.GetType().Name).Append("  ").Append(path);
		if (item.PathRelativeToContainer.Value != path)
			sb.Append(" (container-relative: ").Append(item.PathRelativeToContainer.Value).Append(')');

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
		foreach (var child in item.Children)
			Write(sb, child.Item(), depth + 1);
	}
}
