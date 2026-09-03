// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.ApiExplorer.Infrastructure;

namespace Elastic.ApiExplorer.Components.PropertyTree;

/// <summary>Serializes a property tree to readable CommonMark.</summary>
internal static class ApiPropertyMarkdown
{
	public static void WriteList(StringBuilder markdown, ApiPropertyList? properties, string apiBaseUrl, int depth = 0)
	{
		if (properties is null || properties.Items.Count == 0)
			return;

		foreach (var property in properties.Items)
			WriteProperty(markdown, property, apiBaseUrl, depth);
	}

	public static void WriteVariants(StringBuilder markdown, ApiUnionVariants? variants, string apiBaseUrl, int depth = 0)
	{
		if (variants is null || variants.Variants.Count == 0)
			return;

		foreach (var variant in variants.Variants)
		{
			var label = variant.IsArrayVariant ? $"[]{variant.DisplayName}" : variant.DisplayName;
			_ = markdown.Append(Indent(depth));
			_ = markdown.Append("- **");
			_ = markdown.Append(label);
			_ = markdown.AppendLine("**");
			if (variant.Properties is not null)
				WriteList(markdown, variant.Properties, apiBaseUrl, depth + 1);
		}
	}

	public static void WriteType(StringBuilder markdown, TypeAnnotation? type)
	{
		if (type is null || string.IsNullOrEmpty(type.Text))
			return;

		_ = markdown.Append("Type: `");
		_ = markdown.Append(type.Text);
		_ = markdown.AppendLine("`");
		_ = markdown.AppendLine();
	}

	private static void WriteProperty(StringBuilder markdown, ApiProperty property, string apiBaseUrl, int depth)
	{
		_ = markdown.Append(Indent(depth));
		_ = markdown.Append("- `");
		_ = markdown.Append(property.Name);
		_ = markdown.Append('`');
		if (!string.IsNullOrEmpty(property.Type.Text))
		{
			_ = markdown.Append(" (");
			_ = markdown.Append(property.Type.Text.Trim());
			_ = markdown.Append(')');
		}

		if (property.IsRequest)
			_ = markdown.Append(property.IsRequired ? " — required" : " — optional");
		if (property.ShowDeprecatedBadge)
			_ = markdown.Append(" — deprecated");
		if (property.IsRecursive)
			_ = markdown.Append(" — recursive");
		_ = markdown.AppendLine();

		WriteNestedLine(markdown, depth, ApiMarkdown.Prepare(property.DescriptionMarkdown, apiBaseUrl));
		WriteConstraints(markdown, property, depth);
		WriteEnumOrUnion(markdown, property, depth);
		if (property.TypeLink is { Url: { Length: > 0 } url })
			WriteNestedLine(markdown, depth, $"See {ApiCommonMark.Link(property.TypeLink.TypeName, url)}");

		WriteChildren(markdown, property, apiBaseUrl, depth);
	}

	private static void WriteConstraints(StringBuilder markdown, ApiProperty property, int depth)
	{
		foreach (var constraint in property.Constraints)
		{
			var text = constraint.Code is null ? constraint.Text : $"{constraint.Text}`{constraint.Code}`";
			WriteNestedLine(markdown, depth, text);
		}

		if (property.ArrayItemTypeName is { Length: > 0 })
			WriteNestedLine(markdown, depth, $"Array of: `{property.ArrayItemTypeName}`");
	}

	private static void WriteEnumOrUnion(StringBuilder markdown, ApiProperty property, int depth)
	{
		if (property.EnumValues.Count > 0)
			WriteNestedLine(markdown, depth, "Values: " + string.Join(", ", property.EnumValues.Select(v => $"`{v}`")));

		if (property.Union is null)
			return;

		switch (property.Union.Kind)
		{
			case UnionDisplayKind.EnumLike when property.Union.EnumLikeValues.Count > 0:
				WriteNestedLine(markdown, depth, "Values: " + string.Join(", ", property.Union.EnumLikeValues.Select(v => $"`{v}`")));
				break;
			case UnionDisplayKind.SimpleArrayUnion when property.Union.SimpleUnionBaseName is { Length: > 0 } name:
				WriteNestedLine(markdown, depth, $"One of: `{name}` or `[]{name}`");
				break;
			case UnionDisplayKind.Badges when property.Union.Badges.Count > 0:
				WriteNestedLine(markdown, depth, "One of: " + string.Join(" or ", property.Union.Badges.Select(b => $"`{b.Text}`")));
				break;
		}
	}

	private static void WriteChildren(StringBuilder markdown, ApiProperty property, string apiBaseUrl, int depth)
	{
		switch (property.Children.Kind)
		{
			case ChildKind.PropertyList:
			case ChildKind.SimpleUnionVariants:
				WriteList(markdown, property.Children.Properties, apiBaseUrl, depth + 1);
				WriteVariants(markdown, property.Children.Variants, apiBaseUrl, depth + 1);
				break;
			case ChildKind.UnionVariants:
				WriteVariants(markdown, property.Children.Variants, apiBaseUrl, depth + 1);
				break;
			case ChildKind.Dictionary when property.Children.Dictionary is { } dictionary:
				WriteNestedLine(markdown, depth, $"Map values ({dictionary.ValueType.Text})");
				WriteList(markdown, dictionary.Properties, apiBaseUrl, depth + 1);
				break;
		}
	}

	private static void WriteNestedLine(StringBuilder markdown, int depth, string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return;

		_ = markdown.Append(Indent(depth + 1));
		_ = markdown.AppendLine(text.TrimEnd());
	}

	private static string Indent(int depth) => new(' ', depth * 2);
}
