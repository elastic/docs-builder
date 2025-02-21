// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.IO.Abstractions;

namespace Elastic.Markdown.IO;

public abstract record DocumentationFile(IFileInfo SourceFile, IDirectoryInfo RootPath)
{
	public string RelativePath { get; } = Path.GetRelativePath(RootPath.FullName, SourceFile.FullName);
	public string RelativeFolder { get; } = Path.GetRelativePath(RootPath.FullName, SourceFile.Directory!.FullName);

}

public record ImageFile(IFileInfo SourceFile, IDirectoryInfo RootPath, string MimeType = "image/png")
	: DocumentationFile(SourceFile, RootPath);

public record StaticFile(IFileInfo SourceFile, IDirectoryInfo RootPath)
	: DocumentationFile(SourceFile, RootPath);

public record ExcludedFile(IFileInfo SourceFile, IDirectoryInfo RootPath)
	: DocumentationFile(SourceFile, RootPath);

public record SnippetFile(IFileInfo SourceFile, IDirectoryInfo RootPath)
	: DocumentationFile(SourceFile, RootPath);
