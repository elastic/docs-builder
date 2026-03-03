// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Documentation.Configuration.Changelog;

/// <summary>
/// Top-level rules configuration for controlling changelog creation and publishing.
/// </summary>
public record RulesConfiguration
{
	/// <summary>
	/// Global match mode for multi-valued fields. Inherited by create and publish sections.
	/// </summary>
	public MatchMode Match { get; init; } = MatchMode.Any;

	/// <summary>
	/// Rules controlling which PRs generate changelog entries.
	/// </summary>
	public CreateRules? Create { get; init; }

	/// <summary>
	/// Rules controlling which entries appear in rendered output.
	/// </summary>
	public PublishRules? Publish { get; init; }
}

/// <summary>
/// Rules for create-time blocking based on PR labels.
/// </summary>
public record CreateRules
{
	/// <summary>
	/// Labels to match (semantics depend on <see cref="Mode"/>).
	/// </summary>
	public IReadOnlyList<string>? Labels { get; init; }

	/// <summary>
	/// Whether labels use exclude or include semantics.
	/// </summary>
	public FieldMode Mode { get; init; } = FieldMode.Exclude;

	/// <summary>
	/// Match mode for labels (any or all). Inherited from RulesConfiguration.Match if not set.
	/// </summary>
	public MatchMode Match { get; init; } = MatchMode.Any;

	/// <summary>
	/// Per-product create rule overrides. Keys are product IDs.
	/// </summary>
	public IReadOnlyDictionary<string, CreateRules>? ByProduct { get; init; }
}

/// <summary>
/// Rules for publish-time blocking based on entry type and area.
/// </summary>
public record PublishRules
{
	/// <summary>
	/// Global publish blocker configuration.
	/// </summary>
	public PublishBlocker? Blocker { get; init; }

	/// <summary>
	/// Per-product publish blocker overrides. Keys are product IDs.
	/// </summary>
	public IReadOnlyDictionary<string, PublishBlocker>? ByProduct { get; init; }
}
