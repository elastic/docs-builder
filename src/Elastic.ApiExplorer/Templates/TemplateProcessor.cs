// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.Configuration.Toc;

namespace Elastic.ApiExplorer.Templates;

/// <summary>
/// Processes API landing page templates using standard markdown rendering.
/// </summary>
public class TemplateProcessor(IMarkdownStringRenderer markdownRenderer)
{
	private readonly IMarkdownStringRenderer _markdownRenderer = markdownRenderer;

	/// <summary>
	/// Processes a template file for a specific API configuration.
	/// </summary>
	public async Task<string> ProcessTemplateAsync(
		ResolvedApiConfiguration apiConfig,
		CancellationToken cancellationToken = default)
	{
		if (!apiConfig.HasCustomTemplate || apiConfig.TemplateFile == null)
			return string.Empty;

		// Read template content
		var templateContent = await File.ReadAllTextAsync(apiConfig.TemplateFile.FullName, cancellationToken);

		// Template uses standard substitutions and directives - render directly.
		return _markdownRenderer.Render(templateContent, apiConfig.TemplateFile);
	}
}

/// <summary>
/// Factory for creating template processors with proper dependencies.
/// </summary>
public class TemplateProcessorFactory
{
	/// <summary>
	/// Creates a template processor with the required dependencies.
	/// </summary>
	public static TemplateProcessor Create(IMarkdownStringRenderer markdownRenderer) => new(markdownRenderer);
}
