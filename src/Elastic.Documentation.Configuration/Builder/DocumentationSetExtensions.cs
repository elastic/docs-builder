// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.CompilerServices;
using DotNet.Globbing;
using Elastic.Documentation.Configuration.Suggestions;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Configuration.Builder;

/// <summary>Build-time behavior over a <see cref="ResolvedDocumentationSet"/> that needs loader-side libraries (globbing, suggestions).</summary>
public static class DocumentationSetExtensions
{
	private static readonly ConditionalWeakTable<DocumentationSetExclusions, Glob[]> CompiledExcludes = [];

	/// <summary>Whether the build skips <paramref name="relativePath"/>: a <c>!</c> override wins, then folder-level excludes, then the globs.</summary>
	public static bool IsExcluded(this ResolvedDocumentationSet docset, string relativePath)
	{
		var exclusions = docset.Exclusions;
		if (
			exclusions.IncludeOverrides.Count > 0
			&& exclusions.IncludeOverrides.Any(i => i.Equals(relativePath.OptionalWindowsReplace(), StringComparison.OrdinalIgnoreCase))
		)
			return false;
		if (exclusions.FolderExcludedFiles.Contains(relativePath.OptionalWindowsReplace()))
			return true;
		var globs = CompiledExcludes.GetValue(exclusions, e => [.. e.ExcludeGlobs.Select(Glob.Parse)]);
		return globs.Any(g => g.IsMatch(relativePath));
	}

	/// <summary>
	/// Resolves the right-gutter CTA for a page. An explicit, known <c>cta</c> frontmatter <paramref name="id"/>
	/// always wins. Otherwise the template registered via <c>default_cta</c> on the page's navigation file
	/// applies, falling back to <see cref="Cta.DefaultName"/>.
	/// </summary>
	/// <param name="id">The page's <c>cta.id</c> frontmatter value, if any.</param>
	/// <param name="relativePath">The page's docset-root-relative source path, used for toc default lookup.</param>
	/// <param name="warning">Set when <paramref name="id"/> is unknown, so the caller can report it.</param>
	public static Cta ResolveCta(this ResolvedDocumentationSet docset, string? id, string? relativePath, out string? warning)
	{
		warning = null;
		if (id is not null)
		{
			if (docset.Ctas.TryGetValue(id, out var selected))
				return selected;
			// Unknown id: warn, then resolve as if the page had no `cta` frontmatter.
			warning = UnknownCtaWarning(id, docset.Ctas.Keys);
		}
		if (relativePath is { Length: > 0 })
		{
			var normalizedPath = DocumentationSetFile.NormalizeDocsetRelativePath(relativePath);
			if (
				docset.TocDefaultCtas.TryGetValue(normalizedPath, out var tocDefault) && docset.Ctas.TryGetValue(tocDefault, out var scoped)
			)
				return scoped;
		}
		return docset.Ctas[Cta.DefaultName];
	}

	private static string UnknownCtaWarning(string ctaName, IEnumerable<string> knownCtaNames)
	{
		var known = knownCtaNames.ToHashSet();
		var hint = new Suggestion(known, ctaName).GetSuggestionQuestion();
		if (string.IsNullOrEmpty(hint))
		{
			hint = known.Count > 1
				? $"Available: {string.Join(", ", known.Order())}."
				: "No 'cta' templates are defined in this docset.yml yet. Add one under a top-level 'cta:' map, e.g.:\n"
					+ "cta:\n  mp:\n    button:\n      label: Get started on MP\n      url: https://example.com\n    benefits:\n      - \"Some benefit\"";
		}
		return $"'cta: {ctaName}' does not match any 'cta' template in docset.yml and is ignored. {hint}";
	}
}
