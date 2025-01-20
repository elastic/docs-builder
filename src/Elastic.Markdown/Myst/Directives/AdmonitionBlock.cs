// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
namespace Elastic.Markdown.Myst.Directives;

public class DropdownBlock(DirectiveBlockParser parser, ParserContext context) : AdmonitionBlock(parser, "dropdown", context);

public class AdmonitionBlock : DirectiveBlock
{
	private readonly string _admonition;

	public AdmonitionBlock(DirectiveBlockParser parser, string admonition, ParserContext context) : base(parser, context)
	{
		_admonition = admonition;
		if (_admonition is "admonition")
			Classes = "plain";
	}

	public string Admonition => _admonition;

	public override string Directive => Admonition;

	public string? Classes { get; protected set; }
	public bool? DropdownOpen { get; private set; }

	public string Title
	{
		get
		{
			var t = Admonition;
			var title = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(t);
			if (_admonition is "admonition" or "dropdown" && !string.IsNullOrEmpty(Arguments))
				title = Arguments;
			else if (!string.IsNullOrEmpty(Arguments))
				title += $" {Arguments}";
			return title;
		}
	}

	public override void FinalizeAndValidate(ParserContext context)
	{
		CrossReferenceName = Prop("name");
		DropdownOpen = TryPropBool("open");
		if (DropdownOpen.HasValue)
			Classes = "dropdown";
	}
}


