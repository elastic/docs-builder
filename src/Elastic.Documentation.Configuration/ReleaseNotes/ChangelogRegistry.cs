// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Documentation.Configuration.ReleaseNotes;

/// <summary>
/// Consumer-side view of the <c>bundle/{product}/registry.json</c> manifest (or the
/// <c>changelog/{repo}/registry.json</c> entry index) published alongside scrubbed changelog content.
/// Mirrors the producer's shape (see the changelog upload service) but is intentionally lenient: only
/// the fields the <c>changelog</c> directive needs to enumerate bundles are declared, and nothing is
/// <c>required</c> so a partially-written or future-versioned manifest still deserializes.
/// </summary>
public sealed record ChangelogRegistry
{
	/// <summary>Manifest schema version. A higher major than the consumer understands is rejected upstream.</summary>
	public int SchemaVersion { get; init; } = 1;

	/// <summary>Grouping identifier the manifest belongs to (the second S3 key segment: product for a bundle index, repo for a changelog-entry index).</summary>
	public string? Product { get; init; }

	/// <summary>Bundles known for this product, newest first as written by the producer.</summary>
	public IReadOnlyList<ChangelogRegistryBundle> Bundles { get; init; } = [];
}

/// <summary>One entry in <see cref="ChangelogRegistry.Bundles"/>.</summary>
public sealed record ChangelogRegistryBundle
{
	/// <summary>Bundle file name, resolved at <c>bundle/{product}/{file}</c> on the CDN (or entry file at <c>changelog/{repo}/{file}</c> for the entry index).</summary>
	public string? File { get; init; }

	/// <summary>Target version or release date declared by the bundle (e.g. <c>9.3.0</c>).</summary>
	public string? Target { get; init; }

	/// <summary>
	/// Best-effort change hint from the private bucket (pre-scrub). Not valid for public-object
	/// integrity or HTTP cache validation; see the producer's documentation. Unused by the directive
	/// today but retained for fidelity and future cache-keying.
	/// </summary>
	public string? ETag { get; init; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(ChangelogRegistry))]
[JsonSerializable(typeof(ChangelogRegistryBundle))]
internal sealed partial class ChangelogRegistryJsonContext : JsonSerializerContext;
