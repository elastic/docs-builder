// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;
using Elastic.Markdown.Helpers;
using Markdig.Syntax;

namespace Elastic.Markdown.Exporters;

/// <summary>
/// Provides utilities for converting markdown documents to plain text optimized for search indexing.
/// </summary>
public static class PlainTextExporter
{
	/// <summary>
	/// Converts a parsed markdown document to plain text suitable for search indexing.
	/// Strips all markdown formatting while preserving readable content.
	/// </summary>
	/// <param name="document">The parsed markdown document</param>
	/// <param name="context">The documentation configuration context</param>
	/// <returns>Plain text representation of the document</returns>
	public static string ConvertToPlainText(MarkdownDocument document, IDocumentationConfigurationContext context) =>
		DocumentationObjectPoolProvider.UsePlainTextRenderer(context, document, static (renderer, doc) =>
		{
			_ = renderer.Render(doc);
		});
}
