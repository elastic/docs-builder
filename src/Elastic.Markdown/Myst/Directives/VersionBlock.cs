// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
namespace Elastic.Markdown.Myst.Directives;

public class VersionBlock(DirectiveBlockParser parser, string directive, Dictionary<string, string> properties)
	: DirectiveBlock(parser, properties)
{
	public override string Directive => directive;
	public string Class => directive.Replace("version", "");

	public string Title
	{
		get
		{
			var title = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(directive.Replace("version", "version "));
			if (!string.IsNullOrEmpty(Arguments))
				title += $" {Arguments}";

			return title;
		}
	}

	public override void FinalizeAndValidate(ParserContext context)
	{
	}
}
