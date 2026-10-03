// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Constraints;

namespace Elastic.Documentation.Publishing;

/// <summary>Every rule a <see cref="SiteNavigationDefinition"/> must satisfy, in the order they are reported.</summary>
public static class SiteNavigationConstraints
{
	public static IReadOnlyList<Constraint<SiteNavigationDefinition>> All { get; } =
	[
		new(
			"mount-has-path-prefix",
			"Every mounted table of contents has a path_prefix, except a bare 'toc: name', which is published under its name.",
			MountsWithoutPathPrefix
		),
		new("path-prefixes-unique", "No two repositories publish under the same path prefix.", DuplicatePathPrefixes),
		new(
			"path-prefix-is-a-url-path",
			"Every path_prefix is a legal URL path, so it can be compared and published.",
			PathPrefixesThatAreNotUrlPaths
		),
		new(
			"repository-path-prefixes-unique",
			"No repository mounts two tables of contents under the same path prefix.",
			RepeatedRepositoryPathPrefixes
		)
	];

	public static IReadOnlyList<ConstraintViolation> Check(SiteNavigationDefinition navigation) => All.CheckAll(navigation);

	/// <summary>A mount's source as its published URI: normalized to end in <c>/</c>, with a bare name expanded to the narrative.</summary>
	public static string NormalizedSource(SiteTableOfContentsRef toc)
	{
		var source = toc.ShorthandPath is { } shorthand
			? ContentSourceMoniker.CreateString(NarrativeRepository.RepositoryName, shorthand)
			: toc.Source.OriginalString;
		return source.EndsWith("://", StringComparison.OrdinalIgnoreCase) ? source : source.TrimEnd('/') + "/";
	}

	/// <summary>Where a mount publishes: its path_prefix, a bare name, or null when it has neither.</summary>
	public static string? DeclaredPathPrefix(SiteTableOfContentsRef toc) =>
		!string.IsNullOrEmpty(toc.PathPrefix) ? toc.PathPrefix : toc.ShorthandPath;

	private static IEnumerable<Finding> MountsWithoutPathPrefix(SiteNavigationDefinition navigation)
	{
		foreach (var toc in AllMounts(navigation))
		{
			if (!string.IsNullOrEmpty(DeclaredPathPrefix(toc)))
				continue;
			var source = NormalizedSource(toc);
			if (!Uri.TryCreate(source, UriKind.Absolute, out var sourceUri))
				continue;
			var sourcePrefix = $"{sourceUri.Host}/{sourceUri.AbsolutePath.TrimStart('/')}";
			yield return Finding.Error(
				$"Path prefix is not defined for: {source}, falling back to {sourcePrefix} which may be incorrect",
				toc.Position?.Line,
				toc.Position?.Column
			) with
			{ Length = toc.Position is { } p ? p.Column - p.EndColumn : null };
		}
	}

	// DuplicatePathPrefixes compares prefixes as URIs and skips one that cannot be built; this is the rule that says so.
	private static IEnumerable<Finding> PathPrefixesThatAreNotUrlPaths(SiteNavigationDefinition navigation) =>
		AllMounts(navigation)
			.Where(
				toc => !string.IsNullOrEmpty(toc.PathPrefix) &&
					!SiteNavigationDefinition.TryCreateUri($"{toc.Source.Scheme}://{toc.PathPrefix.TrimEnd('/')}/", out _)
			)
			.Select(
				toc => Finding.Error(
					$"Path prefix '{toc.PathPrefix}' of {NormalizedSource(toc)} is not a legal URL path.",
					toc.Position?.Line,
					toc.Position?.Column
				)
			);

	private static IEnumerable<Finding> DuplicatePathPrefixes(SiteNavigationDefinition navigation)
	{
		var sourcePathPrefixes = SiteNavigationDefinition.GetAllPathPrefixes(navigation);
		var pathPrefixSet = new HashSet<string>();
		foreach (var pathPrefix in sourcePathPrefixes)
		{
			var prefix = $"{pathPrefix.Host}/{pathPrefix.AbsolutePath.Trim('/')}/";
			if (pathPrefixSet.Add(prefix))
				continue;

			var duplicateOf = sourcePathPrefixes.First(p => p.Host == pathPrefix.Host && p.AbsolutePath == pathPrefix.AbsolutePath);
			yield return Finding.Error($"Duplicate path prefix: {pathPrefix} duplicate: {duplicateOf}");
		}
	}

	// DuplicatePathPrefixes compares prefixes across repositories only: the same repository's repeats collapse in its set.
	private static IEnumerable<Finding> RepeatedRepositoryPathPrefixes(SiteNavigationDefinition navigation) =>
		AllMounts(navigation)
			.Where(toc => !string.IsNullOrEmpty(toc.PathPrefix))
			.GroupBy(
				toc => (Repository: toc.Source.Scheme, Prefix: toc.PathPrefix.Trim('/')),
				(key, mounts) => (key, Mounts: mounts.ToList())
			)
			.Where(g => g.Mounts.Count > 1)
			.Select(
				g => Finding.Warning(
					$"Path prefix '{g.key.Prefix}' is mounted {g.Mounts.Count} times by {g.key.Repository}: {string.Join(", ", g.Mounts.Select(NormalizedSource))}",
					g.Mounts[1].Position?.Line,
					g.Mounts[1].Position?.Column
				)
			);

	/// <summary>Every mount, depth-first in declaration order, including those under sections.</summary>
	public static IEnumerable<SiteTableOfContentsRef> AllMounts(SiteNavigationDefinition navigation)
	{
		foreach (var entry in navigation.TableOfContents)
		{
			if (entry.Value is SiteTableOfContentsRef toc)
			{
				foreach (var mount in WithChildren(toc))
					yield return mount;
			}
			else
			{
				foreach (var child in entry.Children)
					foreach (var mount in WithChildren(child))
						yield return mount;
			}
		}

		static IEnumerable<SiteTableOfContentsRef> WithChildren(SiteTableOfContentsRef toc) =>
			[toc, .. toc.Children.SelectMany(WithChildren)];
	}
}
