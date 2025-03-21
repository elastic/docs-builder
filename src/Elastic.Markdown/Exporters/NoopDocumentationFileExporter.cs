// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Markdown.IO;
using Elastic.Markdown.Slices;

namespace Elastic.Markdown.Exporters;

public class NoopDocumentationFileExporter : IDocumentationFileExporter
{
	public string Name { get; } = nameof(NoopDocumentationFileExporter);

	public Task ProcessFile(BuildContext context, DocumentationFile file, IFileInfo outputFile, HtmlWriter htmlWriter,
		IConversionCollector? conversionCollector, Cancel token) =>
		Task.CompletedTask;

	public Task CopyEmbeddedResource(IFileInfo outputFile, Stream resourceStream, Cancel ctx) => Task.CompletedTask;
}
