// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.Documentation.Links;

namespace Elastic.Documentation.Serialization;

/// <summary>
/// Reads and writes the <c>links.json</c> and <c>link-index.json</c> wire formats. The raw readers are internal (visible to
/// <c>Elastic.Documentation.Configuration</c> only): the way to get a record from text is <c>LinkLoader</c>, which checks it. Writing is public.
/// </summary>
public static class LinkJson
{
	internal static LinkManifest? DeserializeManifest(Stream json) =>
		JsonSerializer.Deserialize(json, SourceGenerationContext.Default.LinkManifest);

	internal static LinkManifest? DeserializeManifest(string json) =>
		JsonSerializer.Deserialize(json, SourceGenerationContext.Default.LinkManifest);

	public static string Serialize(LinkManifest manifest) =>
		JsonSerializer.Serialize(manifest, SourceGenerationContext.Default.LinkManifest);

	public static string SerializeRedirects(Dictionary<string, LinkRedirect>? redirects) =>
		JsonSerializer.Serialize(redirects, SourceGenerationContext.Default.DictionaryStringLinkRedirect);

	internal static LinkRegistry? DeserializeRegistry(Stream json) =>
		JsonSerializer.Deserialize(json, SourceGenerationContext.Default.LinkRegistry);

	internal static LinkRegistry? DeserializeRegistry(string json) =>
		JsonSerializer.Deserialize(json, SourceGenerationContext.Default.LinkRegistry);

	public static string Serialize(LinkRegistry registry) =>
		JsonSerializer.Serialize(registry, SourceGenerationContext.Default.LinkRegistry);
}
