// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Infrastructure;
using Elastic.Documentation.Search.Contract;

namespace Elastic.ApiExplorer.Components.PropertyTree;

internal static class ApiPropertyIndex
{
	/// <param name="plainDescriptions">Plain text per description markdown, shared across a spec: shared schemas repeat the same text on every operation.</param>
	public static IEnumerable<ApiPropertyEntry> Entries(
		IEnumerable<ApiProperty> properties,
		string location,
		Dictionary<string, string> plainDescriptions
	) =>
		properties.Select(
			property => new ApiPropertyEntry
			{
				Name = property.Name,
				Path = string.IsNullOrEmpty(property.JsonPath) ? property.Name : property.JsonPath,
				Type = property.Type.Text,
				Location = location,
				Anchor = property.AnchorId,
				Description = PlainText(property.DescriptionMarkdown ?? "", plainDescriptions)
			}
		);

	private static string PlainText(string markdown, Dictionary<string, string> plainDescriptions)
	{
		if (!plainDescriptions.TryGetValue(markdown, out var text))
			plainDescriptions[markdown] = text = ApiMarkdown.StripHtml(ApiSeoDescription.ToPlainText(markdown));
		return text;
	}

	/// <summary>Every property in page order: each property, then its nested, dictionary and variant children.</summary>
	public static IEnumerable<ApiProperty> Flatten(ApiPropertyList? properties) =>
		properties?.Items.SelectMany(
			property => Flatten(property.Children.Properties)
				.Concat(Flatten(property.Children.Dictionary?.Properties))
				.Concat(Flatten(property.Children.Variants))
				.Prepend(property)
		)
			?? [];

	public static IEnumerable<ApiProperty> Flatten(ApiUnionVariants? variants) =>
		variants?.Variants.Where(v => v.ShowProperties && v.Properties is not null).SelectMany(v => Flatten(v.Properties)) ?? [];
}
