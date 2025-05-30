// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;

namespace Elastic.Markdown.Exporters;

public class NoopDocumentationFileExporter : IDocumentationFileExporter
{
	public string Name { get; } = nameof(NoopDocumentationFileExporter);

	public ValueTask ProcessFile(ProcessingFileContext context, Cancel ctx) =>
		ValueTask.CompletedTask;

	public Task CopyEmbeddedResource(IFileInfo outputFile, Stream resourceStream, Cancel ctx) => Task.CompletedTask;
}
