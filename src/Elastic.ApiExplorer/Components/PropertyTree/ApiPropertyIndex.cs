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
			var path = PathOf(property);
			return new ApiPropertyEntry
			{
				Name = property.Name,
				Path = path,
				PathWords = string.Join(' ', ApiPropertyPath.Words(path)),
				NameWords = string.Join(' ', ApiPropertyPath.IdentifierWords(property.Name)),
				Type = property.Type.Text,
				Location = location,
				Anchor = property.AnchorId,
				Description = PlainText(property.DescriptionMarkdown ?? "", plainDescriptions),
				Parent = parent
			};
		});

	private static string PathOf(ApiProperty property) => string.IsNullOrEmpty(property.JsonPath) ? property.Name : property.JsonPath;

	private static string PlainText(string markdown, Dictionary<string, string> plainDescriptions)
	{
		if (!plainDescriptions.TryGetValue(markdown, out var text))
			plainDescriptions[markdown] = text = ApiMarkdown.StripHtml(ApiSeoDescription.ToPlainText(markdown));
		return text;
	}

	/// <summary>Every property in page order with the path of the row it is listed under: each property, then its nested, dictionary and variant children.</summary>
	internal static IEnumerable<IndexedRow> Flatten(ApiPropertyList? properties) => Flatten(properties?.Items ?? [], "");

	internal static IEnumerable<IndexedRow> Flatten(ApiUnionVariants? variants) => Flatten(variants?.Rows ?? [], "");

	private static IEnumerable<IndexedRow> Flatten(IEnumerable<ApiProperty> properties, string parent) =>
		properties.SelectMany(
			property => Flatten(property.Children.NestedRows, ApiPropertyPath.ParentKey(PathOf(property))).Prepend(
				new IndexedRow(property, parent)
			)
		);
}
