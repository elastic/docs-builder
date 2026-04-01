// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Navigation;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst;

namespace Elastic.Markdown.Extensions;

public interface IDocsBuilderExtension
{
	IDocumentationFileExporter? FileExporter { get; }

	/// Create an instance of <see cref="DocumentationFile"/> if it matches the <paramref name="file"/>.
	/// Return `null` to let another extension handle this.
	DocumentationFile? CreateDocumentationFile(IFileInfo file, MarkdownParser markdownParser);

	/// Attempts to locate a documentation file by slug, used to locate the document for `docs-builder serve` command
	bool TryGetDocumentationFileBySlug(DocumentationSet documentationSet, string slug, out DocumentationFile? documentationFile);

	/// Allows the extension to discover more documentation files for <see cref="DocumentationSet"/>
	IReadOnlyCollection<(IFileInfo, DocumentationFile)> ScanDocumentationFiles(Func<IFileInfo, IDirectoryInfo, DocumentationFile> defaultFileHandling);

	MarkdownFile? CreateMarkdownFile(IFileInfo file, IDirectoryInfo sourceDirectory, MarkdownParser markdownParser);

	void VisitNavigation(INavigationItem navigation, IDocumentationFile model);
}
