// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.Json.Serialization;
using IniParser;

namespace Elastic.Markdown.IO;

public record LinkReference
{
	[JsonPropertyName("origin")]
	public required GitConfiguration Origin { get; init; }

	[JsonPropertyName("url_path_prefix")]
	public required string? UrlPathPrefix { get; init; }

	[JsonPropertyName("links")]
	public required string[] Links { get; init; } = [];

	public static LinkReference Create(DocumentationSet set)
	{
		var links = set.FlatMappedFiles.Values
			.OfType<MarkdownFile>()
			.Select(m => m.RelativePath).ToArray();
		return new LinkReference
		{
			UrlPathPrefix = set.Context.UrlPathPrefix,
			Origin = set.Context.Git,
			Links = links
		};
	}
}
