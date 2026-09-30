// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Constraints;

namespace Elastic.Documentation.Codex;

/// <summary>Every rule a <see cref="CodexConfiguration"/> must satisfy, in the order they are reported.</summary>
public static class CodexConfigurationConstraints
{
	public static IReadOnlyList<Constraint<CodexConfiguration>> All { get; } =
	[
		new(
			"group-has-id",
			"Every codex group has an id.",
			m => m
				.Groups
				.Where(g => string.IsNullOrWhiteSpace(g.Id))
				.Select(g => Finding.Warning($"Codex group '{g.Name}' has no id. Docsets join a group by its id."))
		),
		new(
			"group-has-name",
			"Every codex group has a name.",
			m => m
				.Groups
				.Where(g => !string.IsNullOrWhiteSpace(g.Id) && string.IsNullOrWhiteSpace(g.Name))
				.Select(g => Finding.Warning($"Codex group '{g.Id}' has no name."))
		),
		new(
			"group-ids-unique",
			"No two codex groups share an id.",
			m => m
				.Groups
				.Where(g => !string.IsNullOrWhiteSpace(g.Id))
				.GroupBy(g => g.Id, StringComparer.OrdinalIgnoreCase)
				.Where(g => g.Count() > 1)
				.Select(g => Finding.Warning($"Codex group id '{g.Key}' is declared {g.Count()} times. Group ids must be unique."))
		)
	];

	public static IReadOnlyList<ConstraintViolation> Check(CodexConfiguration codex) => All.CheckAll(codex);
}
