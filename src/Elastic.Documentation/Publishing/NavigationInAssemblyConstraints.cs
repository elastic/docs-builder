// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Publishing;

/// <summary>The navigation together with the assembly whose repositories it mounts: the pair a rule over both is written against.</summary>
public sealed record NavigationInAssembly(SiteNavigationDefinition Navigation, AssemblyConfiguration Assembly);

/// <summary>Every rule that relates a <see cref="SiteNavigationDefinition"/> to the <see cref="AssemblyConfiguration"/> it is assembled from.</summary>
[Constraints]
public static partial class NavigationInAssemblyConstraints
{
	public static Constraint<NavigationInAssembly> MountRepositoryInAssembly { get; } = Constraints<NavigationInAssembly>.References(
		pair => SiteNavigationConstraints.AllMounts(pair.Navigation),
		toc => Enumerable.Repeat(toc.Source.Scheme, 1),
		pair => pair.Assembly.AvailableRepositories.Keys,
		StringComparer.OrdinalIgnoreCase,
		at: toc => toc.Position is { } p ? new SourceLocation(null, p.Line, p.Column) : null,
		describe: (toc, repository) =>
			$"{SiteNavigationConstraints.NormalizedSource(toc)} is mounted, but '{repository}' is not a repository the assembly publishes."
	).Describe("G62: Every mounted table of contents names a repository the assembly publishes, because it can only be built from one.");
}
