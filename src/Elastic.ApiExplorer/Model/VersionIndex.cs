// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

// index.json at the root of the elastic-docs-openapi-specs bucket, keyed by "org/repo", then by spec
// file basename, then by version moniker ("main", "9", "8", ...).
global using RootVersionIndex = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, Elastic.ApiExplorer.Model.VersionIndexEntry>>>;

using System.Text.Json.Serialization;

namespace Elastic.ApiExplorer.Model;

/// <summary>One moniker's entry under an <c>org/repo</c> and spec file in a <see cref="RootVersionIndex"/>.</summary>
public sealed record VersionIndexEntry
{
	/// <summary>
	/// The branch segment used in the object key, e.g. <c>main</c>, <c>9.5</c>, or <c>8.19</c>.
	/// </summary>
	public required string Version { get; init; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(RootVersionIndex))]
[JsonSerializable(typeof(VersionIndexEntry))]
internal sealed partial class VersionIndexJsonContext : JsonSerializerContext;
