// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Extensions;
using Elastic.Markdown.Myst;
using Markdig.Syntax;

namespace Elastic.Markdown.IO;

/// <summary>
/// A page read from outside the documentation set root, declared with <c>source:</c> on its table of contents entry.
/// Everything derived from position — URL, output path, link reference, relative links inside the page — anchors on
/// <see cref="VirtualFile"/>, the docset-relative <c>file:</c> path; only the content comes from
/// <see cref="DocumentationFile.SourceFile"/>.
/// </summary>
public record ExternallySourcedMarkdownFile : MarkdownFile
{
	public ExternallySourcedMarkdownFile(
		IFileInfo sourceFile,
		string virtualRelativePath,
		MarkdownParser parser,
		BuildContext build
	) : base(sourceFile, build.DocumentationSourceDirectory, parser, build, virtualRelativePath) =>
		VirtualFile = build.ReadFileSystem.NewFileInfo(build.DocumentationSourceDirectory.FullName, virtualRelativePath);

	/// <summary>The page's position inside the documentation set. Never exists on disk.</summary>
	public IFileInfo VirtualFile { get; }

	protected override Task<MarkdownDocument> GetMinimalParseDocumentAsync(Cancel ctx) =>
		MarkdownParser.MinimalParseAsync(VirtualFile, SourceFile, ctx);

	protected override Task<MarkdownDocument> GetParseDocumentAsync(Cancel ctx) =>
		MarkdownParser.ParseAsync(VirtualFile, SourceFile, YamlFrontMatter, ctx);
}
