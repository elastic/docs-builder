// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;
using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Markdown;
using Elastic.Markdown.Extensions.DetectionRules;
using Elastic.Markdown.IO;

namespace Elastic.Documentation.Assembler.Navigation;

public record GlobalNavigationPathProvider : IDocumentationFileOutputProvider
{
	private readonly AssembleSources _assembleSources;
	private readonly AssembleContext _context;

	public ImmutableSortedSet<string> TableOfContentsPrefixes { get; }
	private ImmutableSortedSet<string> PhantomPrefixes { get; }

	public GlobalNavigationPathProvider(SiteNavigation navigation, AssembleSources assembleSources, AssembleContext context)
	{
		_assembleSources = assembleSources;
		_context = context;

		TableOfContentsPrefixes =
		[
			.. assembleSources
				.NavigationTocMappings
				.Values
				.Select(p =>
				{
					var source = p.Source.ToString();
					return source.EndsWith(":///", StringComparison.OrdinalIgnoreCase) ? source[..^1] : source;
				})
				.OrderByDescending(v => v.Length)
		];

		PhantomPrefixes =
		[
			.. navigation
				.Phantoms
				.Select(p =>
				{
					var source = p.Source.ToString();
					return source.EndsWith(":///", StringComparison.OrdinalIgnoreCase) ? source[..^1] : source;
				})
				.OrderByDescending(v => v.Length)
				.ToArray()
		];
	}

	public IFileInfo? OutputFile(DocumentationSet documentationSet, IFileInfo defaultOutputFile, string relativePath)
	{
		if (relativePath.StartsWith("_static/", StringComparison.Ordinal))
			return defaultOutputFile;

		var repositoryName = documentationSet.Context.Git.RepositoryName;
		var outputDirectory = documentationSet.OutputDirectory;
		var fs = defaultOutputFile.FileSystem;

		if (repositoryName == "detection-rules")
		{
			var output = DetectionRuleFile.OutputPath(defaultOutputFile, documentationSet.Context);
			var md = fs.FileInfo.New(Path.ChangeExtension(output.FullName, "md"));
			relativePath = Path.GetRelativePath(documentationSet.OutputDirectory.FullName, md.FullName);
		}

		var l = ContentSourceMoniker.CreateString(repositoryName, relativePath).TrimEnd('/');
		var lookup = l.AsSpan();
		//TODO clean up docs folders in the following repositories
		if (lookup.StartsWith("cloud://saas/", StringComparison.Ordinal))
			return null;
		if (lookup.StartsWith("docs-content://serverless/", StringComparison.Ordinal))
			return null;
		if (lookup.StartsWith("eland://sphinx/", StringComparison.Ordinal))
			return null;
		if (lookup.StartsWith("elasticsearch-py://sphinx/", StringComparison.Ordinal))
			return null;

		//allow files at root for `docs-content` (index.md 404.md)
		if (lookup.StartsWith("docs-content://") && !relativePath.Contains('/'))
			return defaultOutputFile;

		if (
			!TryGetMountedRelativePath(
				repositoryName,
				relativePath,
				_assembleSources.NavigationTocMappings,
				out var sourcePathPrefix,
				out var remainder
			)
		)
		{
			if (relativePath.StartsWith("raw-migrated-files/", StringComparison.Ordinal))
				return null;
			if (relativePath.StartsWith("images/", StringComparison.Ordinal))
				return null;
			if (relativePath.StartsWith("examples/", StringComparison.Ordinal))
				return null;
			if (relativePath.StartsWith("docset.yml", StringComparison.Ordinal))
				return null;
			if (relativePath.StartsWith("doc_examples", StringComparison.Ordinal))
				return null;
			if (relativePath.EndsWith(".asciidoc", StringComparison.Ordinal))
				return null;

			foreach (var prefix in PhantomPrefixes)
			{
				if (lookup.StartsWith(prefix, StringComparison.Ordinal))
					return null;
			}

			// A JSON file at the docset root has no toc entry. Put it on the docset's published mount.
			if (IsDocsetRootJson(relativePath))
			{
				var root = DocsetRootPrefix(repositoryName, _assembleSources.NavigationTocMappings.Values);
				if (root is not null)
					return fs.FileInfo.New(fs.Path.Join(outputDirectory.FullName, root, relativePath));
			}

			var fallBack = fs.Path.Join(outputDirectory.FullName, "_failed", repositoryName, relativePath);
			_context.Collector.EmitError(
				_context.ConfigurationFileProvider.NavigationFile,
				$"No toc for output path: '{lookup}' falling back to: '{fallBack}'"
			);
			return fs.FileInfo.New(fallBack);
		}

		var path = fs.Path.Join(outputDirectory.FullName, sourcePathPrefix, remainder);
		return fs.FileInfo.New(path);
	}

