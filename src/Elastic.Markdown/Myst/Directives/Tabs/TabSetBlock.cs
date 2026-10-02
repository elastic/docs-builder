// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Helpers;

namespace Elastic.Markdown.Myst.Directives.Tabs;

public class TabSetBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "tab-set";

	/// <summary>
	/// Group keys that render as a dropdown rather than a horizontal tab strip by default.
	/// These are the groups that routinely carry more options than fit on one row.
	/// </summary>
	private static readonly HashSet<string> DropdownGroups = new(["languages"], StringComparer.OrdinalIgnoreCase);

	public int Index { get; set; }
	public string? GetGroupKey() => Prop("group");

	/// <summary>
	/// Renders the tab strip as a &lt;select&gt;. Defaults to true for <see cref="DropdownGroups"/>,
	/// and can be forced either way per tab-set with `:dropdown: true|false`.
	/// </summary>
	public bool RenderAsDropdown() => TryPropBool("dropdown") ?? DropdownGroups.Contains(GetGroupKey() ?? string.Empty);

	public override void FinalizeAndValidate(ParserContext context) => Index = FindIndex();

	private int _index = -1;

	public int FindIndex()
	{
		if (_index > -1)
			return _index;

		_index = GetUniqueLineIndex();
		return _index;
	}
}

public class TabItemBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context), IBlockTitle
{
	public override string Directive => "tab-item";

	public string Title { get; private set; } = default!;
	public int Index { get; private set; }
	public int TabSetIndex { get; private set; }
	public string? TabSetGroupKey { get; private set; }
	public string? SyncKey { get; private set; }
	public bool Selected { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		if (string.IsNullOrWhiteSpace(Arguments))
			this.EmitError("{tab-item} requires an argument to name the tab.");

		Title = (Arguments ?? "{undefined}").ReplaceSubstitutions(context);
		Index = Parent!.OfType<TabItemBlock>().ToList().IndexOf(this);

		var tabSet = Parent as TabSetBlock;

		TabSetIndex = tabSet?.FindIndex() ?? -1;
		TabSetGroupKey = tabSet?.GetGroupKey();

		SyncKey = Prop("sync");
		Selected = PropBool("selected");
	}
}
