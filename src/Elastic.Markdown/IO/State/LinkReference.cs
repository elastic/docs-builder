// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Markdown.IO.Discovery;

namespace Elastic.Markdown.IO.State;

public record LinkMetadata
{
	[JsonPropertyName("anchors")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public required string[]? Anchors { get; init; } = [];

	[JsonPropertyName("hidden")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public required bool Hidden { get; init; }
}

public record LinkReference
{
	[JsonPropertyName("origin")]
	public required GitCheckoutInformation Origin { get; init; }

	[JsonPropertyName("url_path_prefix")]
	public required string? UrlPathPrefix { get; init; }

	/// Mapping of relative filepath and all the page's anchors for deeplinks
	[JsonPropertyName("links")]
	public required Dictionary<string, LinkMetadata> Links { get; init; } = [];

	[JsonPropertyName("cross_links")]
	public required string[] CrossLinks { get; init; } = [];

	public static LinkReference Create(DocumentationSet set)
	{
		var crossLinks = set.Context.Collector.CrossLinks.ToHashSet().ToArray();
		var links = set.MarkdownFiles.Values
			.Select(m => (m.RelativePath, File: m))
			.ToDictionary(k => k.RelativePath, v =>
			{
				var anchors = v.File.Anchors.Count == 0 ? null : v.File.Anchors.ToArray();
				return new LinkMetadata { Anchors = anchors, Hidden = v.File.Hidden };
			});
		return new LinkReference
		{
			UrlPathPrefix = set.Context.UrlPathPrefix,
			Origin = set.Context.Git,
			Links = links,
			CrossLinks = crossLinks
		};
	}
}
