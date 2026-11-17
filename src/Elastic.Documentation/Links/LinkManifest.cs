// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Generator.Equals;
using Nullean.Entities;

namespace Elastic.Documentation.Links;

[Equatable]
public partial record LinkMetadata
{
	[JsonPropertyName("anchors")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[OrderedEquality]
	public string[]? Anchors { get; init; } = [];

	[JsonPropertyName("hidden")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public bool Hidden { get; init; }
}

[Equatable]
public partial record LinkSingleRedirect
{
	[JsonPropertyName("anchors")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[UnorderedEquality]
	public IReadOnlyDictionary<string, string?>? Anchors { get; init; }

	[JsonPropertyName("to")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string? To { get; init; }
}

[Equatable]
public partial record LinkRedirect : LinkSingleRedirect
{
	public static IReadOnlyDictionary<string, string?> CatchAllAnchors { get; } = new Dictionary<string, string?> { { "!", "!" } };

	[JsonPropertyName("many")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	[OrderedEquality]
	public LinkSingleRedirect[]? Many { get; init; }
}

/// <summary>
/// A repository's published <c>links.json</c>: every page and anchor it exposes to cross-links, its redirects, and the cross-links it makes.
/// The JSON keys are the wire format other repositories' builds read; they never change.
/// </summary>
[Equatable]
public sealed partial record LinkManifest : IHaveConstraints<LinkManifest>
{
	public static ConstraintSet<LinkManifest> Constraints => LinkManifestConstraints.All;

	[JsonPropertyName("origin")]
	public required GitCheckoutInformation Origin { get; init; }

	[JsonPropertyName("url_path_prefix")]
	public required string? UrlPathPrefix { get; init; }

	/// Mapping of relative filepath and all the page's anchors for deep links
	[JsonPropertyName("links")]
	[UnorderedEquality]
	public required Dictionary<string, LinkMetadata> Links { get; init; } = [];

	[JsonPropertyName("cross_links")]
	[OrderedEquality]
	public required string[] CrossLinks { get; init; } = [];

	/// Mapping of relative filepath and all the page's anchors for deep links
	[JsonPropertyName("redirects")]
	[UnorderedEquality]
	public Dictionary<string, LinkRedirect>? Redirects { get; init; }

	/// <summary>
	/// The pages of floating sections, such as APIs, keyed by <c>{kind}/{key}</c> (<c>api/elasticsearch</c>).
	/// They are not Markdown files, so they are not in <see cref="Links"/>.
	/// </summary>
	[JsonPropertyName("floating")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[UnorderedEquality]
	public Dictionary<string, FloatingLinkSection>? Floating { get; init; }
}
