// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Markdown.IO;

namespace Elastic.Markdown.Exporters;

public class NoopDocumentationFileExporter : IDocumentationFileExporter
{
	public string Name { get; } = nameof(NoopDocumentationFileExporter);
	public Task ProcessFile(DocumentationFile file, IFileInfo outputFile, Cancel token) => Task.CompletedTask;
	public Task CopyEmbeddedResource(IFileInfo outputFile, Stream resourceStream, Cancel ctx) => Task.CompletedTask;
}
