// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;

namespace Elastic.Documentation.Configuration.Toc;

/// <summary>Compares strings using natural sort order, where numeric segments are compared as integers ("3_2" &lt; "3_10").</summary>
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

/// <summary>Frontmatter value for the <c>listing</c> key — both shorthand (<c>listing: group-name</c>)
/// and full form (<c>listing: {group: group-name}</c>) parse to this.</summary>
public class ListingFrontMatter
{
	/// <summary>The group key this page belongs to inside its listing.</summary>
	public string? Group { get; set; }
}
