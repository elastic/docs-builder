// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;

namespace Elastic.Markdown.Extensions.DetectionRules;

public class RuleDocumentationFileExporter(IFileSystem readFileSystem, IFileSystem writeFileSystem)
	: DocumentationFileExporterBase(readFileSystem, writeFileSystem)
{
	public override string Name { get; } = nameof(RuleDocumentationFileExporter);

	public override async ValueTask ProcessFile(ProcessingFileContext context, Cancel ctx)
	{
		var htmlWriter = context.HtmlWriter;
		var outputFile = context.OutputFile;
		var conversionCollector = context.ConversionCollector;
		switch (context.File)
		{
			case DetectionRuleFile df:
				context.MarkdownDocument = await htmlWriter.WriteAsync(DetectionRuleFile.OutputPath(outputFile, context.BuildContext), df, conversionCollector, ctx);
				break;
			case MarkdownFile markdown:
				context.MarkdownDocument = await htmlWriter.WriteAsync(outputFile, markdown, conversionCollector, ctx);
				break;
			default:
				if (outputFile.Directory is { Exists: false })
					outputFile.Directory.Create();
				await CopyFileFsAware(context.File, outputFile, ctx);
				break;
		}
	}
}
