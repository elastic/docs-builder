// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
namespace Elastic.Markdown.Myst.Directives;

public class CardBlock(DirectiveBlockParser parser, Dictionary<string, string> properties)
	: DirectiveBlock(parser, properties)
{
	public string? Link { get; set; }

	public string? Title { get; set; }

	public string? Header { get; set; }

	public string? Footer { get; set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		Title = Arguments;
		Link = Properties.GetValueOrDefault("link");
		//TODO Render
		Header = Properties.GetValueOrDefault("header");
		Footer = Properties.GetValueOrDefault("footer");
	}
}
