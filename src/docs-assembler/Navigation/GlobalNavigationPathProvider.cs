// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;
using System.IO.Abstractions;
using Documentation.Assembler.Extensions;
using Elastic.Documentation;
using Elastic.Markdown;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Extensions.DetectionRules;
using Elastic.Markdown.IO;

namespace Documentation.Assembler.Navigation;

public record GlobalNavigationPathProvider : IDocumentationFileOutputProvider
{
	private readonly AssembleSources _assembleSources;
	private readonly AssembleContext _context;

	public ImmutableSortedSet<string> TableOfContentsPrefixes { get; }
	private ImmutableSortedSet<string> PhantomPrefixes { get; }

	public GlobalNavigationPathProvider(GlobalNavigationFile navigationFile, AssembleSources assembleSources, AssembleContext context)
	{
		_assembleSources = assembleSources;
		_context = context;

		TableOfContentsPrefixes = [..assembleSources.TocTopLevelMappings
			.Values
			.Select(p =>
			{
				var source = p.Source.ToString();
				return source.EndsWith(":///") ? source[..^1] : source;
			})
			.OrderByDescending(v => v.Length)
		];

		PhantomPrefixes = [..navigationFile.Phantoms
			.Select(p =>
			{
				var source = p.Source.ToString();
				return source.EndsWith(":///") ? source[..^1] : source;
			})
			.OrderByDescending(v => v.Length)
			.ToArray()
		];
	}

	public IFileInfo? OutputFile(DocumentationSet documentationSet, IFileInfo defaultOutputFile, string relativePath)
	{

		if (relativePath.StartsWith("_static/", StringComparison.Ordinal))
			return defaultOutputFile;



		var repositoryName = documentationSet.Build.Git.RepositoryName;
		var outputDirectory = documentationSet.OutputDirectory;
		var fs = defaultOutputFile.FileSystem;

		if (repositoryName == "detection-rules")
		{
			var output = DetectionRuleFile.OutputPath(defaultOutputFile, documentationSet.Build);
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
		var relativePathSpan = relativePath.AsSpan();
		var newRelativePath = relativePathSpan.GetTrimmedRelativePath(originalPath);

		var path = fs.Path.Combine(outputDirectory.FullName, toc.SourcePathPrefix, newRelativePath);

		return fs.FileInfo.New(path);
	}
}
