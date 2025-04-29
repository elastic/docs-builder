// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.IO.Abstractions;
using Elastic.Markdown.Myst;
using Elastic.Markdown.Myst.FrontMatter;
using Elastic.Markdown.Slices;

namespace Elastic.Markdown.IO;

public abstract record DocumentationFile
{
	protected DocumentationFile(IFileInfo sourceFile, IDirectoryInfo rootPath, string repository)
	{
		RootPath = rootPath;
		Repository = repository;
		SourceFile = sourceFile;
		RelativePath = Path.GetRelativePath(RootPath.FullName, SourceFile.FullName);
		RelativeFolder = Path.GetRelativePath(RootPath.FullName, SourceFile.Directory!.FullName);
		CrossLink = $"{Repository}://{RelativePath.Replace('\\', '/')}";
	}

	public IDirectoryInfo RootPath { get; }
	public string RelativePath { get; }
	public string RelativeFolder { get; }
	public string CrossLink { get; }
	public string Repository { get; }

	/// Allows documentation files of non markdown origins to advertise as their markdown equivalent in links.json
	public virtual string LinkReferenceRelativePath => RelativePath;

	public IFileInfo SourceFile { get; }
}

public record ImageFile(IFileInfo SourceFile, IDirectoryInfo RootPath, string Repository, string MimeType = "image/png")
	: DocumentationFile(SourceFile, RootPath, Repository);

public record ExcludedFile(IFileInfo SourceFile, IDirectoryInfo RootPath, string Repository)
	: DocumentationFile(SourceFile, RootPath, Repository);

public record SnippetFile(IFileInfo SourceFile, IDirectoryInfo RootPath, string Repository)
	: DocumentationFile(SourceFile, RootPath, Repository)
{
	private SnippetAnchors? Anchors { get; set; }
	private bool _parsed;

	public SnippetAnchors? GetAnchors(
		DocumentationSet set,
		MarkdownParser parser,
		YamlFrontMatter? frontMatter
	)
	{
		if (_parsed)
			return Anchors;
		if (!SourceFile.Exists)
		{
			_parsed = true;
			return null;
		}

		var document = parser.MinimalParseAsync(SourceFile, default).GetAwaiter().GetResult();
		var toc = MarkdownFile.GetAnchors(set, parser, frontMatter, document, new Dictionary<string, string>(), out var anchors);
		Anchors = new SnippetAnchors(anchors, toc);
		_parsed = true;
		return Anchors;
	}
}

public record SnippetAnchors(string[] Anchors, IReadOnlyCollection<PageTocItem> TableOfContentItems);
