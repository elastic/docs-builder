// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Infrastructure;
using Elastic.Documentation.Search.Contract;

namespace Elastic.ApiExplorer.Components.PropertyTree;

internal readonly record struct IndexedRow(ApiProperty Property, string Parent);

internal static class ApiPropertyIndex
{
	/// <param name="plainDescriptions">Plain text per description markdown, shared across a spec: shared schemas repeat the same text on every operation.</param>
	public static IEnumerable<ApiPropertyEntry> Entries(
		IEnumerable<IndexedRow> rows,
		string location,
		Dictionary<string, string> plainDescriptions
	) =>
		rows.Select(row =>
		{
			var (property, parent) = row;
			return new ApiPropertyEntry
			{
				Name = property.Name,
				Path = PathOf(property),
				Type = property.Type.Text,
				Location = location,
				Anchor = property.AnchorId,
				Description = PlainText(property.DescriptionMarkdown ?? "", plainDescriptions),
				Parent = parent
			};
		});

	private static string PathOf(ApiProperty property) => string.IsNullOrEmpty(property.JsonPath) ? property.Name : property.JsonPath;

	/// <summary>The rows nested directly under a property: its fields, its map values' fields, then each listed variant's fields.</summary>
	internal static IEnumerable<ApiProperty> DirectChildren(ApiProperty property) =>
		(property.Children.Properties?.Items ?? []).Concat(property.Children.Dictionary?.Properties.Items ?? []).Concat(
			VariantChildren(property.Children.Variants)
		);

	private static IEnumerable<ApiProperty> VariantChildren(ApiUnionVariants? variants) =>
		variants?.Variants.Where(v => v.ShowProperties && v.Properties is not null).SelectMany(v => v.Properties!.Items) ?? [];

	private static string PlainText(string markdown, Dictionary<string, string> plainDescriptions)
	{
		if (!plainDescriptions.TryGetValue(markdown, out var text))
			plainDescriptions[markdown] = text = ApiMarkdown.StripHtml(ApiSeoDescription.ToPlainText(markdown));
		return text;
	}

	/// <summary>Every property in page order with the path of the row it is listed under: each property, then its nested, dictionary and variant children.</summary>
	public static IEnumerable<IndexedRow> Flatten(ApiPropertyList? properties, string parent = "") =>
		Flatten(properties?.Items ?? [], parent);

	public static IEnumerable<IndexedRow> Flatten(ApiUnionVariants? variants, string parent = "") =>
		Flatten(VariantChildren(variants), parent);

	private static IEnumerable<IndexedRow> Flatten(IEnumerable<ApiProperty> properties, string parent) =>
		properties.SelectMany(
			property => Flatten(DirectChildren(property), ApiPropertyPath.ParentKey(PathOf(property))).Prepend(
				new IndexedRow(property, parent)
			)
		);
}
