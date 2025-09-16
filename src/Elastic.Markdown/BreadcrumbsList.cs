// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Markdown;

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
