// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;
using Elastic.Markdown.Slices;

namespace Elastic.Markdown.Extensions.DetectionRules;

public class RuleDocumentationFileExporter(IFileSystem readFileSystem, IFileSystem writeFileSystem)
	: DocumentationFileExporterBase(readFileSystem, writeFileSystem)
{
	public override string Name { get; } = nameof(RuleDocumentationFileExporter);

	public override async Task ProcessFile(BuildContext context, DocumentationFile file, IFileInfo outputFile, HtmlWriter htmlWriter,
		IConversionCollector? conversionCollector, Cancel token)
	{
		if (file is DetectionRuleFile df)
			await htmlWriter.WriteAsync(DetectionRuleFile.OutputPath(outputFile, context), df, conversionCollector, token);
		else if (file is MarkdownFile markdown)
			await htmlWriter.WriteAsync(outputFile, markdown, conversionCollector, token);
		else
		{
			if (outputFile.Directory is { Exists: false })
				outputFile.Directory.Create();
			await CopyFileFsAware(file, outputFile, token);
		}
	}
}
