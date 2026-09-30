// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Constraints;

namespace Elastic.Documentation.Catalog;

/// <summary>Every rule a <see cref="VersioningSystemCatalog"/> must satisfy, in the order they are reported.</summary>
public static class VersioningSystemConstraints
{
	public static IReadOnlyList<Constraint<VersioningSystemCatalog>> All { get; } =
	[
		new(
			"base-not-after-current",
			"A versioning system's base version is not after its current version.",
			m => m
				.VersioningSystems
				.Values
				.Where(v => v.Base > v.Current)
				.Select(
					v => Finding.Warning(
						$"Versioning system '{v.Id.ToStringFast(true)}' has base {v.Base}, which is after its current version {v.Current}."
					)
				)
		)
	];

	public static IReadOnlyList<ConstraintViolation> Check(VersioningSystemCatalog catalog) => All.CheckAll(catalog);
}
