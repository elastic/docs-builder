// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.Documentation.Links;

namespace Elastic.Documentation.Serialization;

/// <summary>Writes the <c>links.json</c> and <c>link-index.json</c> wire formats. Reading them is <c>LinkLoader</c>'s job.</summary>
public static class LinkJson
{
	public static string Serialize(LinkManifest manifest) =>
		JsonSerializer.Serialize(manifest, SourceGenerationContext.Default.LinkManifest);

	public static byte[] SerializeToUtf8Bytes(LinkManifest manifest) =>
		JsonSerializer.SerializeToUtf8Bytes(manifest, SourceGenerationContext.Default.LinkManifest);

	public static string SerializeRedirects(Dictionary<string, LinkRedirect>? redirects) =>
		JsonSerializer.Serialize(redirects, SourceGenerationContext.Default.DictionaryStringLinkRedirect);

	public static string Serialize(LinkRegistry registry) =>
		JsonSerializer.Serialize(registry, SourceGenerationContext.Default.LinkRegistry);
}
