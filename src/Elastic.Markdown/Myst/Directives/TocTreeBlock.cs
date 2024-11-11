// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using Markdig.Helpers;

namespace Elastic.Markdown.Myst.Directives;

public class TocTreeLink
{
	public required string Link { get; init; }
	public string? Title { get; set; }
}

public class TocTreeBlock(DirectiveBlockParser parser, Dictionary<string, string> properties)
	: DirectiveBlock(parser, properties)
{
	public OrderedList<TocTreeLink> Links { get; } = new();

	public override void FinalizeAndValidate(ParserContext context)
	{
	}
}
