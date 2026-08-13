// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Navigation;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst;

namespace Elastic.Markdown.Extensions.Listing;

/// <summary>
/// Auto-enabled when the TOC contains a <c>listing:</c> entry.
/// Generates synthetic <c>index.md</c> pages for listing roots and groups that don't have a
/// real file on disk, and augments real index pages by appending the <c>{listing}</c> directive.
/// </summary>
public class ListingDocsBuilderExtension(BuildContext build, MarkdownParser markdownParser) : IDocsBuilderExtension
{
	private BuildContext Build { get; } = build;
	private MarkdownParser MarkdownParser { get; } = markdownParser;

	public IDocumentationFileExporter? FileExporter => null;

	// Absolute paths of all listing root / group index pages (synthetic + real), populated on first use.
	private HashSet<string>? _listingIndexPaths;
	// Absolute paths of synthetic (non-existing) index files we need to register.
	private List<IFileInfo>? _syntheticIndexFiles;

	private void EnsureInitialized()
	{
		if (_listingIndexPaths is not null)
			return;

		_listingIndexPaths = [];
		_syntheticIndexFiles = [];

		CollectListingRefs(Build.ConfigurationYaml.TableOfContents);
	}

	private void CollectListingRefs(IReadOnlyCollection<ITableOfContentsItem> items)
	{
		foreach (var item in items)
		{
			if (item is ListingRef listingRef)
				RegisterListingRef(listingRef);

			var children = item switch
			{
				FileRef f => f.Children,
				FolderRef f => f.Children,
				IsolatedTableOfContentsRef t => t.Children,
				ListingRef lr => lr.Children,
				_ => null
			};
			if (children is { Count: > 0 })
				CollectListingRefs(children);
		}
	}

	private void RegisterListingRef(ListingRef listingRef)
	{
		var rootDir = Build.ReadFileSystem.Path.Join(
			Build.DocumentationSourceDirectory.FullName,
			listingRef.PathRelativeToDocumentationSet);

		// Root index
		RegisterIndexPath(Build.ReadFileSystem.Path.Join(rootDir, "index.md"));

		// Group index pages
		foreach (var child in listingRef.Children)
		{
			if (child is not ListingGroupRef groupRef)
				continue;

			// Find the IndexFileRef inside the group children (the first child of type IndexFileRef)
			var groupIndex = groupRef.Children.OfType<IndexFileRef>().FirstOrDefault();
			if (groupIndex is null)
			{
				// Synthesize <rootDir>/<groupKey>/index.md
				var groupIndexPath = Build.ReadFileSystem.Path.Join(
					Build.DocumentationSourceDirectory.FullName,
					listingRef.PathRelativeToDocumentationSet,
					groupRef.GroupKey,
					"index.md");
				RegisterIndexPath(groupIndexPath);
			}
			else
			{
				RegisterIndexPath(Build.ReadFileSystem.Path.Join(
					Build.DocumentationSourceDirectory.FullName,
					groupIndex.PathRelativeToDocumentationSet));
			}
		}
	}

	private void RegisterIndexPath(string absolutePath)
	{
		var normalized = Path.GetFullPath(absolutePath);
		_ = _listingIndexPaths!.Add(normalized);

		var fileInfo = Build.ReadFileSystem.FileInfo.New(normalized);
		if (!fileInfo.Exists)
			_syntheticIndexFiles!.Add(fileInfo);
	}

	public DocumentationFile? CreateDocumentationFile(IFileInfo file, MarkdownParser markdownParser)
	{
		EnsureInitialized();
		var normalized = Path.GetFullPath(file.FullName);
		if (!_listingIndexPaths!.Contains(normalized))
			return null;
		// Let CreateMarkdownFile handle this; CreateDocumentationFile is for non-.md files.
		return null;
	}

	public MarkdownFile? CreateMarkdownFile(IFileInfo file, IDirectoryInfo sourceDirectory, MarkdownParser markdownParser)
	{
		EnsureInitialized();
		var normalized = Path.GetFullPath(file.FullName);
		if (!_listingIndexPaths!.Contains(normalized))
			return null;
		// Both real and synthetic listing index pages get the listing appended
		return new ListingIndexFile(file, sourceDirectory, markdownParser, Build);
	}

	public bool TryGetDocumentationFileBySlug(DocumentationSet documentationSet, string slug, out DocumentationFile? documentationFile)
	{
		documentationFile = null;
		return false;
	}

	public IReadOnlyCollection<(IFileInfo, DocumentationFile)> ScanDocumentationFiles(
		Func<IFileInfo, IDirectoryInfo, DocumentationFile> defaultFileHandling)
	{
		EnsureInitialized();
		if (_syntheticIndexFiles is not { Count: > 0 })
			return [];

		var results = new List<(IFileInfo, DocumentationFile)>();
		foreach (var fileInfo in _syntheticIndexFiles)
		{
			// Skip if it was intercepted during the main scan (real file that existed)
			if (fileInfo.Exists)
				continue;
			// Synthetic listing index pages must be ListingIndexFile, not ExcludedFile.
			// defaultFileHandling returns ExcludedFile for .md files it doesn't recognise,
			// which then gets filtered from Files and causes navigation lookup failures.
			var doc = new ListingIndexFile(fileInfo, Build.DocumentationSourceDirectory, MarkdownParser, Build);
			results.Add((fileInfo, doc));
		}
		return results;
	}

	public void VisitNavigation(INavigationItem navigation, IDocumentationFile model) { }
}
