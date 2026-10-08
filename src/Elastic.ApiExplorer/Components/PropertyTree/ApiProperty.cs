// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Types;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Components.PropertyTree;

/// <summary>An external documentation link with its elastic.co treatment precomputed.</summary>
public record ExternalDocLink(string Url, bool IsElasticDocs, string? Description = null)
{
	public string LinkText =>
		!string.IsNullOrWhiteSpace(Description)
			? Description
			: IsElasticDocs ? "Read the reference documentation" : "External documentation";
}

/// <summary>An earlier row on the page whose fields (or union options) a row repeats; the row links back to it.</summary>
/// <remarks><see cref="Owner"/> names the variant or row the first listing sits in, since many listings share a name.</remarks>
public record RepeatedShape(string Name, string AnchorId, bool IsUnion, string? Owner = null)
{
	public string Label => IsUnion ? "Same options as" : "Same fields as";
}

/// <summary>A link from a group-4 property row to that schema's dedicated page.</summary>
public record TypePageLink(string TypeName, string? Url);

/// <summary>A single validation constraint, e.g. <c>min: 5</c> or <c>default: 10</c>.</summary>
public record ConstraintDisplay(string Text, string? Code = null);

/// <summary>How a union renders inline on a property row.</summary>
public enum UnionDisplayKind
{
	/// <summary>An <c>X | X[]</c> pair: rendered as a compact "One of: X or []X" row.</summary>
	SimpleArrayUnion,

	/// <summary>Everything else: rendered as "One of:" badges (empty when variants expand below).</summary>
	Badges
}

/// <summary>One "One of:" badge for a union option.</summary>
public record UnionBadge(string Text, bool IsTypeOption);

/// <summary>The precomputed inline rendering of a union type on a property row.</summary>
public record UnionDisplay
{
	public required UnionDisplayKind Kind { get; init; }

	/// <summary>Base type name of an <c>X | X[]</c> union (<see cref="UnionDisplayKind.SimpleArrayUnion"/>).</summary>
	public string? SimpleUnionBaseName { get; init; }

	/// <summary>Whether the simple union's base type is an object (shows the <c>{}</c> icon).</summary>
	public bool SimpleUnionIsObject { get; init; }

	/// <summary>Value-type keyword prefix (e.g. <c>string </c>) for the simple union base name.</summary>
	public string SimpleUnionValueTypePrefix { get; init; } = "";

	/// <summary>Union option badges (<see cref="UnionDisplayKind.Badges"/>).</summary>
	public IReadOnlyList<UnionBadge> Badges { get; init; } = [];

	/// <summary>The schema keyword the union came from, <c>oneOf</c> or <c>anyOf</c>.</summary>
	public UnionKeyword? Keyword { get; init; }

	/// <summary>"Any of:" when any number of options can match, "One of:" otherwise.</summary>
	public string Label => SchemaHelpers.UnionLabel(Keyword);

	/// <summary>Discriminator property name shown next to the badges.</summary>
	public string? DiscriminatorProperty { get; init; }
}

/// <summary>Which nested rendering a property expands into.</summary>
public enum ChildKind
{
	None,

	/// <summary>Dictionary value properties nested under a synthetic <c>&lt;string&gt;</c> key row.</summary>
	Dictionary,

	/// <summary>A plain nested property list (object, array items or simple-union base type).</summary>
	PropertyList,

	/// <summary>Union variants of a regular union property; always visible.</summary>
	UnionVariants,

	/// <summary>Union variants nested under an X | X[] simple union; participates in hidden="until-found".</summary>
	SimpleUnionVariants
}

/// <summary>The synthetic <c>&lt;string&gt;</c> key row a dictionary property nests its value properties under.</summary>
public record DictionaryChildDisplay
{
	public required string KeyAnchorId { get; init; }
	public required int Depth { get; init; }
	public required bool IsCollapsible { get; init; }
	public required bool DefaultExpanded { get; init; }
	public required int NestedCount { get; init; }
	public required bool UseHidden { get; init; }
	public required TypeAnnotation ValueType { get; init; }
	public required ApiPropertyList Properties { get; init; }
}

