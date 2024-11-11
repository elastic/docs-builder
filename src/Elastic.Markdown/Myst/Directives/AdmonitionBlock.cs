// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
namespace Elastic.Markdown.Myst.Directives;

public class AdmonitionBlock(DirectiveBlockParser parser, string admonition, Dictionary<string, string> properties)
	: DirectiveBlock(parser, properties)
{
	public string Admonition => admonition == "admonition" ? Classes?.Trim() ?? "note" : admonition;
	public string? Classes { get; protected set; }
	public string? CrossReferenceName  { get; private set; }
	public bool? DropdownOpen  { get; private set; }

	public string Title
	{
		get
		{
			var t = Admonition == "seealso" ? "see also" : Admonition;
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
		Classes = Properties.GetValueOrDefault("class");
		CrossReferenceName = Properties.GetValueOrDefault("name");
		ParseBool("open", b => DropdownOpen = b);
	}
}


public class DropdownBlock(DirectiveBlockParser parser, Dictionary<string, string> properties)
	: AdmonitionBlock(parser, "admonition", properties)
{
	// ReSharper disable once RedundantOverriddenMember
	public override void FinalizeAndValidate(ParserContext context)
	{
		base.FinalizeAndValidate(context);
		Classes = $"dropdown {Classes}";
	}
}
