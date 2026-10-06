// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Catalog;

/// <summary>Every rule a <see cref="VersioningSystemCatalog"/> must satisfy, in the order they are reported.</summary>
[Constraints]
public static partial class VersioningSystemConstraints
{
	public static Constraint<VersioningSystemCatalog> BaseNotAfterCurrent { get; } = Constraints<VersioningSystemCatalog>
		.Each(
			m => m.VersioningSystems.Values,
			v => !(v.Base > v.Current),
			v => $"Versioning system '{v.Id.ToStringFast(true)}' has base {v.Base}, which is after its current version {v.Current}."
		)
		.WithSeverity(Severity.Warning)
		.Describe("A versioning system's base version is not after its current version.");

	public static Constraint<VersioningSystemCatalog> VersioningSystemsComplete { get; } = Constraints<VersioningSystemCatalog>
		.References(
			_ => VersioningSystemIdExtensions.GetValues().Where(id => id != VersioningSystemId.None),
			id => Enumerable.Repeat(id, 1),
			m => m.VersioningSystems.Keys,
			describe: (_, id) => $"Versioning system '{id.ToStringFast(true)}' is not defined in versions.yml, so looking it up throws."
		)
		.WithSeverity(Severity.Warning)
		.Describe("G90: Every versioning system id has an entry in versions.yml; a missing one fails only when it is looked up.");

	public static Constraint<VersioningSystemCatalog> VersionlessBaseMatchesCurrent { get; } = Constraints<VersioningSystemCatalog>
		.Each(
			m => m.VersioningSystems.Values,
			v => !(v.IsVersionless && v.Base.Major < VersioningSystem.VersionlessSentinel),
			v =>
				$"Versioning system '{v.Id.ToStringFast(true)}' is versionless (current {v.Current}) but has a real base {v.Base}; a versionless system uses the {VersioningSystem.VersionlessSentinel} sentinel for both."
		)
		.WithSeverity(Severity.Hint)
		.Describe("G91: A versionless system (current major at or above the sentinel) also has the sentinel as its base.");
}
