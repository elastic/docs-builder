// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Helpers;

namespace Elastic.Markdown.Myst.Directives.Admonition;

public class DropdownBlock(DirectiveBlockParser parser, ParserContext context) : AdmonitionBlock(parser, "dropdown", context);

public class AdmonitionBlock : DirectiveBlock, ITitledBlock
{
	public AdmonitionBlock(DirectiveBlockParser parser, string admonition, ParserContext context) : base(parser, context)
	{
		Admonition = admonition;
		if (Admonition is "admonition")
			Classes = "plain";

		var t = Admonition;
		var title = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(t);
		Title = title;
	}

	public string Admonition { get; }

	public override string Directive => Admonition;

	public string? Classes { get; protected set; }

	public bool? DropdownOpen { get; private set; }

	public string Title { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		CrossReferenceName = Prop("name");
		DropdownOpen = TryPropBool("open");
		if (DropdownOpen.HasValue)
			Classes = "dropdown";

		if (Admonition is "admonition" or "dropdown" && !string.IsNullOrEmpty(Arguments))
			Title = Arguments;
		else if (!string.IsNullOrEmpty(Arguments))
			Title += $" {Arguments}";
		Title = Title.ReplaceSubstitutions(context);
	}
}
