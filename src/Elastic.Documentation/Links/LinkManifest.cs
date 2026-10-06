// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Documentation.Serialization;
using Generator.Equals;

namespace Elastic.Documentation.Links;

[Equatable]
public partial record LinkMetadata
{
	[JsonPropertyName("anchors")]
	[JsonInclude]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[OrderedEquality]
	public string[]? Anchors { get; internal init; } = [];

	[JsonPropertyName("hidden")]
	[JsonInclude]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public bool Hidden { get; internal init; }

	/// <summary>
	/// Anchors the page declared more than once (compared ignoring case), in the order met. <see cref="Anchors"/> is a set, so the duplicates
	/// are gone from it. Not part of the wire format.
	/// </summary>
	[JsonIgnore]
	[IgnoreEquality]
	public IReadOnlyList<string> DuplicateAnchors { get; internal init; } = [];
}

[Equatable]
public partial record LinkSingleRedirect
{
	[JsonPropertyName("anchors")]
	[JsonInclude]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[UnorderedEquality]
	public IReadOnlyDictionary<string, string?>? Anchors { get; internal init; }

	[JsonPropertyName("to")]
	[JsonInclude]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string? To { get; internal init; }
}

[Equatable]
public partial record LinkRedirect : LinkSingleRedirect
{
	public static IReadOnlyDictionary<string, string?> CatchAllAnchors { get; } = new Dictionary<string, string?> { { "!", "!" } };

	[JsonPropertyName("many")]
	[JsonInclude]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	[OrderedEquality]
	public LinkSingleRedirect[]? Many { get; internal init; }
}

/// <summary>
/// A repository's published <c>links.json</c>: every page and anchor it exposes to cross-links, its redirects, and the
/// cross-links it makes. The JSON keys are the wire format other repositories' builds read; they never change.
/// </summary>
[Equatable]
public partial record LinkManifest
{
	[JsonPropertyName("origin")]
	[JsonInclude]
	[JsonRequired]
	public GitCheckoutInformation Origin { get; internal init; } = default!;

	[JsonPropertyName("url_path_prefix")]
	[JsonInclude]
	[JsonRequired]
	public string? UrlPathPrefix { get; internal init; }

	/// Mapping of relative filepath and all the page's anchors for deep links
	[JsonPropertyName("links")]
	[JsonInclude]
	[JsonRequired]
	[UnorderedEquality]
	public Dictionary<string, LinkMetadata> Links { get; internal init; } = [];

	[JsonPropertyName("cross_links")]
	[JsonInclude]
	[JsonRequired]
	[OrderedEquality]
	public string[] CrossLinks { get; internal init; } = [];

	/// Mapping of relative filepath and all the page's anchors for deep links
	[JsonPropertyName("redirects")]
	[JsonInclude]
	[UnorderedEquality]
	public Dictionary<string, LinkRedirect>? Redirects { get; internal init; }
}
