// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration.TableOfContents;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;

namespace Elastic.Markdown.Extensions;

public interface IDocsBuilderExtension
{
	IDocumentationFileExporter? FileExporter { get; }

	/// Visit the <paramref name="tocItem"/> and its equivalent <see cref="DocumentationFile"/>
	void Visit(DocumentationFile file, ITocItem tocItem);

	/// Create an instance of <see cref="DocumentationFile"/> if it matches the <paramref name="file"/>.
	/// Return `null` to let another extension handle this.
	DocumentationFile? CreateDocumentationFile(IFileInfo file, DocumentationSet documentationSet);

	/// Attempts to locate a documentation file by slug, used to locate the document for `docs-builder serve` command
	bool TryGetDocumentationFileBySlug(DocumentationSet documentationSet, string slug, out DocumentationFile? documentationFile);

	/// Allows the extension to discover more documentation files for <see cref="DocumentationSet"/>
	IReadOnlyCollection<DocumentationFile> ScanDocumentationFiles(Func<IFileInfo, IDirectoryInfo, DocumentationFile> defaultFileHandling);

	MarkdownFile? CreateMarkdownFile(IFileInfo file, IDirectoryInfo sourceDirectory, DocumentationSet documentationSet);
}
