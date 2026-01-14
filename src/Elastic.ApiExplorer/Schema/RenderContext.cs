// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Schema;

/// <summary>
/// Controls how property sections collapse/expand by default.
/// </summary>
public enum CollapseMode
{
	/// <summary>Properties always start collapsed when toggle is shown (OperationView behavior).</summary>
	AlwaysCollapsed,

	/// <summary>Depth-based: depth 0 collapsed, deeper levels expand if less than 5 properties (SchemaView behavior).</summary>
	DepthBased
}

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

	/// <summary>Whether to show deprecated badges on deprecated properties.</summary>
	public bool ShowDeprecated { get; init; } = true;

	/// <summary>Whether to show version badges from x-state extension.</summary>
	public bool ShowVersionInfo { get; init; } = true;

	/// <summary>Whether to show external documentation links.</summary>
	public bool ShowExternalDocs { get; init; } = true;

	/// <summary>Whether to use hidden="until-found" for collapsed sections (enables browser find-in-page).</summary>
	public bool UseHiddenUntilFound { get; init; } = true;

	/// <summary>How collapsed sections should be expanded by default.</summary>
	public CollapseMode CollapseMode { get; init; } = CollapseMode.AlwaysCollapsed;
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

	/// <summary>Whether to use hidden="until-found" for collapsed sections (enables browser find-in-page).</summary>
	public bool UseHiddenUntilFound { get; init; } = true;

	/// <summary>How collapsed sections should be expanded by default.</summary>
	public CollapseMode CollapseMode { get; init; } = CollapseMode.AlwaysCollapsed;

	/// <summary>Maximum depth for property expansion.</summary>
	public int MaxDepth { get; init; } = SchemaHelpers.MaxDepth;
}

