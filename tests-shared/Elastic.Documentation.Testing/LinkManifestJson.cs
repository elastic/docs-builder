// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Links;

namespace Elastic.Documentation.Testing;

/// <summary>Reads a <c>links.json</c> or <c>link-index.json</c> text the way a build does, with the exception it always threw for text that does not parse.</summary>
public static class LinkManifestJson
{
	public static LinkManifest Read(string json) => LinkLoader.ReadManifest(json, "links.json");

	public static LinkRegistry ReadRegistry(string json) => LinkLoader.ReadRegistry(json, "link-index.json");
}
