// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Myst;
using Elastic.Markdown.Myst.FrontMatter;

namespace Elastic.Markdown.IO;

public abstract record DocumentationFile
{
	protected DocumentationFile(IFileInfo sourceFile, IDirectoryInfo rootPath, string repository, string? virtualRelativePath = null)
	{
		RootPath = rootPath;
		Repository = repository;
		SourceFile = sourceFile;
		RelativePath = virtualRelativePath ?? Path.GetRelativePath(RootPath.FullName, SourceFile.FullName);
		RelativeFolder = virtualRelativePath is null
			? Path.GetRelativePath(RootPath.FullName, SourceFile.Directory!.FullName)
			: Path.GetDirectoryName(virtualRelativePath) is { Length: > 0 } folder ? folder : ".";
		CrossLink = $"{Repository}://{RelativePath.Replace('\\', '/')}";
	}

	public IDirectoryInfo RootPath { get; }

	/// <summary>
	/// Position inside the documentation set — what drives URL, output path and link reference. Equals
	/// <see cref="SourceFile"/> relative to <see cref="RootPath"/> unless the page was sourced from outside the
	/// root via <c>source:</c>, in which case it is the virtual <c>file:</c> path the author declared.
	/// </summary>
	public string RelativePath { get; }
	public string RelativeFolder { get; }
	public string CrossLink { get; }
	public string Repository { get; }

	/// Allows documentation files of non markdown origins to advertise as their markdown equivalent in links.json
	public virtual string LinkReferenceRelativePath => RelativePath;

	public IFileInfo SourceFile { get; }
}

public record ImageFile(
	IFileInfo SourceFile,
	IDirectoryInfo RootPath,
	string Repository,
	string MimeType = "image/png"
) : DocumentationFile(SourceFile, RootPath, Repository);

public record ExcludedFile(IFileInfo SourceFile, IDirectoryInfo RootPath, string Repository) : DocumentationFile(
	SourceFile,
	RootPath,
	Repository
);

public record SnippetFile(IFileInfo SourceFile, IDirectoryInfo RootPath, string Repository) : DocumentationFile(
	SourceFile,
	RootPath,
	Repository
)
{
	private SnippetAnchors? Anchors { get; set; }
	private bool _parsed;

	public SnippetAnchors? GetAnchors(
		IDiagnosticsCollector collector,
		Func<string, DocumentationFile?> documentationFileLookup,
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
		var toc = MarkdownFile.GetAnchors(
			collector,
			documentationFileLookup,
			parser,
			frontMatter,
			document,
			new Dictionary<string, string>(),
			out var anchors
		);
		Anchors = new SnippetAnchors(anchors, toc);
		_parsed = true;
		return Anchors;
	}
}

public record SnippetAnchors(string[] Anchors, IReadOnlyCollection<PageTocItem> TableOfContentItems);
