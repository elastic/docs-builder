// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.DocumentationSets;

/// <summary>
/// What the table of contents loader needs to decide a listing, shared with <see cref="DocumentationSetConstraints"/>.
/// The rules themselves are declared there, over the model.
/// </summary>
public static class TableOfContentsRules
{
	/// <summary>Whether a listing glob may match files that are not Markdown; those need an <c>extension:</c> to handle them.</summary>
	public static bool GlobMayMatchNonMarkdown(string glob) =>
		!glob.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
			&& !glob.EndsWith("/*.md", StringComparison.OrdinalIgnoreCase)
			&& !glob.Equals("**/*.md", StringComparison.OrdinalIgnoreCase);

	/// <summary>The message for a listing the loader drops before it reaches the model; a listing that is kept is reported by <c>listing-glob-has-extension</c> with the same text.</summary>
	public static string ListingGlobNeedsExtension(string listingPath, string glob) =>
		$"Listing '{listingPath}': glob '{glob}' may match non-.md files — 'extension:' is required to handle them.";
}
