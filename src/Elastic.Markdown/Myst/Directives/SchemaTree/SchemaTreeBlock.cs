// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst.Directives.SchemaTree;

/// <summary>Renders an ordinary Markdown field list as an expandable tree.</summary>
public class SchemaTreeBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "schema-tree";
	public ListBlock? Tree { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		if (Count != 1 || this[0] is not ListBlock { IsOrdered: false } list)
		{
			this.EmitError("schema-tree requires one unordered Markdown list.");
			return;
		}

		Tree = list;
		ValidateFields(list);
	}

	// Match the view's traversal: description blocks can contain unrelated lists.
	private void ValidateFields(ListBlock list)
	{
		foreach (var item in list.OfType<ListItemBlock>())
		{
			if (item.Count == 0 || item[0] is not ParagraphBlock)
				this.EmitError("Each schema-tree item requires a label paragraph.");
			foreach (var child in item.OfType<ListBlock>())
			{
				if (child.IsOrdered)
					this.EmitError("schema-tree child lists must be unordered.");
				ValidateFields(child);
			}
		}
	}
}
