// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Collections.Frozen;
using Elastic.Markdown.Myst.Directives.CsvInclude;
using Elastic.Markdown.Myst.Directives.Include;
using Markdig.Syntax;

namespace Elastic.Markdown.IO;

/// <summary>
/// Reverse-dependency lookup over <c>{{include}}</c> and <c>{{csv-include}}</c> directives
/// across a <see cref="DocumentationSet"/>. Resolves "which pages would re-render if this
/// snippet/CSV changes?" — used by the preview workflow to surface meaningful preview URLs
/// when only non-<c>.md</c> files (or files under <c>_snippets/</c>) are edited.
/// </summary>
public sealed class IncludeGraph
{
	private readonly FrozenDictionary<string, FrozenSet<string>> _consumersByTarget;
	private readonly FrozenSet<string> _snippetPaths;

	private IncludeGraph(
		FrozenDictionary<string, FrozenSet<string>> consumersByTarget,
		FrozenSet<string> snippetPaths)
	{
		_consumersByTarget = consumersByTarget;
		_snippetPaths = snippetPaths;
	}

	public static async Task<IncludeGraph> BuildAsync(DocumentationSet set, Cancel ctx)
	{
		var edges = new ConcurrentBag<(string Target, string Consumer)>();

		// Pages live in DocumentationSet.MarkdownFiles; snippets are stored separately in
		// DocumentationSet.Files as SnippetFile records. Both can host {{include}} directives,
		// so we parse both to capture page→snippet AND snippet→snippet edges.
		var pages = set.MarkdownFiles.Cast<DocumentationFile>();
		var snippets = set.Files.Values.OfType<SnippetFile>().Cast<DocumentationFile>();

		await Parallel.ForEachAsync(pages.Concat(snippets), ctx, async (file, token) =>
		{
			var document = file switch
			{
				MarkdownFile md => await md.MinimalParseAsync(set.TryFindDocumentByRelativePath, token),
				SnippetFile sn => await set.MarkdownParser.MinimalParseAsync(sn.SourceFile, token),
				_ => null
			};
			if (document is null)
				return;

			var consumer = NormalizePath(file.RelativePath);

			foreach (var include in document.Descendants<IncludeBlock>())
			{
				if (include is { Found: true, IncludePathRelativeToSource: { } target })
					edges.Add((NormalizePath(target), consumer));
			}

			foreach (var csv in document.Descendants<CsvIncludeBlock>())
			{
				if (csv is { Found: true, CsvFilePathRelativeToSource: { } target })
					edges.Add((NormalizePath(target), consumer));
			}
		}).ConfigureAwait(false);

		var consumersByTarget = edges
			.GroupBy(e => e.Target, StringComparer.OrdinalIgnoreCase)
			.ToFrozenDictionary(
				g => g.Key,
				g => g.Select(e => e.Consumer).ToFrozenSet(StringComparer.OrdinalIgnoreCase),
				StringComparer.OrdinalIgnoreCase);

		var snippetPaths = set.Files.Values
			.OfType<SnippetFile>()
			.Select(f => NormalizePath(f.RelativePath))
			.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

		return new IncludeGraph(consumersByTarget, snippetPaths);
	}

	/// <summary>
	/// Walks consumers of <paramref name="targetRelativePath"/> upward through any intermediate
	/// snippet-includes-snippet hops and returns the set of *page-level* dependents (non-snippet
	/// markdown files). The input itself is not included in the result.
	/// </summary>
	public IReadOnlySet<string> ResolvePageDependents(string targetRelativePath)
	{
		var pages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var queue = new Queue<string>();
		queue.Enqueue(NormalizePath(targetRelativePath));

		while (queue.Count > 0)
		{
			var current = queue.Dequeue();
			if (!_consumersByTarget.TryGetValue(current, out var consumers))
				continue;

			foreach (var consumer in consumers)
			{
				if (!seen.Add(consumer))
					continue;
				if (_snippetPaths.Contains(consumer))
					queue.Enqueue(consumer);
				else
					_ = pages.Add(consumer);
			}
		}

		return pages;
	}

	/// <summary>
	/// Returns true if the graph has at least one consumer recorded for the given target.
	/// Lets callers distinguish "snippet exists but is unused" from "snippet path unknown".
	/// </summary>
	public bool HasConsumers(string targetRelativePath) =>
		_consumersByTarget.ContainsKey(NormalizePath(targetRelativePath));

	private static string NormalizePath(string path) => path.Replace('\\', '/');
}
