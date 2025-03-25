// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;
using System.IO.Abstractions;
using Elastic.Markdown;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Configuration;

namespace Documentation.Assembler.Navigation;

public record GlobalNavigationPathProvider : IDocumentationFileOutputProvider
{
	private readonly AssembleSources _assembleSources;
	private readonly AssembleContext _context;

	private ImmutableSortedSet<string> TableOfContentsPrefixes { get; }
	private ImmutableSortedSet<string> PhantomPrefixes { get; }

	public GlobalNavigationPathProvider(GlobalNavigationFile navigationFile, AssembleSources assembleSources, AssembleContext context)
	{
		_assembleSources = assembleSources;
		_context = context;

		TableOfContentsPrefixes = [..assembleSources.TocTopLevelMappings
			.Values
			.Select(v => v.Source.ToString())
			.OrderByDescending(v => v.Length)
		];

		PhantomPrefixes = [..navigationFile.Phantoms
			.Select(p => p.Source.ToString())
			.OrderByDescending(v => v.Length)
			.ToArray()
		];
	}

	public IFileInfo? OutputFile(DocumentationSet documentationSet, IFileInfo defaultOutputFile, string relativePath)
	{
		if (relativePath.StartsWith("_static/", StringComparison.Ordinal))
			return defaultOutputFile;

		var outputDirectory = documentationSet.OutputDirectory;
		var fs = defaultOutputFile.FileSystem;

		var repositoryName = documentationSet.Build.Git.RepositoryName;

		var l = ContentSourceMoniker.CreateString(repositoryName, relativePath).TrimEnd('/');
		var lookup = l.AsSpan();
		if (lookup.StartsWith("docs-content://serverless/", StringComparison.Ordinal))
			return null;
		if (lookup.StartsWith("eland://sphinx/", StringComparison.Ordinal))
			return null;
		if (lookup.StartsWith("elasticsearch-py://sphinx/", StringComparison.Ordinal))
			return null;
		if (lookup.StartsWith("elastic-serverless-forwarder://", StringComparison.Ordinal) && lookup.EndsWith(".png"))
			return null;

		//allow files at root for `docs-content` (index.md 404.md)
		if (lookup.StartsWith("docs-content://") && !relativePath.Contains('/'))
			return defaultOutputFile;

		Uri? match = null;
		foreach (var prefix in TableOfContentsPrefixes)
		{
			if (!lookup.StartsWith(prefix, StringComparison.Ordinal))
				continue;
			match = new Uri(prefix);
			break;
		}

		if (match is null || !_assembleSources.TocTopLevelMappings.TryGetValue(match, out var toc))
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

			var fallBack = fs.Path.Combine(outputDirectory.FullName, "_failed", repositoryName, relativePath);
			_context.Collector.EmitError(_context.NavigationPath, $"No toc for output path: '{lookup}' falling back to: '{fallBack}'");
			return fs.FileInfo.New(fallBack);
		}

		var originalPath = Path.Combine(match.Host, match.AbsolutePath.Trim('/')).TrimStart('/');
		var newRelativePath = relativePath.AsSpan().TrimStart(originalPath).TrimStart('/').ToString();
		var path = fs.Path.Combine(outputDirectory.FullName, toc.SourcePathPrefix, newRelativePath);

		return fs.FileInfo.New(path);
	}
}
