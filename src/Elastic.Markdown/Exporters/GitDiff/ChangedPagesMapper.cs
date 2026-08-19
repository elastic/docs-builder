// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.GitDiff;

namespace Elastic.Markdown.Exporters.GitDiff;

internal static class ChangedPagesMapper
{
	private static readonly HashSet<string> ConfigFileNames = new(StringComparer.OrdinalIgnoreCase)
	[
		"docset.yml",
		"_docset.yml",
		"redirects.yml",
		"toc.yml",
		"navigation.yml",
		"navigation_preview.yml",
		"products.yml",
		"versions.yml",
		"legacy-url-mappings.yml",
		"assembler.yml",
		"search.yml",
	];

	public static ChangedPagesExport Map(
		string diffBase,
		string docsetPrefix,
		IReadOnlyDictionary<string, BuiltPageInfo> builtPages,
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> includeIndex,
		IReadOnlyList<SourceFileChange> changes
	)
	{
		var configChanged = false;
		var deleted = new List<DeletedPageEntry>();
		var pageEntries = new Dictionary<string, ChangedPageEntry>(StringComparer.OrdinalIgnoreCase);

		foreach (var change in changes)
		{
			if (!GitDiffPathNormalization.TryToDocsetRelative(change.Path, docsetPrefix, out var docsetPath))
				continue;

			if (IsConfigFile(docsetPath, change.Path))
			{
				configChanged = true;
				continue;
			}

			switch (change.ChangeType)
			{
				case SourceFileChangeType.Deleted:
					if (GitDiffPathNormalization.IsMarkdownPagePath(docsetPath))
						deleted.Add(new DeletedPageEntry { SourcePath = docsetPath });
					break;

				case SourceFileChangeType.Renamed:
					if (GitDiffPathNormalization.IsMarkdownPagePath(docsetPath))
						deleted.Add(new DeletedPageEntry { SourcePath = docsetPath });
					if (GitDiffPathNormalization.TryToDocsetRelative(change.NewPath ?? string.Empty, docsetPrefix, out var newDocsetPath))
					{
						TryAddDirectPage(pageEntries, builtPages, newDocsetPath, "renamed");
						TryAddAffectedByInclude(pageEntries, builtPages, includeIndex, newDocsetPath);
					}
					break;

				default:
					var changeLabel = change.ChangeType == SourceFileChangeType.Added ? "added" : "modified";
					TryAddDirectPage(pageEntries, builtPages, docsetPath, changeLabel);
					TryAddAffectedByInclude(pageEntries, builtPages, includeIndex, docsetPath);
					break;
			}
		}

		var pages = pageEntries.Values
			.OrderBy(p => p.SourcePath, StringComparer.OrdinalIgnoreCase)
			.ToArray();

		deleted.Sort(static (a, b) => string.Compare(a.SourcePath, b.SourcePath, StringComparison.OrdinalIgnoreCase));

		return new ChangedPagesExport
		{
			Base = diffBase,
			ConfigChanged = configChanged,
			Pages = pages,
			Deleted = deleted
		};
	}

	private static bool IsConfigFile(string docsetPath, string repoPath)
	{
		var fileName = Path.GetFileName(string.IsNullOrEmpty(docsetPath) ? repoPath : docsetPath);
		return ConfigFileNames.Contains(fileName);
	}

	private static void TryAddDirectPage(
		Dictionary<string, ChangedPageEntry> pageEntries,
		IReadOnlyDictionary<string, BuiltPageInfo> builtPages,
		string docsetPath,
		string change
	)
	{
		if (!GitDiffPathNormalization.IsMarkdownPagePath(docsetPath))
			return;

		if (!builtPages.TryGetValue(docsetPath, out var page))
			return;

		pageEntries[docsetPath] = new ChangedPageEntry
		{
			SourcePath = docsetPath,
			Url = page.Url,
			Title = page.Title,
			Change = change,
			IncludedFrom = []
		};
	}

	private static void TryAddAffectedByInclude(
		Dictionary<string, ChangedPageEntry> pageEntries,
		IReadOnlyDictionary<string, BuiltPageInfo> builtPages,
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> includeIndex,
		string changedDocsetPath
	)
	{
		if (!includeIndex.TryGetValue(changedDocsetPath, out var affectedPages))
			return;

		foreach (var pagePath in affectedPages)
		{
			if (!builtPages.TryGetValue(pagePath, out var page))
				continue;

			if (pageEntries.TryGetValue(pagePath, out var existing))
			{
				if (existing.IncludedFrom.Contains(changedDocsetPath))
					continue;

				pageEntries[pagePath] = existing with
				{
					IncludedFrom = [.. existing.IncludedFrom, changedDocsetPath]
				};
				continue;
			}

			pageEntries[pagePath] = new ChangedPageEntry
			{
				SourcePath = pagePath,
				Url = page.Url,
				Title = page.Title,
				Change = "modified",
				IncludedFrom = [changedDocsetPath]
			};
		}
	}
}
