// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Markdown.IO;
using Elastic.Markdown.Slices;

namespace Elastic.Markdown.Exporters;

public interface IDocumentationFileExporter
{
	/// Used in documentation state to ensure we break the build cache if a different exporter is chosen
	string Name { get; }

	Task ProcessFile(DocumentationFile file, IFileInfo outputFile, Cancel token);

	Task CopyEmbeddedResource(IFileInfo outputFile, Stream resourceStream, Cancel ctx);
}

public abstract class DocumentationFileExporterBase(IFileSystem readFileSystem, IFileSystem writeFileSystem) : IDocumentationFileExporter
{
	public abstract string Name { get; }
	public abstract Task ProcessFile(DocumentationFile file, IFileInfo outputFile, Cancel token);

	protected async Task CopyFileFsAware(DocumentationFile file, IFileInfo outputFile, Cancel ctx)
	{
		// fast path, normal case.
		if (readFileSystem == writeFileSystem)
			readFileSystem.File.Copy(file.SourceFile.FullName, outputFile.FullName, true);
		//slower when we are mocking the write filesystem
		else
		{
			var bytes = await file.SourceFile.FileSystem.File.ReadAllBytesAsync(file.SourceFile.FullName, ctx);
			await outputFile.FileSystem.File.WriteAllBytesAsync(outputFile.FullName, bytes, ctx);
		}
	}

	public async Task CopyEmbeddedResource(IFileInfo outputFile, Stream resourceStream, Cancel ctx)
	{
		if (outputFile.Directory is { Exists: false })
			outputFile.Directory.Create();
		await using var stream = outputFile.OpenWrite();
		await resourceStream.CopyToAsync(stream, ctx);
	}
}

public class DocumentationFileExporter(
	IFileSystem readFileSystem,
	IFileSystem writeFileSystem,
	HtmlWriter htmlWriter,
	IConversionCollector? conversionCollector
) : DocumentationFileExporterBase(readFileSystem, writeFileSystem)
{
	public override string Name { get; } = nameof(DocumentationFileExporter);

	public override async Task ProcessFile(DocumentationFile file, IFileInfo outputFile, Cancel token)
	{
		if (file is MarkdownFile markdown)
			await htmlWriter.WriteAsync(outputFile, markdown, conversionCollector, token);
		else
		{
			if (outputFile.Directory is { Exists: false })
				outputFile.Directory.Create();
			await CopyFileFsAware(file, outputFile, token);
		}
	}
}
