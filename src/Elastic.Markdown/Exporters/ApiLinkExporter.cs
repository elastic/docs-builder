// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Markdown.Exporters;

/// <summary>
/// Records every link to an API reference page so the assembler can check it once those pages exist.
/// Reuses the document each page already parsed for rendering, so it costs one tree walk per page.
/// </summary>
public class ApiLinkExporter : IMarkdownExporter
{
	/// <inheritdoc />
	public ValueTask StartAsync(Cancel ctx = default) => default;

	/// <inheritdoc />
	public ValueTask StopAsync(Cancel ctx = default) => default;

	/// <inheritdoc />
	public ValueTask<bool> ExportAsync(MarkdownExportFileContext fileContext, Cancel ctx)
	{
		var file = fileContext.SourceFile.SourceFile.FullName;
		foreach (var link in OutboundLinkExtractor.ExtractWithLocations(fileContext.Document, file))
		{
			if (ApiLinkMatcher.TryGetApiPath(link.Url, out _))
				fileContext
					.BuildContext
					.Collector
					.EmitApiLink(new ApiLinkReference(link.Url, link.File, link.Line, link.Column, link.Url.Length));
		}

		return ValueTask.FromResult(true);
	}

	/// <inheritdoc />
	public ValueTask<bool> FinishExportAsync(IDirectoryInfo outputFolder, Cancel ctx) => ValueTask.FromResult(true);
}
