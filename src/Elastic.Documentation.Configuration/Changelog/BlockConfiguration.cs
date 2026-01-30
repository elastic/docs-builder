// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration.Changelog;

/// <summary>
/// Combined block configuration for create and publish blockers
/// </summary>
public record BlockConfiguration
{
	/// <summary>
	/// Global labels that block changelog creation
	/// </summary>
	public IReadOnlyList<string>? Create { get; init; }

	/// <summary>
	/// Global configuration for blocking changelog entries from publishing based on type or area
	/// </summary>
	public PublishBlocker? Publish { get; init; }

	/// <summary>
	/// Per-product block overrides (overrides global blockers, does not merge)
	/// Keys are product IDs
	/// </summary>
	public IReadOnlyDictionary<string, ProductBlockers>? ByProduct { get; init; }
}

/// <summary>
/// Product-specific blockers
/// </summary>
public record ProductBlockers
{
	/// <summary>
	/// Labels that block creation for this product (overrides global create blockers)
	/// </summary>
	public IReadOnlyList<string>? Create { get; init; }

	/// <summary>
	/// Configuration for blocking changelog entries from publishing based on type or area
	/// </summary>
	public PublishBlocker? Publish { get; init; }
}

/// <summary>
/// Configuration for blocking changelog entries from publishing based on type or area
/// </summary>
public record PublishBlocker
{
	/// <summary>
	/// Entry types to block from publishing (e.g., "deprecation", "known-issue")
	/// </summary>
	public IReadOnlyList<string>? Types { get; init; }

	/// <summary>
	/// Entry areas to block from publishing (e.g., "Internal", "Experimental")
	/// </summary>
	public IReadOnlyList<string>? Areas { get; init; }

	/// <summary>
	/// Returns true if this blocker has any blocking rules configured
	/// </summary>
	public bool HasBlockingRules => (Types?.Count > 0) || (Areas?.Count > 0);
}
