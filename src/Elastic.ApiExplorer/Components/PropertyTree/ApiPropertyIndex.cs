// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Infrastructure;
using Elastic.Documentation.Search.Contract;

namespace Elastic.ApiExplorer.Components.PropertyTree;

internal static class ApiPropertyIndex
{
	public static IEnumerable<ApiPropertyEntry> Entries(IEnumerable<ApiProperty> properties, string location) =>
		properties.Select(
			property => new ApiPropertyEntry
			{
				Name = property.Name,
				Path = string.IsNullOrEmpty(property.JsonPath) ? property.Name : property.JsonPath,
				Type = property.Type.Text,
				Location = location,
				Anchor = property.AnchorId,
				Description = ApiMarkdown.StripHtml(ApiSeoDescription.ToPlainText(property.DescriptionMarkdown ?? ""))
			}
		);

	public static IEnumerable<ApiProperty> Flatten(ApiPropertyList properties)
	{
		foreach (var property in properties.Items)
		{
			yield return property;
			if (property.Children.Properties is { } children)
			{
				foreach (var child in Flatten(children))
					yield return child;
			}
			if (property.Children.Dictionary is { } dictionary)
			{
				foreach (var child in Flatten(dictionary.Properties))
					yield return child;
			}
			foreach (var child in Flatten(property.Children.Variants))
				yield return child;
		}
	}

	public static IEnumerable<ApiProperty> Flatten(ApiUnionVariants? variants) =>
		variants?.Variants.Where(v => v.ShowProperties && v.Properties is not null).SelectMany(v => Flatten(v.Properties!)) ?? [];
}
