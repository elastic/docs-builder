// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;

namespace Elastic.Markdown.Myst.Directives;

public class UnsupportedDirectiveBlock(DirectiveBlockParser parser, string directive, Dictionary<string, string> properties, int issueId)
	: DirectiveBlock(parser, properties)
{
	public string Directive => directive;

	public string IssueUrl => $"https://github.com/elastic/docs-builder/issues/{issueId}";

	public override void FinalizeAndValidate(ParserContext context) =>
		context.EmitWarning(line:1, column:1, length:2, message: $"Directive block '{directive}' is unsupported. See {IssueUrl} for more information.");
}