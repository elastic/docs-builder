// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;

namespace Documentation.Assembler.Exporters;

public class LLMTextExporter : IMarkdownExporter
{
	public ValueTask StartAsync(CancellationToken ctx = default) => ValueTask.CompletedTask;

	public ValueTask StopAsync(CancellationToken ctx = default) => ValueTask.CompletedTask;

	public ValueTask<bool> ExportAsync(MarkdownExportContext context, CancellationToken ctx)
	{
		var llmText = context.LLMText ??= MarkdownFile.ToLLMText(context.Document);
		return ValueTask.FromResult(true);
	}
}
