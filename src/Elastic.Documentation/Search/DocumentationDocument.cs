// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Documentation.AppliesTo;

namespace Elastic.Documentation.Search;

public record ParentDocument
{
	[JsonPropertyName("title")]
	public required string Title { get; set; }

	[JsonPropertyName("url")]
	public required string Url { get; set; }
}

public record DocumentationDocument
{
	[JsonPropertyName("title")]
	public required string Title { get; set; }

	/// <summary>
	/// Search title is a combination of the title and the url components.
	/// This is used for querying to not reward documents with short titles contributing to heavily to scoring
	/// </summary>
	[JsonPropertyName("search_title")]
	public required string SearchTitle { get; set; }

	[JsonPropertyName("type")]
	public required string Type { get; set; } = "doc";

	[JsonPropertyName("url")]
	public required string Url { get; set; } = string.Empty;

	[JsonPropertyName("hash")]
	public string Hash { get; set; } = string.Empty;

	[JsonPropertyName("navigation_depth")]
	public int NavigationDepth { get; set; } = 50; //default to a high number so that omission gets penalized.

	[JsonPropertyName("navigation_table_of_contents")]
	public int NavigationTableOfContents { get; set; } = 50; //default to a high number so that omission gets penalized.

	[JsonPropertyName("navigation_section")]
	public string? NavigationSection { get; set; }

	/// The date of the batch update this document was part of last.
	/// This date could be higher than the date_last_updated.
	[JsonPropertyName("batch_index_date")]
	public DateTimeOffset BatchIndexDate { get; set; }

	/// The date this document was last updated,
	[JsonPropertyName("last_updated")]
	public DateTimeOffset LastUpdated { get; set; }

	[JsonPropertyName("description")]
	public string? Description { get; set; }

	[JsonPropertyName("headings")]
	public string[] Headings { get; set; } = [];

	[JsonPropertyName("links")]
	public string[] Links { get; set; } = [];

	[JsonPropertyName("applies_to")]
	public ApplicableTo? Applies { get; set; }

	[JsonPropertyName("body")]
	public string? Body { get; set; }

	/// Stripped body is the body with Markdown removed, suitable for search indexing
	[JsonPropertyName("stripped_body")]
	public string? StrippedBody { get; set; }

	[JsonPropertyName("abstract")]
	public string? Abstract { get; set; }

	[JsonPropertyName("parents")]
	public ParentDocument[] Parents { get; set; } = [];

	[JsonPropertyName("hidden")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public bool Hidden { get; set; }
}
