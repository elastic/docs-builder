// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;

namespace Elastic.Markdown.Myst.Directives;

public class TabSetBlock(DirectiveBlockParser parser, Dictionary<string, string> properties, ParserContext context)
	: DirectiveBlock(parser, properties, context)
{
	public override string Directive => "tab-set";

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
public class TabItemBlock(DirectiveBlockParser parser, Dictionary<string, string> properties, ParserContext context)
	: DirectiveBlock(parser, properties, context)
{
	public override string Directive => "tab-item";

	public string Title { get; private set; } = default!;
	public int Index { get; private set; }
	public int TabSetIndex { get; private set; }

	public string? SyncKey { get; private set; }
	public bool Selected { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		if (string.IsNullOrWhiteSpace(Arguments))
			this.EmitError("{tab-item} requires an argument to name the tab.");

		Title = Arguments ?? "{undefined}";
		Index = Parent!.IndexOf(this);
		TabSetIndex = Parent is TabSetBlock tb ? tb.FindIndex() : -1;

		SyncKey = Prop("sync");
		Selected = PropBool("selected");
	}

}
