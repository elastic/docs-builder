// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.ApiSummary;

public class ApiSummaryBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "api-summary";

	public string? Tag { get; private set; }
	public string? Product { get; private set; }
	public string? Type { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		Tag = Prop("tag");
		Product = Prop("product");
		Type = Prop("type");
	}

	public bool IsDescriptionKind =>
		string.Equals(Type, "description", StringComparison.OrdinalIgnoreCase);

	public bool IsOperationsKind =>
		string.IsNullOrWhiteSpace(Type) ||
		string.Equals(Type, "operations", StringComparison.OrdinalIgnoreCase);
}
