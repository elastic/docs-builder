// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Reconciliation;

/// <summary>How a group's public manifest diverges from its public listing.</summary>
public enum RegistryDivergenceKind
{
	/// <summary>A public object (or the manifest itself) that the registry should describe but doesn't.</summary>
	Missing,

	/// <summary>A manifest entry (or the whole manifest) describing something no longer in the bucket, or metadata a reconcile would rewrite.</summary>
	Stale,

	/// <summary>The manifest exists but cannot be parsed.</summary>
	Corrupt,

	/// <summary>File present on both sides but the recorded ETag or target disagrees with the object.</summary>
	ObjectDivergent,

	/// <summary>The manifest declares a newer schema than this tool understands; reported distinctly, never rewritten.</summary>
	UnsupportedSchema
}

/// <summary>One verify finding for a group.</summary>
public sealed record RegistryDivergence
{
	/// <summary>The divergence family.</summary>
	public required RegistryDivergenceKind Kind { get; init; }

	/// <summary>The file inside the group's prefix (or <c>registry.json</c> for manifest-level findings).</summary>
	public required string File { get; init; }

	/// <summary>Human-readable specifics.</summary>
	public required string Detail { get; init; }
}
