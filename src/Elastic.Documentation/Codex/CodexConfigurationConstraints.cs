// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Codex;

/// <summary>Every rule a <see cref="CodexConfiguration"/> must satisfy, in the order they are reported.</summary>
[Constraints]
public static partial class CodexConfigurationConstraints
{
	public static Constraint<CodexConfiguration> GroupHasId { get; } = Constraints<CodexConfiguration>
		.Each(
			m => m.Groups,
			g => !string.IsNullOrWhiteSpace(g.Id),
			g => $"Codex group '{g.Name}' has no id. Docsets join a group by its id."
		)
		.WithSeverity(Severity.Warning)
		.Describe("Every codex group has an id.");

	public static Constraint<CodexConfiguration> GroupHasName { get; } = Constraints<CodexConfiguration>
		.Each(
			m => m.Groups,
			g => string.IsNullOrWhiteSpace(g.Id) || !string.IsNullOrWhiteSpace(g.Name),
			g => $"Codex group '{g.Id}' has no name."
		)
		.WithSeverity(Severity.Warning)
		.Describe("Every codex group has a name.");

	public static Constraint<CodexConfiguration> GroupIdsUnique { get; } = Constraints<CodexConfiguration>
		.Unique(
			m => m.Groups.Where(g => !string.IsNullOrWhiteSpace(g.Id)),
			g => g.Id,
			StringComparer.OrdinalIgnoreCase,
			describe: (id, count) => $"Codex group id '{id}' is declared {count} times. Group ids must be unique."
		)
		.WithSeverity(Severity.Warning)
		.Describe("No two codex groups share an id.");

	public static Constraint<CodexConfiguration> CodexCanonicalBaseUrlAbsolute { get; } = Constraints<CodexConfiguration>
		.Always(
			c => c.CanonicalBaseUrl,
			u => u is null || Uri.TryCreate(u, UriKind.Absolute, out _),
			"canonical_base_url is not an absolute URI, so the build drops it."
		)
		.WithSeverity(Severity.Warning)
		.Describe("G107: A codex's canonical_base_url, when set, is an absolute URI; an invalid one is silently dropped.");
}

/// <summary>
/// What a <see cref="CodexConfiguration"/> must additionally satisfy to be published. Reading it (to update redirects, say) needs none of this,
/// so the rules live apart from <see cref="CodexConfigurationConstraints"/> and the loader's purpose picks the set.
/// </summary>
[Constraints]
public static partial class CodexPublishingConstraints
{
	public static Constraint<CodexConfiguration> EnvironmentRequired { get; } = Constraints<CodexConfiguration>.Always(
		c => c.Environment,
		e => !string.IsNullOrWhiteSpace(e),
		"Codex configuration must specify an 'environment' (e.g., 'internal', 'security')."
	).Describe("G104: A codex that is published names its environment, which is its Elasticsearch index namespace.");
}
