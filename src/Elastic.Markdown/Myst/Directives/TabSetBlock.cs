// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.Xml;
using System.Xml.Xsl;

namespace Elastic.Markdown.Myst.Directives;

public class TabSetBlock(DirectiveBlockParser parser, Dictionary<string, string> properties)
	: DirectiveBlock(parser, properties)
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
public class TabItemBlock(DirectiveBlockParser parser, Dictionary<string, string> properties)
	: DirectiveBlock(parser, properties)
{
	public override string Directive => "tab-set-item";

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
