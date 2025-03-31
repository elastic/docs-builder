// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;

namespace Elastic.Markdown.IO.Configuration;

public interface IDocumentationPlugin
{
	/// Visit the <paramref name="tocItem"/> and its equivalent <see cref="DocumentationFile"/>
	void Visit(DocumentationFile file, ITocItem tocItem);

	/// Allows the extension to discover more documentation files for <see cref="DocumentationSet"/>
	IReadOnlyCollection<DocumentationFile> ScanDocumentationFiles(
		Func<BuildContext, IDirectoryInfo, DocumentationFile[]> scanDocumentationFiles,
		Func<IFileInfo, IDirectoryInfo, DocumentationFile> defaultFileHandling
	);

	/// Create an instance of <see cref="DocumentationFile"/> if it matches the <paramref name="file"/>.
	/// Return `null` to let another extension handle this.
	DocumentationFile? CreateDocumentationFile(IFileInfo file, IDirectoryInfo sourceDirectory, DocumentationSet documentationSet);

	/// Attempts to locate a documentation file by slug, used to locate the document for `docs-builder serve` command
	bool TryGetDocumentationFileBySlug(DocumentationSet documentationSet, string slug, out DocumentationFile? documentationFile);


}
