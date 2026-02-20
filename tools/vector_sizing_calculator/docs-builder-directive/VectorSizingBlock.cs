// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.VectorSizing;

/// <summary>
/// Represents a {vector-sizing-calculator} directive block that renders
/// an interactive vector sizing calculator web component.
///
/// Usage in MyST Markdown:
///   :::{vector-sizing-calculator}
///   :::
///
/// The directive takes no arguments or body content.
/// It renders a &lt;vector-sizing-calculator&gt; custom element that is
/// hydrated client-side by the bundled React+EUI web component.
/// </summary>
public class VectorSizingBlock(DirectiveBlockParser parser, ParserContext context)
    : DirectiveBlock(parser, context)
{
    public override string Directive => "vector-sizing-calculator";

    public override void FinalizeAndValidate(ParserContext context)
    {
        // No properties or arguments to validate.
        // The web component handles all configuration client-side.
    }
}
