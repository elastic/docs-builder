// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Documentation.Search.Contract;

/// <summary>Single breadcrumb entry — the title + URL of an ancestor page.</summary>
public record ParentDocument
{
	[JsonPropertyName("title")]
	public required string Title { get; set; }

	[JsonPropertyName("url")]
	public required string Url { get; set; }
}
