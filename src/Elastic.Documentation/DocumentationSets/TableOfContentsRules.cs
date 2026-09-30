// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Constraints;

namespace Elastic.Documentation.DocumentationSets;

/// <summary>The rules a resolved table of contents must satisfy, as checked by <see cref="DocumentationSetConstraints"/>.</summary>
public static class TableOfContentsRules
{
	/// <summary>Whether a listing glob may match files that are not Markdown; those need an <c>extension:</c> to handle them.</summary>
	public static bool GlobMayMatchNonMarkdown(string glob) =>
		!glob.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
			&& !glob.EndsWith("/*.md", StringComparison.OrdinalIgnoreCase)
			&& !glob.Equals("**/*.md", StringComparison.OrdinalIgnoreCase);

	public static string ListingGlobNeedsExtension(string listingPath, string glob) =>
		$"Listing '{listingPath}': glob '{glob}' may match non-.md files — 'extension:' is required to handle them.";

	/// <summary>Listings whose glob may match non-Markdown files but that name no extension to handle them.</summary>
	public static IEnumerable<Finding> ListingsNeedingExtension(DocumentationSetDefinition docset) =>
		docset
			.TableOfContents
			.Descendants()
			.OfCase<ListingRef>()
			.Where(l => GlobMayMatchNonMarkdown(l.Options.Glob ?? "**/*.md") && string.IsNullOrEmpty(l.Options.Extension))
			.Select(
				l => Finding.Error(ListingGlobNeedsExtension(l.PathRelativeToDocumentationSet, l.Options.Glob ?? "**/*.md")).In(l.Context)
			);

	/// <summary>Nested tables of contents whose <c>toc.yml</c> exists but lists nothing.</summary>
	public static IEnumerable<Finding> EmptyNestedTablesOfContents(DocumentationSetDefinition docset)
	{
		var baseDirectory = docset.SourceFile.Directory!;
		foreach (var toc in docset.TableOfContents.Descendants().OfCase<IsolatedTableOfContentsRef>())
		{
			if (toc.Children.Count > 0)
				continue;
			var tocDirectory = docset
				.FileSystem
				.DirectoryInfo
				.New(docset.FileSystem.Path.Join(baseDirectory.FullName, toc.PathRelativeToDocumentationSet));
			var tocFile = docset.FileSystem.Path.Join(tocDirectory.FullName, "toc.yml");
			// A missing toc.yml is reported by the loader, which could not read it.
			if (docset.FileSystem.File.Exists(tocFile))
				yield return Finding.Error($"Table of contents '{toc.PathRelativeToDocumentationSet}' has no children defined").In(tocFile);
		}
	}

	/// <summary>File entries whose path climbs out of the folder they are declared in with a <c>..</c> segment.</summary>
	public static IEnumerable<Finding> FilesLeavingTheirFolder(DocumentationSetDefinition docset) =>
		docset
			.TableOfContents
			.Descendants()
			.OfCase<FileRef>()
			.Where(f => f.PathRelativeToDocumentationSet.Value.Split('/', '\\').Contains(".."))
			.Select(
				f => Finding.Warning(
					$"File '{f.PathRelativeToDocumentationSet}' leaves the folder it is declared in with '..'. A file's children must be its siblings or deeper; move the entry to where the file lives."
				).In(f.Context)
			);

	/// <summary>CLI references whose supplemental docs folder does not exist.</summary>
	public static IEnumerable<Finding> MissingCliSupplementalFolders(DocumentationSetDefinition docset)
	{
		var baseDirectory = docset.SourceFile.Directory!;
		foreach (var cli in docset.TableOfContents.Descendants().OfCase<CliReferenceRef>())
		{
			if (cli.SupplementalFolder is not { } folder)
				continue;
			if (!docset.FileSystem.Directory.Exists(docset.FileSystem.Path.Join(baseDirectory.FullName, folder)))
				yield return Finding.Warning($"CLI supplemental docs folder not found: {folder}").In(cli.Context);
		}
	}
}
