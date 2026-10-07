// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Generator.Equals;

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// Matching mode for multi-valued fields (areas, labels).
/// </summary>
public enum MatchMode
{
	/// <summary>Match if ANY item matches the list.</summary>
	Any,

	/// <summary>Match only if ALL items match the list.</summary>
	All,

	/// <summary>Match if every configured item appears on the entry (logical AND over the list). See documentation.</summary>
	Conjunction
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
/// <remarks>
/// <para><b>Built by:</b> <c>ChangelogConfigurationLoader</c> in <c>Elastic.Documentation.Configuration</c> (<c>Load</c>, <c>LoadRequired</c>, <c>LoadForProfileMode</c>), reading <c>changelog.yml</c> (<c>ChangelogConfigurationLoader.LoadPublishBlocker</c> and the <c>ReleaseNotesSerialization</c> reader for the blocker of a rule).</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new PublishBlocker { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
[Equatable]
public partial record PublishBlocker
{
	/// <summary>
	/// Types to filter (either exclude or include based on <see cref="TypesMode"/>).
	/// </summary>
	[OrderedEquality]
	public IReadOnlyList<string>? Types { get; internal init; }

	/// <summary>
	/// Whether types uses exclude or include semantics.
	/// </summary>
	public FieldMode TypesMode { get; internal init; } = FieldMode.Exclude;

	/// <summary>
	/// Areas to filter (either exclude or include based on <see cref="AreasMode"/>).
	/// </summary>
	[OrderedEquality]
	public IReadOnlyList<string>? Areas { get; internal init; }

	/// <summary>
	/// Whether areas uses exclude or include semantics.
	/// </summary>
	public FieldMode AreasMode { get; internal init; } = FieldMode.Exclude;

	/// <summary>
	/// How to match areas: Any (default), All, or Conjunction.
	/// </summary>
	public MatchMode MatchAreas { get; internal init; } = MatchMode.Any;

	/// <summary>
	/// Returns true if this blocker has any blocking rules configured.
	/// </summary>
	public bool HasBlockingRules => (Types?.Count > 0) || (Areas?.Count > 0);
}