/// <summary>The precomputed children of a property, discriminated by <see cref="Kind"/>.</summary>
public record ApiPropertyChildren
{
	public static readonly ApiPropertyChildren None = new() { Kind = ChildKind.None, UseHidden = false };

	public required ChildKind Kind { get; init; }

	/// <summary>Whether the child wrapper renders with hidden="until-found".</summary>
	public required bool UseHidden { get; init; }

	public ApiPropertyList? Properties { get; init; }
	public ApiUnionVariants? Variants { get; init; }
	public DictionaryChildDisplay? Dictionary { get; init; }
}

/// <summary>
/// A single renderable property row: wraps the underlying <see cref="IOpenApiSchema"/> for verbatim
/// scalar reads and precomputes every structural and display decision the view needs.
/// </summary>
public record ApiProperty
{
	public required string Name { get; init; }

	/// <summary>The underlying OpenAPI schema; views read scalar values off it directly.</summary>
	public required IOpenApiSchema Schema { get; init; }

	public required string AnchorId { get; init; }
	public required int Depth { get; init; }
	public required bool IsRequired { get; init; }
	public required bool IsLast { get; init; }
	public required bool IsRecursive { get; init; }

	/// <summary>Whether the row is in a request body (vs response); kept for callers that branch on context.</summary>
	public required bool IsRequest { get; init; }

	public required TypeAnnotation Type { get; init; }

	/// <summary>Markdown-rendered description; empty when the schema has none.</summary>
	public required HtmlString DescriptionHtml { get; init; }
	public required string? DescriptionMarkdown { get; init; }
	public bool HasDescription => DescriptionHtml.Value is { Length: > 0 };

	public required bool ShowDeprecatedBadge { get; init; }
	public AvailabilityBadgeData? Availability { get; init; }
	public ExternalDocLink? ExternalDocs { get; init; }
	public IReadOnlyList<ConstraintDisplay> Constraints { get; init; } = [];
	public IReadOnlyList<string> EnumValues { get; init; } = [];
	public UnionDisplay? Union { get; init; }

	/// <summary>Item type name for primitive arrays, rendered as an "Array of:" row.</summary>
	public string? ArrayItemTypeName { get; init; }

	public TypePageLink? TypeLink { get; init; }

	/// <summary>The earlier row whose fields or options this row repeats; set instead of listing them a second time.</summary>
	public RepeatedShape? Repeats { get; init; }

	/// <summary>Further schemas an <c>allOf</c> merges in; the row's type names only the first.</summary>
	public IReadOnlyList<TypePageLink> AlsoIncludes { get; init; } = [];

	/// <summary>Which of the object's fields it needs, when a <c>oneOf</c>/<c>anyOf</c> only lists <c>required</c> sets.</summary>
	public RequiredAlternatives? Requires { get; init; }

	public required bool IsCollapsible { get; init; }
	public required bool DefaultExpanded { get; init; }
	public required int NestedCount { get; init; }

	public required ApiPropertyChildren Children { get; init; }
}

/// <summary>An ordered list of property rows; the model for <c>_PropertyList</c>.</summary>
public record ApiPropertyList(IReadOnlyList<ApiProperty> Items);

/// <summary>A "Requires at least one of:" row; each option is a field, or fields joined by <c>+</c> that go together.</summary>
public record RequiredAlternatives(string Label, IReadOnlyList<string> Options);

/// <summary>A "Values:" row; long lists show the first few literals and fold the rest behind a toggle.</summary>
public record EnumValueList(IReadOnlyList<string> Values)
{
	private const int CollapseAbove = 20;
	private const int VisibleWhenCollapsed = 12;

	private bool Folds => Values.Count > CollapseAbove;
	public IReadOnlyList<string> Visible => Folds ? Values.Take(VisibleWhenCollapsed).ToArray() : Values;
	public IReadOnlyList<string> Folded => Folds ? Values.Skip(VisibleWhenCollapsed).ToArray() : [];
}
