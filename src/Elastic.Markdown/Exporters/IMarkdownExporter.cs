// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.IO;
using Markdig.Syntax;

namespace Elastic.Markdown.Exporters;

public class MarkdownExportContext
{
	public required MarkdownDocument Document { get; init; }
	public required MarkdownFile File { get; init; }
	public string? LLMText { get; set; }
}

public interface IMarkdownExporter
{
	ValueTask StartAsync(Cancel ctx = default);
	ValueTask StopAsync(Cancel ctx = default);
	ValueTask<bool> ExportAsync(MarkdownExportContext context, Cancel ctx);
}
