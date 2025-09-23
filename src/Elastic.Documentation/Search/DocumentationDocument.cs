// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Documentation.AppliesTo;

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
	[JsonPropertyName("title")]
	public string? Title { get; set; }

	[JsonPropertyName("description")]
	public string? Description { get; set; }

	[JsonPropertyName("headings")]
	public string[] Headings { get; set; } = [];

	[JsonPropertyName("links")]
	public string[] Links { get; set; } = [];

	[JsonPropertyName("url")]
	public string? Url { get; set; }

	[JsonPropertyName("applies_to")]
	public ApplicableTo? Applies { get; set; }

	[JsonPropertyName("body")]
	public string? Body { get; set; }

	[JsonPropertyName("url_segment_count")]
	public int? UrlSegmentCount { get; set; }

	[JsonPropertyName("abstract")]
	public string? Abstract { get; set; }

	[JsonPropertyName("parents")]
	public ParentDocument[] Parents { get; set; } = [];
}
