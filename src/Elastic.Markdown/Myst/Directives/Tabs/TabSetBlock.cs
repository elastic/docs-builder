// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Helpers;

namespace Elastic.Markdown.Myst.Directives.Tabs;

public class TabSetBlock(DirectiveBlockParser parser, ParserContext context)
	: DirectiveBlock(parser, context)
{
	public override string Directive => "tab-set";

	public int Index { get; set; }
	public string? GetGroupKey() => Prop("group");

	public override void FinalizeAndValidate(ParserContext context) => Index = FindIndex();

	private int _index = -1;

	// For simplicity, we use the line number as the index.
	// It's not ideal, but it's unique.
	// This is more efficient than finding the root block and then finding the index.
	public int FindIndex()
	{
		if (_index > -1)
			return _index;

		_index = Line;
		return _index;
	}
}

public class TabItemBlock(DirectiveBlockParser parser, ParserContext context)
	: DirectiveBlock(parser, context)
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
		Index = Parent!.IndexOf(this);

		var tabSet = Parent as TabSetBlock;

		TabSetIndex = tabSet?.FindIndex() ?? -1;
		TabSetGroupKey = tabSet?.GetGroupKey();

		SyncKey = Prop("sync");
		Selected = PropBool("selected");
	}

}
