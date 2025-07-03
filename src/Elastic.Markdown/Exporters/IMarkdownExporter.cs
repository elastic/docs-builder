// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst;
using Markdig.Syntax;

namespace Elastic.Markdown.Exporters;


public record MarkdownExportContext
{
}
public record MarkdownExportFileContext
{
	public required BuildContext BuildContext { get; init; }
	public required IParserResolvers Resolvers { get; init; }
	public required MarkdownDocument Document { get; init; }
	public required MarkdownFile SourceFile { get; init; }
	public required IFileInfo DefaultOutputFile { get; init; }
	public string? LLMText { get; set; }
}

public interface IMarkdownExporter
{
	ValueTask StartAsync(Cancel ctx = default);
	ValueTask StopAsync(Cancel ctx = default);
	ValueTask<bool> ExportAsync(MarkdownExportFileContext fileContext, Cancel ctx);
	ValueTask<bool> FinishExportAsync(IDirectoryInfo outputFolder, Cancel ctx);
}
