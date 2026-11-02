// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.S3CostCalculator;

/// <summary>
/// Represents a {s3-cost-calculator} directive block that renders
/// an interactive Amazon S3 cost calculator for ES|QL Data Federation queries.
///
/// Usage in MyST Markdown:
///   :::{s3-cost-calculator}
///   :::
///
/// The directive takes no arguments or body content.
/// It renders a &lt;s3-cost-calculator&gt; custom element that is
/// hydrated client-side by the bundled React+EUI web component.
/// </summary>
public class S3CostCalculatorBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "s3-cost-calculator";

	public override void FinalizeAndValidate(ParserContext context) => _ = context;
}
