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
/// <remarks>
/// <para><b>Built by:</b> <c>VersionIndexLoader.Load</c> / <c>LoadAsync</c> (a version index read from JSON) and <c>VersionIndexBuilder.Build</c> (the publisher), both in <c>Elastic.Documentation.OpenApiIndex</c>, checked through <c>VersionIndexLoader.Check</c>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation.OpenApiIndex</c> and its test project (<c>Elastic.Documentation.OpenApiIndex.Tests</c>, through <c>InternalsVisibleTo</c>) can set them (the domain's grant does not reach this assembly), so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>Members that must always be set</b> (they were <c>required</c> before the type was sealed; C# forbids <c>required</c> on a member whose setter is less visible than the type, CS9032): <c>Version</c>. A loader or test that leaves one out gets the default; the aggregate's constraint set (<c>VersionIndexConstraints</c>) is where a missing member is reported, and the loader's mapping is what normally sets them.</para>
/// <para><b>In a test:</b> build it with an object initializer (<c>new VersionIndexEntry { Version = ... }</c>), and <c>with</c> for variations; <c>VersionIndexProperties</c> (tests) generates indexes.</para>
/// <para><b>Serialised:</b> <c>[JsonInclude]</c> on the internal setters and <c>[JsonRequired]</c> stand in for the lost <c>required</c>: the serializer cannot reach an internal setter without the first, and a missing key is still a parse error with the second.</para>
/// </remarks>
public sealed record VersionIndexEntry
{
	/// <summary>The version this key resolves to: <c>main</c>, or the highest minor of its major, e.g. <c>9.5</c>.</summary>
	[JsonInclude]
	[JsonRequired]
	public string Version { get; internal init; } = default!;
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(RootVersionIndex))]
[JsonSerializable(typeof(VersionIndexEntry))]
public sealed partial class VersionIndexJsonContext : JsonSerializerContext;
