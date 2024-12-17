// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
namespace Elastic.Markdown.Myst.Directives;

public class AdmonitionBlock(
	DirectiveBlockParser parser,
	string admonition,
	Dictionary<string, string> properties,
	ParserContext context)
	: DirectiveBlock(parser, properties, context)
{
	public string Admonition => admonition == "admonition" ? Classes?.Trim() ?? "note" : admonition;

	public override string Directive => Admonition;

	public string? Classes { get; protected set; }
	public bool? DropdownOpen  { get; private set; }

	public string Title
	{
		get
		{
			var t = Admonition;
			var title = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(t);
			if (admonition is "admonition" && !string.IsNullOrEmpty(Arguments))
				title = Arguments;
			else if (!string.IsNullOrEmpty(Arguments))
				title += $" {Arguments}";
			return title;
		}
	}

	public override void FinalizeAndValidate(ParserContext context)
	{
		CrossReferenceName = Properties.GetValueOrDefault("name");
		DropdownOpen = PropBool("open");
		if (DropdownOpen.HasValue)
			Classes = "dropdown";
	}
}


public class DropdownBlock(DirectiveBlockParser parser, Dictionary<string, string> properties, ParserContext context)
	: AdmonitionBlock(parser, "admonition", properties, context)
{
}
