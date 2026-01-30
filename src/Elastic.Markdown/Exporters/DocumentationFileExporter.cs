// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Markdown.IO;
using Markdig.Syntax;

namespace Elastic.Markdown.Exporters;

public class ProcessingFileContext
{
	public required BuildContext BuildContext { get; init; }
	public required DocumentationFile File { get; init; }
	public required IFileInfo OutputFile { get; init; }
	public required HtmlWriter HtmlWriter { get; init; }
	public required IConversionCollector? ConversionCollector { get; init; }

	public MarkdownDocument? MarkdownDocument { get; set; }
}

public interface IDocumentationFileExporter
{
	/// Used in the documentation state to ensure we break the build cache if a different exporter is chosen
	string Name { get; }

	ValueTask ProcessFile(ProcessingFileContext context, Cancel ctx);

	Task CopyEmbeddedResource(IFileInfo outputFile, Stream resourceStream, Cancel ctx);
}

public abstract class DocumentationFileExporterBase(IFileSystem readFileSystem, IFileSystem writeFileSystem) : IDocumentationFileExporter
{
	public abstract string Name { get; }

	public abstract ValueTask ProcessFile(ProcessingFileContext context, Cancel ctx);

	protected async Task CopyFileFsAware(DocumentationFile file, IFileInfo outputFile, Cancel ctx)
	{
		// fast path, normal case.
		if (readFileSystem == writeFileSystem)
			readFileSystem.File.Copy(file.SourceFile.FullName, outputFile.FullName, true);
		//slower when we are mocking the write-filesystem
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

public class DocumentationFileExporter(IFileSystem readFileSystem, IFileSystem writeFileSystem)
	: DocumentationFileExporterBase(readFileSystem, writeFileSystem)
{
	public override string Name => nameof(DocumentationFileExporter);

	public override async ValueTask ProcessFile(ProcessingFileContext context, Cancel ctx)
	{
		if (context.File is MarkdownFile markdown)
			context.MarkdownDocument = await context.HtmlWriter.WriteAsync(context.OutputFile, markdown, context.ConversionCollector, ctx);
		else
		{
			if (context.OutputFile.Directory is { Exists: false })
				context.OutputFile.Directory.Create();
			await CopyFileFsAware(context.File, context.OutputFile, ctx);
		}
	}
}
