// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Publishing;

/// <summary>Every rule a <see cref="SiteNavigationDefinition"/> must satisfy, in the order they are reported.</summary>
[Constraints<SiteNavigationDefinition>]
public static partial class SiteNavigationConstraints
{
	public static readonly Constraint<SiteNavigationDefinition> MountHasPathPrefix = Constraints<SiteNavigationDefinition>.Each(
		AllMounts,
		MountWithoutPathPrefix,
		toc => toc.Position is { } p ? new SourceLocation(null, p.Line, p.Column, p.Column - p.EndColumn) : null
	).Describe("Every mounted table of contents has a path_prefix, except a bare 'toc: name', which is published under its name.");

	public static readonly Constraint<SiteNavigationDefinition> PathPrefixesUnique = Constraints<SiteNavigationDefinition>.Check(
		DuplicatePathPrefixes
	).Describe("No two repositories publish under the same path prefix.");

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

	/// <summary>A mount's source as its published URI: normalized to end in <c>/</c>, with a bare name expanded to the narrative.</summary>
	public static string NormalizedSource(SiteTableOfContentsRef toc)
	{
		var source = toc.ShorthandPath is { } shorthand
			? ContentSourceMoniker.CreateString(NarrativeRepository.RepositoryName, shorthand)
			: toc.Source.OriginalString;
		return source.EndsWith("://", StringComparison.OrdinalIgnoreCase) ? source : source.TrimEnd('/') + "/";
	}

	private static string? MountWithoutPathPrefix(SiteTableOfContentsRef toc)
	{
		if (!string.IsNullOrEmpty(toc.DeclaredPathPrefix))
			return null;
		var source = NormalizedSource(toc);
		if (!Uri.TryCreate(source, UriKind.Absolute, out var sourceUri))
			return null;
		var sourcePrefix = $"{sourceUri.Host}/{sourceUri.AbsolutePath.TrimStart('/')}";
		return $"Path prefix is not defined for: {source}, falling back to {sourcePrefix} which may be incorrect";
	}

	// One error per prefix that an earlier one already claimed, in the order the prefix set yields them.
	private static IEnumerable<Violation> DuplicatePathPrefixes(SiteNavigationDefinition navigation)
	{
		var sourcePathPrefixes = SiteNavigationDefinition.GetAllPathPrefixes(navigation);
		var seen = new HashSet<string>();
		foreach (var pathPrefix in sourcePathPrefixes)
		{
			if (seen.Add($"{pathPrefix.Host}/{pathPrefix.AbsolutePath.Trim('/')}/"))
				continue;

			var duplicateOf = sourcePathPrefixes.First(p => p.Host == pathPrefix.Host && p.AbsolutePath == pathPrefix.AbsolutePath);
			yield return Violation.Error($"Duplicate path prefix: {pathPrefix} duplicate: {duplicateOf}");
		}
	}
}
