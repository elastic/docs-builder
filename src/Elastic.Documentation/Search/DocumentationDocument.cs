// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Search;

public record ParentDocument
{
	[JsonPropertyName("title")]
	public string? Title { get; set; }

	[JsonPropertyName("url")]
	public string? Url { get; set; }
}

public record DocumentationDocument
{
	// TODO make this required once all doc_sets have published again
	[JsonPropertyName("url")]
	public string Url { get; set; } = string.Empty;

	/// The date of the batch update this document was part of last.
	/// This date could be higher than the date_last_updated.
	[JsonPropertyName("batch_index_date")]
	public DateTimeOffset BatchIndexDate { get; set; }

	/// The date this document was last updated,
	[JsonPropertyName("last_updated")]
	public DateTimeOffset LastUpdated { get; set; }

	// TODO make this required once all doc_sets have published again
	[JsonPropertyName("hash")]
	public string Hash { get; set; } = string.Empty;

	[JsonPropertyName("title")]
	public string? Title { get; set; }

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

	// Stripped body is the body with Markdown removed, suitable for search indexing
	[JsonPropertyName("stripped_body")]
	public string? StrippedBody { get; set; }

	[JsonPropertyName("url_segment_count")]
	public int? UrlSegmentCount { get; set; }

	[JsonPropertyName("abstract")]
	public string? Abstract { get; set; }

	[JsonPropertyName("parents")]
	public ParentDocument[] Parents { get; set; } = [];
}
