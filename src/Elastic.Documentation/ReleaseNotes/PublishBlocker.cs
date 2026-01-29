// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// Configuration for blocking changelog entries from publishing based on type or area.
/// </summary>
public record PublishBlocker
{
	/// <summary>
	/// Entry types to block from publishing (e.g., "deprecation", "known-issue").
	/// </summary>
	public IReadOnlyList<string>? Types { get; init; }

	/// <summary>
	/// Entry areas to block from publishing (e.g., "Internal", "Experimental").
	/// </summary>
	public IReadOnlyList<string>? Areas { get; init; }

	/// <summary>
	/// Returns true if this blocker has any blocking rules configured.
	/// </summary>
	public bool HasBlockingRules => (Types?.Count > 0) || (Areas?.Count > 0);
}
