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
		),
		new(
			"environment-uri-absolute",
			"Every environment's uri is an absolute URI.",
			m => m
				.Environments
				.Values
				.Where(e => !System.Uri.TryCreate(e.Uri, UriKind.Absolute, out _))
				.Select(e => Finding.Warning($"Environment '{e.Name}' has uri '{e.Uri}', which is not an absolute URI."))
		),
		new(
			"content-source-declared",
			"Every environment declares which content source it publishes.",
			m => m
				.Environments
				.Values
				.Where(e => e.DeclaredContentSource is null)
				.Select(
					e => Finding.Warning(
						$"Environment '{e.Name}' does not declare content_source, so it publishes 'next'. Set content_source to current, next or edge."
					)
				)
		),
		new(
			"optimizely-has-id",
			"An environment that enables Optimizely names its project id.",
			m => m
				.Environments
				.Values
				.Where(e => e.Optimizely.Enabled && string.IsNullOrWhiteSpace(e.Optimizely.Id))
				.Select(e => Finding.Warning($"Environment '{e.Name}' enables Optimizely without an id."))
		)
	];

	public static IReadOnlyList<ConstraintViolation> Check(AssemblyConfiguration assembly) => All.CheckAll(assembly);
}
