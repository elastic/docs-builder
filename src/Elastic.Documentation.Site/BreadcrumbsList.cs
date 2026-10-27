// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Documentation.Navigation;

namespace Elastic.Documentation.Site;

// Model structure based on https://developers.google.com/search/docs/appearance/structured-data/breadcrumb#json-ld
public record BreadcrumbsList
{
	[JsonPropertyName("@context")]
	public string Context => "https://schema.org";
	[JsonPropertyName("@type")]
	public string Type => "BreadcrumbList";
	[JsonPropertyName("itemListElement")]
	public required List<BreadcrumbListItem> ItemListElement { get; init; }
}

public record BreadcrumbListItem
{
	[JsonPropertyName("@type")]
	public string Type => "ListItem";
	[JsonPropertyName("position")]
	public required int Position { get; init; }
	[JsonPropertyName("name")]
	public required string Name { get; init; }

	[JsonPropertyName("item")]
	public string? Item { get; init; }
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(BreadcrumbsList))]
public sealed partial class BreadcrumbsContext : JsonSerializerContext;

public static class BreadcrumbJson
{
	public static string Serialize(IReadOnlyList<INavigationItem> parents, string currentName, Uri? canonicalBaseUrl)
	{
		var baseUri = canonicalBaseUrl ?? new Uri("http://localhost");
		var position = 1;
		var items = new List<BreadcrumbListItem>(parents.Count + 1);
		foreach (var parent in parents)
		{
			items.Add(new BreadcrumbListItem
			{
				Position = position++,
				Name = parent.NavigationTitle,
				Item = new Uri(baseUri, parent.Url).ToString()
			});
		}

		items.Add(new BreadcrumbListItem { Position = position, Name = currentName, Item = null });
		return JsonSerializer.Serialize(new BreadcrumbsList { ItemListElement = items }, BreadcrumbsContext.Default.BreadcrumbsList);
	}
}
