// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Security.Cryptography;
using System.Text;

namespace Elastic.ApiExplorer.Components.PropertyTree;

/// <summary>
/// A hash of everything a built listing shows, without its anchors, so two listings can be compared exactly. A row's
/// children count by their own hash, and a row that links to an earlier listing counts as that listing's hash, so a copy
/// whose fields were already linked still matches the listing it copies.
/// </summary>
internal static class ListingContent
{
	public static string Of(ApiPropertyChildren children, PageShapes shapes)
	{
		var text = new StringBuilder();
		Append(text, children, shapes);
		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
	}

	private static void Append(StringBuilder text, ApiPropertyChildren children, PageShapes shapes)
	{
		_ = text.Append(children.Kind).Append('[');
		Append(text, children.Properties, shapes);
		if (children.Variants is { } variants)
		{
			_ = text.Append(variants.Label);
			foreach (var variant in variants.Variants)
			{
				_ = text
					.Append("<v ")
					.Append(variant.DisplayName)
					.Append('|')
					.Append(variant.IsArrayVariant)
					.Append('|')
					.Append(variant.IsObjectType)
					.Append('|')
					.Append(variant.PageUrl)
					.Append('|')
					.Append(variant.DiscriminatorLabel)
					.Append('|')
					.Append(variant.DescriptionHtml.Value)
					.Append('|')
					.Append(variant.ShowProperties);
				Append(text, variant.Properties, shapes);
				_ = text.Append('>');
			}
		}

		if (children.Dictionary is { } dictionary)
		{
			_ = text.Append("<map ").Append(dictionary.ValueType.Text);
			Append(text, dictionary.Properties, shapes);
			_ = text.Append('>');
		}

		_ = text.Append(']');
	}

	private static void Append(StringBuilder text, ApiPropertyList? properties, PageShapes shapes)
	{
		foreach (var property in properties?.Items ?? [])
		{
			_ = text
				.Append("<p ")
				.Append(property.Name)
				.Append('|')
				.Append(property.Type.Text)
				.Append('|')
				.Append(property.IsRequired)
				.Append('|')
				.Append(property.IsRecursive)
				.Append('|')
				.Append(property.ShowDeprecatedBadge)
				.Append('|')
				.Append(property.Availability)
				.Append('|')
				.Append(property.ExternalDocs?.Url)
				.Append('|')
				.Append(property.DescriptionHtml.Value)
				.Append('|')
				.AppendJoin(',', property.EnumValues)
				.Append('|')
				.Append(property.Union?.Label)
				.Append(property.Union?.DiscriminatorProperty)
				.AppendJoin(',', property.Union?.Badges.Select(b => b.Text) ?? [])
				.Append('|')
				.AppendJoin(',', property.AlsoIncludes.Select(t => t.TypeName))
				.Append('|')
				.Append(property.Requires?.Label)
				.AppendJoin(',', property.Requires?.Options ?? [])
				.Append('|')
				.Append(property.TypeLink?.Url)
				.Append('|');
			// Children count by their hash, so a full listing and a link to an identical listing contribute the same.
			_ = property.Repeats is { } repeats
				// A recursion counts by the shape it returns to, not by the row it names, so identical copies under different rows match.
				? text.Append(shapes.ContentOf(repeats) ?? $"ancestor:{shapes.KeyOf(repeats) ?? repeats.Name}")
				: text.Append(Of(property.Children, shapes));
			_ = text.Append('>');
		}
	}
}
