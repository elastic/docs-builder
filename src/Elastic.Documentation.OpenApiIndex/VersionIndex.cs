// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

// index.json at the root of the elastic-docs-openapi-specs bucket, keyed by "org/repo", then by spec
// file name, then by "main" or a major version number. SortedDictionary at every level so that a
// rebuild which changes nothing serializes to a byte-identical file.
global using RootVersionIndex = System.Collections.Generic.SortedDictionary<string, System.Collections.Generic.SortedDictionary<string, System.Collections.Generic.SortedDictionary<string, Elastic.Documentation.OpenApiIndex.VersionIndexEntry>>>;

using System.Text.Json.Serialization;

namespace Elastic.Documentation.OpenApiIndex;

/// <summary>One entry in a <see cref="RootVersionIndex"/>.</summary>
public sealed record VersionIndexEntry
{
	/// <summary>The version this key resolves to: <c>main</c>, or the highest minor of its major, e.g. <c>9.5</c>.</summary>
	public required string Version { get; init; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(RootVersionIndex))]
[JsonSerializable(typeof(VersionIndexEntry))]
public sealed partial class VersionIndexJsonContext : JsonSerializerContext;