	/// <summary>
	/// Maps a docset file onto its navigation mount. A file under <c>autoops-insights://elasticsearch</c>
	/// lands at <c>reference/autoops-insights/elasticsearch</c>, not next to the shared output root.
	/// </summary>
	internal static bool TryGetMountedRelativePath(
		string repositoryName,
		string relativePath,
		IReadOnlyDictionary<Uri, NavigationTocMapping> mappings,
		out string sourcePathPrefix,
		out string remainder
	)
	{
		var lookup = ContentSourceMoniker.CreateString(repositoryName, relativePath).TrimEnd('/');
		string? bestPrefix = null;
		foreach (var mapping in mappings.Values)
		{
			var source = mapping.Source.ToString();
			var prefix = source.EndsWith(":///", StringComparison.OrdinalIgnoreCase) ? source[..^1] : source;
			if (!lookup.StartsWith(prefix, StringComparison.Ordinal))
				continue;
			if (bestPrefix is null || prefix.Length > bestPrefix.Length)
				bestPrefix = prefix;
		}

		if (bestPrefix is null || !Uri.TryCreate(bestPrefix, UriKind.Absolute, out var match) || !mappings.TryGetValue(match, out var toc))
		{
			sourcePathPrefix = "";
			remainder = "";
			return false;
		}

		var originalPath = Path.Join(match.Host, match.AbsolutePath.Trim('/')).TrimStart('/');
		remainder = relativePath.AsSpan().GetTrimmedRelativePath(originalPath);
		sourcePathPrefix = toc.SourcePathPrefix;
		return true;
	}

	/// <summary>
	/// Published directory for a JSON file that sits at the docset root, such as <c>autoops-insights.json</c>.
	/// Uses the navigation parent of this repository's mounts.
	/// </summary>
	internal static string? DocsetRootPrefix(string repositoryName, IEnumerable<NavigationTocMapping> mappings)
	{
		var mappingList = mappings as IReadOnlyCollection<NavigationTocMapping> ?? mappings.ToArray();
		var repoPrefixes = mappingList
			.Where(mapping => string.Equals(mapping.Source.Scheme, repositoryName, StringComparison.OrdinalIgnoreCase))
			.Select(mapping => mapping.SourcePathPrefix.Replace('\\', '/').Trim('/'))
			.Where(prefix => prefix.Length > 0)
			.Distinct(StringComparer.Ordinal)
			.ToArray();
		if (repoPrefixes.Length == 0)
			return null;

		foreach (var candidate in mappingList
			.Select(mapping => mapping.SourcePathPrefix.Replace('\\', '/').Trim('/'))
			.Distinct()
			.OrderByDescending(prefix => prefix.Length))
		{
			if (candidate.Length == 0)
				continue;
			if (repoPrefixes.All(prefix => prefix.StartsWith(candidate + "/", StringComparison.Ordinal)))
				return candidate;
		}

		var common = repoPrefixes[0];
		foreach (var prefix in repoPrefixes.Skip(1))
		{
			while (common.Length > 0 && prefix != common && !prefix.StartsWith(common + "/", StringComparison.Ordinal))
			{
				var slash = common.LastIndexOf('/');
				common = slash < 0 ? "" : common[..slash];
			}
		}

		return common.Length == 0 ? null : common;
	}

	private static bool IsDocsetRootJson(string relativePath) =>
		relativePath.EndsWith(".json", StringComparison.Ordinal) && !relativePath.Contains('/') && !relativePath.Contains('\\');
}
