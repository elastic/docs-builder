// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Documentation.Serialization;
using Generator.Equals;

namespace Elastic.Documentation.Links;

/// <summary>One page of a <see cref="LinkManifest"/>: its anchors and whether it is hidden.</summary>
/// <remarks>
/// <para><b>Built by:</b> <c>LinkLoader.LoadManifest</c> in <c>Elastic.Documentation.Configuration</c>, which deserializes <c>links.json</c> and checks it once, or the pure <see cref="LinkManifest.From"/> and <see cref="LinkManifest.Empty"/>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new LinkMetadata { ... }</c>) and <c>with</c> for variations.</para>
/// <para><b>Serialised:</b> <c>[JsonInclude]</c> on the internal setters lets System.Text.Json reach them (it silently skips a setter it cannot reach).</para>
/// </remarks>
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

/// <summary>One redirect of a <see cref="LinkManifest"/>: where it points and which anchors it maps.</summary>
/// <remarks>
/// <para><b>Built by:</b> <c>LinkLoader.LoadManifest</c> in <c>Elastic.Documentation.Configuration</c>, which deserializes <c>links.json</c> and checks it once, or the pure <see cref="LinkManifest.From"/> and <see cref="LinkManifest.Empty"/>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new LinkSingleRedirect { ... }</c>) and <c>with</c> for variations.</para>
/// <para><b>Serialised:</b> <c>[JsonInclude]</c> on the internal setters lets System.Text.Json reach them (it silently skips a setter it cannot reach).</para>
/// </remarks>
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

/// <summary>A redirect of a <see cref="LinkManifest"/>, optionally a list of anchor-specific redirects.</summary>
/// <remarks>
/// <para><b>Built by:</b> <c>LinkLoader.LoadManifest</c> in <c>Elastic.Documentation.Configuration</c>, which deserializes <c>links.json</c> and checks it once, or the pure <see cref="LinkManifest.From"/> and <see cref="LinkManifest.Empty"/>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new LinkRedirect { ... }</c>) and <c>with</c> for variations.</para>
/// <para><b>Serialised:</b> <c>[JsonInclude]</c> on the internal setters lets System.Text.Json reach them (it silently skips a setter it cannot reach).</para>
/// </remarks>
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
/// <remarks>
/// <para><b>Built by:</b> <c>LinkLoader.LoadManifest</c> in <c>Elastic.Documentation.Configuration</c>, which deserializes <c>links.json</c> and checks it once, or the pure <see cref="LinkManifest.From"/> and <see cref="LinkManifest.Empty"/>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>Members that must always be set</b> (they were <c>required</c> before the type was sealed; C# forbids <c>required</c> on a member whose setter is less visible than the type, CS9032): <c>Origin</c>, <c>UrlPathPrefix</c>, <c>Links</c>, <c>CrossLinks</c>. A loader or test that leaves one out gets the default; the aggregate's constraint set (<see cref="LinkManifestConstraints"/>) reports it: <see cref="LinkManifestConstraints.LinkManifestOriginNamed"/> reports an origin that names no repository.</para>
/// <para><b>In a test:</b> build it with <c>LinkExamples.Manifest</c> (tests), an object initializer (<c>new LinkManifest { Origin = ..., UrlPathPrefix = ..., Links = ..., CrossLinks = ... }</c>) or <c>DomainGen.Of&lt;LinkManifest&gt;()</c> (tests), and <c>with</c> for variations.</para>
/// <para><b>Serialised:</b> <c>[JsonInclude]</c> on the internal setters and <c>[JsonRequired]</c> stand in for the lost <c>required</c>: the serializer cannot reach an internal setter without the first, and a missing key is still a parse error with the second.</para>
/// </remarks>
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
