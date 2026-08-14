// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.RelatedLearning;
using Markdig.Extensions.Footnotes;
using Markdig.Parsers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Myst.RelatedLearning;

public sealed class RelatedLearningBlock : LeafBlock
{
	public const string Heading = "Related learning";
	public const string Anchor = "related-learning-heading";

	public RelatedLearningBlock(BlockParser? parser) : base(parser) { }

	public required IReadOnlyList<RelatedLearningLink> Links { get; init; }

	public static void Append(MarkdownDocument document, IReadOnlyList<RelatedLearningLink> links)
	{
		if (links.Count == 0)
			return;
		if (document.Any(static b => b is RelatedLearningBlock))
			return;

		var heading = new HeadingBlock(null)
		{
			Level = 2,
			Line = int.MaxValue - 1
		};
		heading.SetData("header", Heading);
		heading.SetData("anchor", Anchor);
		heading.Inline = new ContainerInline();
		heading.Inline.AppendChild(new LiteralInline(Heading));

		var list = new RelatedLearningBlock(null)
		{
			Links = links,
			Line = int.MaxValue
		};

		var insertAt = document.Count;
		for (var i = 0; i < document.Count; i++)
		{
			if (document[i] is FootnoteGroup)
			{
				insertAt = i;
				break;
			}
		}

		document.Insert(insertAt, heading);
		document.Insert(insertAt + 1, list);
	}
}
