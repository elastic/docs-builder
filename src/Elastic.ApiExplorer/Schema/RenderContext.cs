// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Schema;

/// <summary>
/// Context for rendering a property list.
/// </summary>
public record PropertyRenderContext
{
	/// <summary>The schema containing properties to render.</summary>
	public required IOpenApiSchema? Schema { get; init; }

	/// <summary>Set of required property names.</summary>
	public required ISet<string>? RequiredProperties { get; init; }

	/// <summary>ID prefix for property anchors.</summary>
	public required string Prefix { get; init; }

	/// <summary>Current nesting depth.</summary>
	public required int Depth { get; init; }

	/// <summary>Set of ancestor type names for recursion detection.</summary>
	public required HashSet<string>? AncestorTypes { get; init; }

	/// <summary>Whether rendering request properties (shows "required" badge) vs response (shows "optional").</summary>
	public required bool IsRequest { get; init; }

	/// <summary>The schema analyzer for type resolution.</summary>
	public required SchemaAnalyzer Analyzer { get; init; }

	/// <summary>Function to render markdown to HTML.</summary>
	public required Func<string?, HtmlString> RenderMarkdown { get; init; }

	/// <summary>Maximum depth for property expansion.</summary>
	public int MaxDepth { get; init; } = SchemaHelpers.MaxDepth;
}

/// <summary>
/// Context for rendering a single property item.
/// </summary>
public record PropertyItemContext
{
	/// <summary>The property name/key.</summary>
	public required string PropertyName { get; init; }

	/// <summary>The property's schema.</summary>
	public required IOpenApiSchema PropertySchema { get; init; }

	/// <summary>Type information for the property.</summary>
	public required TypeInfo TypeInfo { get; init; }

	/// <summary>The HTML ID for the property anchor.</summary>
	public required string PropId { get; init; }

	/// <summary>Whether this property is required.</summary>
	public required bool IsRequired { get; init; }

	/// <summary>Whether this is the last property in the list.</summary>
	public required bool IsLast { get; init; }

	/// <summary>Whether this type is recursive (appears in ancestors).</summary>
	public required bool IsRecursive { get; init; }

	/// <summary>The parent rendering context.</summary>
	public required PropertyRenderContext ParentContext { get; init; }
}

/// <summary>
/// Context for rendering a schema type annotation.
/// </summary>
public record SchemaTypeContext
{
	/// <summary>The schema to render type information for.</summary>
	public required IOpenApiSchema Schema { get; init; }

	/// <summary>Pre-computed type information.</summary>
	public required TypeInfo TypeInfo { get; init; }

	/// <summary>Whether the schema has actual properties (for showing {} icon).</summary>
	public required bool HasActualProperties { get; init; }
}

/// <summary>
/// Context for rendering union variants.
/// </summary>
public record UnionVariantsContext
{
	/// <summary>The union options to render.</summary>
	public required List<UnionOption> Options { get; init; }

	/// <summary>ID prefix for variant anchors.</summary>
	public required string Prefix { get; init; }

	/// <summary>Current nesting depth.</summary>
	public required int Depth { get; init; }

	/// <summary>Set of ancestor type names for recursion detection.</summary>
	public required HashSet<string>? AncestorTypes { get; init; }

	/// <summary>Whether rendering request properties.</summary>
	public required bool IsRequest { get; init; }

	/// <summary>The schema analyzer for type resolution.</summary>
	public required SchemaAnalyzer Analyzer { get; init; }

	/// <summary>Function to render markdown to HTML.</summary>
	public required Func<string?, HtmlString> RenderMarkdown { get; init; }
}
