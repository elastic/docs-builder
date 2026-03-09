// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;

namespace Elastic.Documentation.Configuration.Toc;

/// <summary>
/// Specifies the sort order for auto-discovered files in a folder.
/// </summary>
public enum SortOrder
{
	/// <summary>
	/// Sort files in ascending alphabetical order (A-Z). This is the default.
	/// </summary>
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

/// <summary>Compares strings using natural sort order, where numeric segments are compared as integers (e.g. "3_2" &lt; "3_10").</summary>
public sealed class NaturalStringComparer : IComparer<string>
{
	public static NaturalStringComparer Instance { get; } = new();

	public int Compare(string? x, string? y)
	{
		if (ReferenceEquals(x, y))
			return 0;
		if (x is null)
			return -1;
		if (y is null)
			return 1;

		var ix = 0;
		var iy = 0;

		while (ix < x.Length && iy < y.Length)
		{
			if (char.IsDigit(x[ix]) && char.IsDigit(y[iy]))
			{
				// Compare numeric segments as integers
				var nx = ParseNumber(x, ref ix);
				var ny = ParseNumber(y, ref iy);
				var cmp = nx.CompareTo(ny);
				if (cmp != 0)
					return cmp;
			}
			else
			{
				var cmp = x[ix].CompareTo(y[iy]);
				if (cmp != 0)
					return cmp;
				ix++;
				iy++;
			}
		}

		return x.Length.CompareTo(y.Length);
	}

	private static long ParseNumber(string s, ref int index)
	{
		var start = index;
		while (index < s.Length && char.IsDigit(s[index]))
			index++;
		return long.Parse(s[start..index], CultureInfo.InvariantCulture);
	}
}

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

/// <param name="Sort">Raw YAML sort value, parsed and validated during resolution via <see cref="SortOrderExtensions.TryParse"/>.</param>
public record FolderRef(string PathRelativeToDocumentationSet, string PathRelativeToContainer, IReadOnlyCollection<ITableOfContentsItem> Children, string Context, string? Sort = null)
	: ITableOfContentsItem;

public record IsolatedTableOfContentsRef(string PathRelativeToDocumentationSet, string PathRelativeToContainer, IReadOnlyCollection<ITableOfContentsItem> Children, string Context)
	: ITableOfContentsItem;
