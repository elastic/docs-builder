// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Documentation.Search;

public record DocumentationDocument
{
	[JsonPropertyName("title")]
	public string? Title { get; set; }

	[JsonPropertyName("body")]
	public string? Body { get; set; }

	[JsonPropertyName("description")]
	public string? Description { get; set; }

	[JsonPropertyName("headings")]
	public string[] Headings { get; set; } = [];

	[JsonPropertyName("links")]
	public string[] Links { get; set; } = [];

	[JsonPropertyName("url")]
	public string? Url { get; set; }
}
