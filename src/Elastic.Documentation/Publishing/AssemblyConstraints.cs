// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Constraints;

namespace Elastic.Documentation.Publishing;

/// <summary>Every rule an <see cref="AssemblyConfiguration"/> must satisfy, in the order they are reported.</summary>
public static class AssemblyConstraints
{
	public static IReadOnlyList<Constraint<AssemblyConfiguration>> All { get; } =
	[
		new(
			"narrative-name-reserved",
			"No referenced repository is named after the narrative repository.",
			m => m.ReferenceRepositories.ContainsKey(NarrativeRepository.RepositoryName)
				? [Finding.Error($"An item with the same key has already been added. Key: {NarrativeRepository.RepositoryName}")]
				: []
		),
		new(
			"google-tag-manager-has-id",
			"An environment that enables Google Tag Manager names its container id.",
			m => m
				.Environments
				.Values
				.Where(e => e.GoogleTagManager.Enabled && string.IsNullOrEmpty(e.GoogleTagManager.Id))
				.Select(_ => Finding.Error("Id is required when Enabled is true."))
		)
	];

	public static IReadOnlyList<ConstraintViolation> Check(AssemblyConfiguration assembly) => All.CheckAll(assembly);
}
