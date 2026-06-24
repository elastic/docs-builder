// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Changelog.Uploading;

/// <summary>
/// Per-product manifest published alongside scrubbed changelog bundles.
/// Lets consumers (e.g. the <c>changelog</c> directive in <c>cdn:</c> mode) enumerate
/// bundle files without an S3 listing call.
/// </summary>
/// <remarks>
/// Stored at <c>bundle/{product}/registry.json</c> (bundle index) or
/// <c>changelog/{repo}/registry.json</c> (changelog-entry index) in the changelog bundles bucket.
/// The scrubber Lambda mirrors it verbatim to the public bucket (pass-through).
/// </remarks>
public sealed record Registry
{
	/// <summary>
	/// Manifest schema version. Incremented when consumers must change their parser.
	/// </summary>
	public int SchemaVersion { get; init; } = 1;

	/// <summary>
	/// Grouping identifier: the product for a bundle index (<c>bundle/{product}/…</c>) or the
	/// authoring repo for a changelog-entry index (<c>changelog/{repo}/…</c>) — i.e. the second
	/// path segment of the S3 key. Informational only; consumers locate the manifest by key.
	/// </summary>
	public required string Product { get; init; }

	/// <summary>
	/// Time the manifest was last regenerated, in UTC.
	/// </summary>
	public required DateTimeOffset GeneratedAt { get; init; }

	/// <summary>
	/// Bundles currently known for this product, sorted by <see cref="RegistryBundle.Target"/>
	/// descending (newest first), with a deterministic tiebreak on <see cref="RegistryBundle.File"/>.
	/// </summary>
	public required IReadOnlyList<RegistryBundle> Bundles { get; init; }
}

/// <summary>
/// One entry in <see cref="Registry.Bundles"/>.
/// </summary>
public sealed record RegistryBundle
{
	/// <summary>
	/// Bundle file name (e.g. <c>9.3.0.yaml</c> or <c>cloud-2025-11.yaml</c>),
	/// resolved at <c>bundle/{product}/{file}</c>. For changelog-entry indexes this is the entry
	/// file name resolved at <c>changelog/{repo}/{file}</c>.
	/// </summary>
	public required string File { get; init; }

	/// <summary>
	/// Target version or release date as declared in the bundle's first product
	/// (e.g. <c>9.3.0</c> or <c>2025-11-01</c>). May be null if the bundle declares no products.
	/// </summary>
	public string? Target { get; init; }

	/// <summary>
	/// S3 ETag of the bundle object as uploaded to the <em>private</em> bundles bucket (pre-scrub).
	/// For single-part uploads smaller than
	/// <see cref="Elastic.Documentation.Integrations.S3.S3EtagCalculator.PartSize"/> this is the MD5 of the body.
	/// </summary>
	/// <remarks>
	/// Best-effort identity / change hint only. The public (CDN) object is produced by the changelog
	/// scrubber Lambda, which rewrites any bundle that contains private references — so for scrubbed
	/// bundles this value will <em>not</em> match the public object's ETag. Consumers MUST NOT use it
	/// for integrity checks or HTTP cache validation against the public bucket; use the CDN response's
	/// own ETag for that. It is safe to use to detect whether a bundle changed between manifest reads.
	/// </remarks>
	public required string ETag { get; init; }
}

[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(Registry))]
[JsonSerializable(typeof(RegistryBundle))]
public sealed partial class RegistryJsonContext : JsonSerializerContext;
