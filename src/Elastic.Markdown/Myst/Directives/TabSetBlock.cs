// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.Xml;
using System.Xml.Xsl;

namespace Elastic.Markdown.Myst.Directives;

public class TabSetBlock(DirectiveBlockParser parser, Dictionary<string, string> properties)
	: DirectiveBlock(parser, properties)
{
	public int Index { get; set; }
	public override void FinalizeAndValidate(ParserContext context) => Index = FindIndex();

	private int _index = -1;
	public int FindIndex()
	{
		if (_index > -1) return _index;
		var siblings = Parent!.OfType<TabSetBlock>().ToList();
		_index = siblings.IndexOf(this);
		return _index;
	}
}
public class TabItemBlock(DirectiveBlockParser parser, Dictionary<string, string> properties)
	: DirectiveBlock(parser, properties)
{
	public string Title { get; set; } = default!;
	public int Index { get; set; }
	public int TabSetIndex { get; set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		Title = Arguments ?? "Unnamed Tab";
		Index = Parent!.IndexOf(this);
		TabSetIndex = Parent is TabSetBlock tb ? tb.FindIndex() : -1;
	}

}

public class GridResponsive
{
	public required int Xs { get; init; }
	public required int Sm { get; init; }
	public required int Md { get; init; }
	public required int Lg { get; init; }
}
public class GridCorners
{
	public required int Top { get; init; }
	public required int Bottom { get; init; }
	public required int Left { get; init; }
	public required int Right { get; init; }
}

public class GridBlock(DirectiveBlockParser parser, Dictionary<string, string> properties)
	: DirectiveBlock(parser, properties)
{

	public GridResponsive BreakPoint { get; set; } = new() { Xs = 1, Sm = 1, Md = 2, Lg = 3 };

	/// <summary> Spacing between items. One or four integers (for “xs sm md lg”) between 0 and 5. </summary>
	public GridResponsive? Gutter { get; set; }

	/// <summary> Outer margin of grid. One (all) or four (top bottom left right) values from: 0, 1, 2, 3, 4, 5, auto. </summary>
	public GridCorners? Margin { get; set; }

	/// <summary> Inner padding of grid. One (all) or four (top bottom left right) values from: 0, 1, 2, 3, 4, 5. </summary>
	public GridCorners? Padding { get; set; }

	/// <summary> Create a border around the grid. </summary>
	public bool? Outline { get; set; }

	/// <summary> Reverse the order of the grid items. </summary>
	public bool? Reverse { get; set; }

	/// <summary> Additional CSS classes for the grid container element. </summary>
	public string? ClassContainer { get; set; }

	/// <summary> Additional CSS classes for the grid row element </summary>
	public string? ClassRow { get; set; }


	public override void FinalizeAndValidate(ParserContext context)
	{
		//todo we always assume 4 integers
		if (!string.IsNullOrEmpty(Arguments))
			ParseData(Arguments, (xs, sm, md, lg) => BreakPoint = new() { Xs = xs, Sm = sm, Md = md, Lg = lg });
		else
		{
			//todo invalidate
		}
		if (Properties.GetValueOrDefault("gutter") is { } gutter)
			ParseData(gutter, (xs, sm, md, lg) => Gutter = new() { Xs = xs, Sm = sm, Md = md, Lg = lg });
		if (Properties.GetValueOrDefault("margin") is { } margin)
			ParseData(margin, (top, bottom, left, right) => Margin = new() { Top = top, Bottom = bottom, Left = left, Right = right });
		if (Properties.GetValueOrDefault("padding") is { } padding)
			ParseData(padding, (top, bottom, left, right) => Padding = new() { Top = top, Bottom = bottom, Left = left, Right = right });
		ParseBool("outline", b => Outline = b);
		ParseBool("reverse", b => Reverse = b);

		ClassContainer = Properties.GetValueOrDefault("class-container");
		ClassRow = Properties.GetValueOrDefault("class-row");

	}

	private void ParseData(string data, Action<int, int, int, int> setter, bool allowAuto = true)
	{
		var columns = data.Split(' ')
			.Select(t => int.TryParse(t, out var c) ? c : t == "auto" ? -1 : -2)
			.Where(t => t is > -2 and <= 5)
			.ToArray();
		if (columns.Length == 1)
			setter(columns[0], columns[0], columns[0], columns[0]);
		else if (columns.Length == 4)
			setter(columns[0], columns[1], columns[2], columns[3]);
		else
		{
			//todo invalidate
		}
	}


}
public class GridItemCardBlock(DirectiveBlockParser parser, Dictionary<string, string> properties)
	: DirectiveBlock(parser, properties)
{
	public override void FinalizeAndValidate(ParserContext context)
	{
	}
}
