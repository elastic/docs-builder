// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// Matching mode for multi-valued fields (areas, labels).
/// </summary>
public enum MatchMode
{
	/// <summary>Match if ANY item matches the list.</summary>
	Any,

	/// <summary>Match only if ALL items match the list.</summary>
	All
}

/// <summary>
/// Whether a field uses exclude or include semantics.
/// </summary>
public enum FieldMode
{
	/// <summary>Block entries that match the list.</summary>
	Exclude,

	/// <summary>Only allow entries that match the list.</summary>
	Include
}

/// <summary>
/// Configuration for blocking changelog entries from publishing based on type or area.
/// Supports both exclude (block if matches) and include (block if doesn't match) modes.
/// </summary>
public record PublishBlocker
{
	/// <summary>
	/// Types to filter (either exclude or include based on <see cref="TypesMode"/>).
	/// </summary>
	public IReadOnlyList<string>? Types { get; init; }

	/// <summary>
	/// Whether types uses exclude or include semantics.
	/// </summary>
	public FieldMode TypesMode { get; init; } = FieldMode.Exclude;

	/// <summary>
	/// Areas to filter (either exclude or include based on <see cref="AreasMode"/>).
	/// </summary>
	public IReadOnlyList<string>? Areas { get; init; }

	/// <summary>
	/// Whether areas uses exclude or include semantics.
	/// </summary>
	public FieldMode AreasMode { get; init; } = FieldMode.Exclude;

	/// <summary>
	/// How to match areas: Any (default) or All.
	/// </summary>
	public MatchMode MatchAreas { get; init; } = MatchMode.Any;

	/// <summary>
	/// Returns true if this blocker has any blocking rules configured.
	/// </summary>
	public bool HasBlockingRules => (Types?.Count > 0) || (Areas?.Count > 0);
}
